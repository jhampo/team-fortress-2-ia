using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System.IO;

namespace ScoutStage1
{
    public static class ScoutStage1Setup
    {
        const string Root = "Assets/ScoutStage1";
        public static string ConfigureModels()
        {
            foreach (var name in new[] { "Idle", "Disparo", "Recarga" })
            {
                var path = Root + "/Models/Scout_" + name + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Legacy;
                importer.importAnimation = true;
                importer.bakeAxisConversion = true;
                importer.animationCompression = ModelImporterAnimationCompression.Off;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                var clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    clip.name = name;
                    clip.loopTime = name == "Idle";
                    clip.wrapMode = name == "Idle" ? WrapMode.Loop : WrapMode.ClampForever;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
            return "Models configured";
        }
        static Material Material(string name, Color color, string texture = null, float metal = 0)
        {
            var path = Root + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metal);
            mat.SetFloat("_Smoothness", .32f);
            if (texture != null) mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+texture+".png"));
            return mat;
        }
        [MenuItem("Scout Stage 1/Create Test Scene")]
        public static void Build()
        {
            Directory.CreateDirectory(Root+"/Scenes");Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Prefabs");AssetDatabase.Refresh();
            var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (previous.isDirty) EditorSceneManager.SaveScene(previous,AssetDatabase.GenerateUniqueAssetPath(Root+"/Scenes/PreviousScene_Backup.unity"),true);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraObject=new GameObject("Scout_PlayerCamera");cameraObject.tag="MainCamera";
            var camera=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<AudioListener>();camera.transform.position=new Vector3(0,1.65f,0);
            camera.nearClipPlane=.01f;camera.farClipPlane=80;camera.fieldOfView=42.55f;camera.transform.rotation=Quaternion.Euler(10,0,0);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.20f,.26f);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/Scout_Idle.fbx"));model.name="Scout_FirstPerson";
            model.transform.SetParent(camera.transform,false);
            model.transform.localPosition=new Vector3(.025f,.05f,.15f);
            var anim=model.GetComponent<Animation>();if(!anim)anim=model.AddComponent<Animation>();
            foreach(var name in new[]{"Idle","Disparo","Recarga"})
            {
                var clip=AssetDatabase.LoadAllAssetsAtPath(Root+"/Models/Scout_"+name+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
                anim.AddClip(clip,name);if(name=="Idle")anim.clip=clip;
            }
            anim.playAutomatically=true;anim.cullingType=AnimationCullingType.AlwaysAnimate;
            var arms=Material("Scout_Wraps",Color.white,"Scout_BaseColor");
            var gun=Material("Dispensadora",Color.white,"Baked_BaseColor",.45f);
            var shell=Material("Cartridge_Red",new Color(.35f,.042f,.018f));
            var brass=Material("Cartridge_Brass",new Color(.65f,.36f,.065f),null,.7f);
            foreach(var rend in model.GetComponentsInChildren<Renderer>())
            {
                rend.sharedMaterial=rend.name.Contains("Scout_Arms")?arms:rend.name.Contains("Dispensadora")?gun:rend.name.EndsWith("Rim")?brass:shell;
                rend.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                if(rend is SkinnedMeshRenderer sk)sk.updateWhenOffscreen=true;
            }
            var behaviour=cameraObject.AddComponent<ScoutFirstPerson>();behaviour.viewmodel=anim;behaviour.playerCamera=camera;
            var platform=GameObject.CreatePrimitive(PrimitiveType.Cube);platform.name="Scout_TestPlatform";platform.transform.position=new Vector3(0,-.15f,1);platform.transform.localScale=new Vector3(8,.3f,8);platform.GetComponent<Renderer>().sharedMaterial=Material("Platform",new Color(.25f,.31f,.36f));
            var lightObject=new GameObject("Scout_KeyLight");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(40,-30,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.38f,.40f,.45f);
            PrefabUtility.SaveAsPrefabAsset(cameraObject,Root+"/Prefabs/Scout_Player.prefab");
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/Scout_FirstPerson_Test.unity");
            AssetDatabase.SaveAssets();Selection.activeGameObject=cameraObject;
        }
        public static string ValidateTiming()
        {
            var t=new ScoutWeaponTiming();
            void Check(bool b,string message){if(!b)throw new System.Exception(message);}
            Check(t.State=="Idle","Starts idle");
            Check(t.Fire(0),"First shot");Check(!t.Fire(.312499),"Early shot blocked");
            Check(t.Fire(.3125),"Exact interval allowed");Check(!t.Reload(.4),"Shot recovery respected");
            Check(t.Reload(.625),"Reload starts after recovery");Check(!t.Fire(1),"Reload blocks fire");Check(!t.Reload(1),"Reload cannot restart");
            t.Tick(.625+1.4333-.000001);Check(t.State=="Recarga","Reload not early");
            t.Tick(.625+1.4333);Check(t.State=="Idle"&&t.Reloads==1,"Reload ends at exact duration");
            Check(t.Fire(.625+1.4333),"Fire resumes");
            return "PASS: idle, 0.3125s fire gate, 1.4333s reload, repeated input, and return to idle";
        }
    }
}

