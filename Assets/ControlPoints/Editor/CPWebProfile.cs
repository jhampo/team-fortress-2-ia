using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Assets are cloned, never edit PC/Mobile graphics or the authored map for a web export.
[BuildCallbackVersion(3)]
public sealed class CPWebProfile : IProcessSceneWithReport {
 public int callbackOrder=>10;
 public static string Configure(){
  const string root="Assets/ControlPoints/Resources/";
  const string path=root+"CPWebPipeline.asset",rendererPath=root+"CPWebRenderer.asset";
  if(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath)==null)AssetDatabase.CopyAsset("Assets/Settings/Mobile_Renderer.asset",rendererPath);
  var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
  var rp=new SerializedObject(renderer);rp.FindProperty("m_RenderingMode").intValue=0;rp.FindProperty("m_DepthPrimingMode").intValue=0;rp.FindProperty("m_RendererFeatures").ClearArray();rp.ApplyModifiedPropertiesWithoutUndo();
  if(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path)==null)AssetDatabase.CopyAsset("Assets/Settings/Mobile_RPAsset.asset",path);
  var asset=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);var so=new SerializedObject(asset);
  so.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;
  foreach(string key in new[]{"m_RequireDepthTexture","m_RequireOpaqueTexture","m_SupportsHDR","m_AdditionalLightShadowsSupported","m_SoftShadowsSupported","m_ReflectionProbeBlending","m_ReflectionProbeBoxProjection","m_SupportsLightCookies","m_SupportsLightLayers"}){var p=so.FindProperty(key);if(p!=null)p.boolValue=false;}
  var gpu=so.FindProperty("m_GPUResidentDrawerMode");if(gpu!=null)gpu.intValue=0;
  so.FindProperty("m_MainLightShadowmapResolution").intValue=1024;
  so.FindProperty("m_MainLightShadowsSupported").boolValue=true;
  so.FindProperty("m_ShadowCascadeCount").intValue=1;so.FindProperty("m_ShadowDistance").floatValue=28;
  so.FindProperty("m_AdditionalLightsRenderingMode").intValue=1;so.FindProperty("m_AdditionalLightsPerObjectLimit").intValue=2;
  so.FindProperty("m_RenderScale").floatValue=.85f;so.FindProperty("m_MSAA").intValue=1;
  so.FindProperty("m_UseSRPBatcher").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
  var quality=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
  var levels=quality.FindProperty("m_QualitySettings");int index=-1;
  for(int i=0;i<levels.arraySize;i++)if(levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue=="Web")index=i;
  if(index<0){index=levels.arraySize;levels.InsertArrayElementAtIndex(index);}
  var level=levels.GetArrayElementAtIndex(index);level.FindPropertyRelative("name").stringValue="Web";level.FindPropertyRelative("customRenderPipeline").objectReferenceValue=asset;
  level.FindPropertyRelative("vSyncCount").intValue=0;level.FindPropertyRelative("lodBias").floatValue=.8f;level.FindPropertyRelative("shadows").intValue=1;level.FindPropertyRelative("shadowDistance").floatValue=28;level.FindPropertyRelative("realtimeReflectionProbes").boolValue=false;
  var excludes=level.FindPropertyRelative("excludedTargetPlatforms");excludes.arraySize=3;excludes.GetArrayElementAtIndex(0).stringValue="Standalone";excludes.GetArrayElementAtIndex(1).stringValue="Android";excludes.GetArrayElementAtIndex(2).stringValue="iPhone";
  var defaults=quality.FindProperty("m_PerPlatformDefaultQuality");for(int i=0;i<defaults.arraySize;i++){var pair=defaults.GetArrayElementAtIndex(i);if(pair.FindPropertyRelative("first").stringValue=="WebGL")pair.FindPropertyRelative("second").intValue=index;}
  // Unity's automatic static batching runs before scene callbacks and replaces
  // MeshFilters with shared, precombined meshes. Our spatial batching must see
  // the original meshes; applying both duplicates entire batches per object.
  quality.ApplyModifiedPropertiesWithoutUndo();PlayerSettings.SetStaticBatchingForPlatform(BuildTarget.WebGL,false);
  EditorUtility.SetDirty(asset);EditorUtility.SetDirty(renderer);AssetDatabase.SaveAssets();
  return "Web profile: Forward, SRP/static batching, 0.85 adaptive scale, hard 1024 shadows/28m, 2 lights per object; PC untouched.";
 }
 public void OnProcessScene(Scene scene,BuildReport report){
  if(report==null||report.summary.platform!=BuildTarget.WebGL)return;
  // NavMesh data is registered by CPMatch.Awake. Don't activate agents before it exists.
  foreach(var root in scene.GetRootGameObjects())foreach(var agent in root.GetComponentsInChildren<NavMeshAgent>(true))agent.enabled=false;
  var map=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Powerhouse");
  var match=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FpsStage2.CPMatch>()).FirstOrDefault();
  if(map!=null)Debug.Log(CombineMap(map,match));
 }

 public static string CombineMap(GameObject map,FpsStage2.CPMatch match){
  var protectedNames=new HashSet<string>(match!=null?match.zones.SelectMany(z=>z.glowRenderers).Where(r=>r!=null).Select(r=>r.name):Array.Empty<string>());
  // Animated water and any scripted renderer must keep its identity/property block.
  var renderers=map.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.isStatic&&!protectedNames.Contains(r.name)&&r.GetComponents<MonoBehaviour>().Length==0&&r.sharedMaterials.Length==1&&r.sharedMaterial!=null&&r.sharedMaterial.renderQueue<3000&&r.GetComponent<MeshFilter>()?.sharedMesh!=null).ToArray();
  if(renderers.Any(r=>r.isPartOfStaticBatch))throw new InvalidOperationException("Disable automatic WebGL static batching before spatial batching: source meshes are already combined.");
  var simple=Shader.Find("Universal Render Pipeline/Simple Lit");if(simple==null)throw new InvalidOperationException("Simple Lit shader is missing");
  var materials=new Dictionary<Material,Material>();int pieces=0,groups=0,triangles=0;
  foreach(var sector in renderers.GroupBy(r=>new {material=r.sharedMaterial,x=Mathf.FloorToInt(r.bounds.center.x/64),y=Mathf.FloorToInt(r.bounds.center.y/32),z=Mathf.FloorToInt(r.bounds.center.z/64)})){
   var sourceMaterial=sector.Key.material;
   if(!materials.TryGetValue(sourceMaterial,out var material)){
    material=new Material(sourceMaterial){name=sourceMaterial.name+"_Web",shader=simple};
    material.SetFloat("_SpecularHighlights",0);material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");material.DisableKeyword("_SPECULAR_COLOR");
    materials.Add(sourceMaterial,material);
   }
   var pending=new List<MeshRenderer>();int vertices=0;
   Action flush=()=>{
    if(pending.Count==0)return;
    var mesh=new Mesh{name="WebSectorMesh_"+groups,indexFormat=IndexFormat.UInt32};
    var sources=pending.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,subMeshIndex=0,transform=map.transform.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray();
    mesh.CombineMeshes(sources,true,true);mesh.RecalculateBounds();triangles+=mesh.triangles.Length/3;
    var go=new GameObject("WebSector_"+groups+"_"+sourceMaterial.name);go.transform.SetParent(map.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
    var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
    // Geometry is already combined into spatial sectors; keep collider objects untouched.
    foreach(var r in pending){UnityEngine.Object.DestroyImmediate(r);pieces++;}pending.Clear();vertices=0;groups++;
   };
   foreach(var r in sector){int v=r.GetComponent<MeshFilter>().sharedMesh.vertexCount;if(vertices+v>60000)flush();pending.Add(r);vertices+=v;}flush();
  }
  return $"Web geometry: {pieces} static renderers -> {groups} spatial batches, {triangles} triangles. All colliders and control-point renderers retained.";
 }
}
