using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Keeps the static-hosting export reproducible without changing gameplay.
public sealed class CPWebExport : IPostprocessBuildWithReport
{
    public int callbackOrder => 1000;

    public static string Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before exporting.");
        PlayerSettings.productName = "Powerhouse";
        PlayerSettings.WebGL.template = "PROJECT:Powerhouse";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.initialMemorySize = 256;
        PlayerSettings.WebGL.maximumMemorySize = 2048;
        PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
        // Avoid Bee repeatedly invalidating WebGL_web.loader.js while hashing it.
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        PlayerSettings.WebGL.showDiagnostics = false;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Powerhouse/Scenes/Powerhouse.unity", true) };
        CPWebProfile.Configure();
        CPGraphicsProfiles.Configure();

        // These effects create materials at runtime. Retain their exact shader variants.
        const string directory = "Assets/ControlPoints/Resources/WebShaderReferences";
        if (!AssetDatabase.IsValidFolder(directory)) AssetDatabase.CreateFolder("Assets/ControlPoints/Resources", "WebShaderReferences");
        ReferenceMaterial(directory, "MuzzleFlame", "Stage2/MuzzleFlame", false);
        ReferenceMaterial(directory, "Unlit", "Universal Render Pipeline/Unlit", false);
        ReferenceMaterial(directory, "ParticlesTransparent", "Universal Render Pipeline/Particles/Unlit", true);
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        return "Web export configured: gzip fallback, optimized Web graphics, retained runtime effects.";
    }

    static void ReferenceMaterial(string directory, string name, string shaderName, bool transparent)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) throw new InvalidOperationException("Missing shader: " + shaderName);
        string path = directory + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        if (transparent)
        {
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
        }
        EditorUtility.SetDirty(material);
    }

    // A fresh scene asset invalidates Unity's scene cache without throwing away
    // the expensive IL2CPP/WebAssembly compilation cache. The authored map stays unchanged.
    public static string PrepareScene()
    {
        if (EditorApplication.isPlaying || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Stop Play Mode and wait for any build to finish.");
        const string source = "Assets/Powerhouse/Scenes/Powerhouse.unity";
        const string folder = "Assets/ControlPoints/Generated";
        const string path = folder + "/Powerhouse_Web.unity";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/ControlPoints", "Generated");
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(source);
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open Powerhouse before exporting.");
        if (scene.isDirty) throw new InvalidOperationException("Save the authored scene before exporting.");
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path, true);
        var copy = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            // Only the disposable web scene changes: preserve every class on both teams.
            var actors = copy.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<FpsStage2.CPActor>(true)).ToList();
            foreach (var team in new[] { FpsStage2.CPTeam.RED, FpsStage2.CPTeam.BLU })
            {
                for (int i = 0; i < 2; i++)
                {
                    var duplicate = actors.Where(a => !a.isPlayer && a.team == team)
                        .GroupBy(a => a.characterClass).Where(g => g.Count() > 1)
                        .OrderByDescending(g => g.Count()).ThenBy(g => g.Key).FirstOrDefault();
                    if (duplicate == null) throw new InvalidOperationException("Cannot remove two web bots while preserving all four classes.");
                    var removed = duplicate.OrderBy(a => a.name).Last();
                    actors.Remove(removed);
                    UnityEngine.Object.DestroyImmediate(removed.gameObject);
                }
            }
            Debug.Log("Web roster: " + actors.Count(a => !a.isPlayer && a.team == FpsStage2.CPTeam.RED) + " RED bots, " + actors.Count(a => !a.isPlayer && a.team == FpsStage2.CPTeam.BLU) + " BLU bots; authored scene unchanged.");
            var revision = new GameObject("WebBuildRevision_" + Guid.NewGuid().ToString("N"));
            revision.tag = "EditorOnly";
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(revision, copy);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(copy);
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(copy, true); }
        AssetDatabase.SaveAssets();
        return path;
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL) return;
        // Unity finalizes size/result after postprocessing. Read the completed report.
        EditorApplication.delayCall += () =>
        {
            if (report == null) return;
            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded) return;
            File.WriteAllText(Path.Combine(summary.outputPath, "build-info.json"),
                JsonUtility.ToJson(new ExportInfo {
                    unity = Application.unityVersion, builtUtc = DateTime.UtcNow.ToString("O"),
                    result = summary.result.ToString(), errors = summary.totalErrors,
                    warnings = summary.totalWarnings, bytes = summary.totalSize
                }, true));
        };
    }

    [Serializable] sealed class ExportInfo
    {
        public string unity, builtUtc, result;
        public int errors, warnings;
        public ulong bytes;
    }
}
