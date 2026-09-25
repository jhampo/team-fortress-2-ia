using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FpsStage2 {
// Web-only presentation budgets. Combat, timers, movement and player count are unchanged.
[DefaultExecutionOrder(-400)]
public sealed class CPWebPerformance : MonoBehaviour {
 public static bool Active => Application.platform == RuntimePlatform.WebGLPlayer;
 static int decisionFrame=-1, decisions;
 public static bool CanThink(){
  if(!Active)return true;
  if(decisionFrame!=Time.frameCount){decisionFrame=Time.frameCount;decisions=0;}
  if(decisions>=2)return false;decisions++;return true;
 }
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
 static void Install(){if(!Active)return;SceneManager.sceneLoaded-=InstallScene;SceneManager.sceneLoaded+=InstallScene;}
 static void InstallScene(Scene scene,LoadSceneMode mode){if(CPMatch.Instance!=null)new GameObject("WebPerformance").AddComponent<CPWebPerformance>();}
 struct Detail {public Renderer renderer;public Vector3 position;public float distanceSquared;}
 readonly List<Detail> details=new List<Detail>();
 readonly List<Light> lights=new List<Light>();
 readonly Plane[] lightPlanes=new Plane[6];
 readonly float[] samples=new float[256];
 UniversalRenderPipelineAsset pipeline;RenderPipelineAsset previousPipeline;Camera cameraView;CPMatch match;
 float nextBudget,elapsed,adjustAt;int frames,count;bool configured;
 CPGraphicsQuality quality;float minScale=.60f,maxScale=.90f,detailRange=1;int lightBudget=12;
 public float FramesPerSecond {get;private set;}
 public float P95Milliseconds {get;private set;}
 public int VisibleDetails {get;private set;}
 public int ActiveLights {get;private set;}
 public float RenderScale=>pipeline!=null?pipeline.renderScale:1;
#if UNITY_WEBGL && !UNITY_EDITOR
 [DllImport("__Internal")] static extern void CPWebReport(string message);
#endif
 IEnumerator Start(){
  // This component is never installed in native players or normal Editor play mode.
  Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
  var source=Resources.Load<UniversalRenderPipelineAsset>("CPWebPipeline");
  if(source==null){Debug.LogError("Web graphics profile missing");yield break;}
  previousPipeline=QualitySettings.renderPipeline;pipeline=Instantiate(source);QualitySettings.renderPipeline=pipeline;
  QualitySettings.realtimeReflectionProbes=false;QualitySettings.lodBias=.8f;
  QualitySettings.particleRaycastBudget=32;
  yield return null;yield return null;
  match=CPMatch.Instance;if(match==null)yield break;cameraView=match.player.playerCamera;
  var cameraData=cameraView.GetUniversalAdditionalCameraData();
  cameraData.renderPostProcessing=false;cameraData.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
  cameraData.requiresDepthTexture=false;cameraData.requiresColorTexture=false;
  cameraView.allowHDR=false;
  var map=GameObject.Find("Powerhouse");
  if(map!=null){
   foreach(var r in map.GetComponentsInChildren<MeshRenderer>()){
    float extent=r.bounds.size.magnitude;
    if(extent>=12)continue;float distance=extent<3?65:110;
    details.Add(new Detail{renderer=r,position=r.bounds.center,distanceSquared=distance*distance});
    if(extent<3)r.shadowCastingMode=ShadowCastingMode.Off;
   }
   foreach(var l in map.GetComponentsInChildren<Light>())if(l.type!=LightType.Directional)lights.Add(l);
  }
  // Particle quality is applied/restored centrally, including runtime-created fire.
  // Keep navigation and weapon origins live; only expensive skinning can be culled.
  foreach(var skin in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))skin.updateWhenOffscreen=false;
  // Medium avoidance retains local separation with fewer velocity samples per bot.
  foreach(var agent in FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None))agent.obstacleAvoidanceType=ObstacleAvoidanceType.MedQualityObstacleAvoidance;
  configured=true;ApplyQuality(CPSettings.ReadGraphics());
 }
 public void ApplyQuality(CPGraphicsQuality value){
  quality=value;if(!configured||pipeline==null)return;
  bool low=value==CPGraphicsQuality.Low,ultra=value==CPGraphicsQuality.Ultra;
  minScale=low?.50f:ultra?1:.60f;maxScale=low?.70f:ultra?1:.90f;
  pipeline.renderScale=low?.65f:ultra?1:.85f;
  pipeline.msaaSampleCount=ultra?2:1;pipeline.shadowDistance=ultra?60:0;
  pipeline.mainLightShadowmapResolution=ultra?2048:1024;pipeline.maxAdditionalLightsCount=low?1:ultra?4:2;
  QualitySettings.lodBias=low?.55f:ultra?1.5f:.8f;
  lightBudget=low?3:ultra?24:8;detailRange=low?.75f:ultra?1.5f:1;
  adjustAt=Time.unscaledTime+8;elapsed=0;frames=count=0;nextBudget=0;
 }
 void Update(){
  if(!configured||cameraView==null)return;
  // The front end is an opaque overlay: don't render the entire hidden map behind it.
  cameraView.enabled=match.flow==null||match.flow.Phase!=CPRoundPhase.MainMenu;
  if(Time.unscaledTime>=nextBudget){nextBudget=Time.unscaledTime+.35f;BudgetScene();}
  float dt=Time.unscaledDeltaTime;if(dt<=0||!Application.isFocused){elapsed=0;frames=count=0;return;}
  elapsed+=dt;frames++;if(count<samples.Length)samples[count++]=dt*1000;
  if(elapsed<2.5f)return;
  FramesPerSecond=frames/elapsed;Array.Sort(samples,0,count);P95Milliseconds=samples[Mathf.Max(0,Mathf.CeilToInt(count*.95f)-1)];
  if(quality!=CPGraphicsQuality.Ultra&&match.RoundLive&&!match.MenuOpen&&Time.unscaledTime>=adjustAt){
   if(FramesPerSecond<48&&pipeline.renderScale>minScale){pipeline.renderScale=Mathf.Max(minScale,pipeline.renderScale-.08f);adjustAt=Time.unscaledTime+5;}
   else if(FramesPerSecond>58&&pipeline.renderScale<maxScale){pipeline.renderScale=Mathf.Min(maxScale,pipeline.renderScale+.04f);adjustAt=Time.unscaledTime+12;}
   if(pipeline.renderScale<=minScale+.01f&&FramesPerSecond<30)pipeline.shadowDistance=0;
  }
  int onNav=0,moving=0,bots=0;foreach(var a in match.actors)if(!a.isPlayer){bots++;var nav=a.GetComponent<NavMeshAgent>();if(nav!=null&&nav.isOnNavMesh){onNav++;if(nav.velocity.sqrMagnitude>.1f)moving++;}}
#if UNITY_WEBGL && !UNITY_EDITOR
  CPWebReport($"Unity: {FramesPerSecond:F1} FPS | p95 {P95Milliseconds:F1} ms | escala {RenderScale:F2}\nBots: {onNav}/{bots} navegación, {moving} en movimiento | luces {ActiveLights}\nGPU: {SystemInfo.graphicsDeviceName}");
#endif
  elapsed=0;frames=count=0;
 }
 void BudgetScene(){
  var position=cameraView.transform.position;VisibleDetails=0;
  GeometryUtility.CalculateFrustumPlanes(cameraView,lightPlanes);
  foreach(var d in details){if(d.renderer==null)continue;bool visible=(d.position-position).sqrMagnitude<d.distanceSquared*detailRange*detailRange;d.renderer.forceRenderingOff=!visible;if(visible)VisibleDetails++;}
  // Sort a small, retained list rather than allocate queries every frame.
  if(quality!=CPGraphicsQuality.Ultra){ActiveLights=0;foreach(var light in lights)if(light!=null&&light.enabled)ActiveLights++;return;} // Low/Medium lights owned by CPQualityEffects.
  lights.Sort((a,b)=>(a.transform.position-position).sqrMagnitude.CompareTo((b.transform.position-position).sqrMagnitude));
  ActiveLights=0;for(int i=0;i<lights.Count;i++){var light=lights[i];float reach=light.range+25;bool show=ActiveLights<lightBudget&&(light.transform.position-position).sqrMagnitude<reach*reach&&GeometryUtility.TestPlanesAABB(lightPlanes,new Bounds(light.transform.position,Vector3.one*(light.range*2+4)));light.enabled=show;if(show)ActiveLights++;}
 }
 void OnDestroy(){if(cameraView!=null)cameraView.enabled=true;if(pipeline!=null){if(QualitySettings.renderPipeline==pipeline)QualitySettings.renderPipeline=previousPipeline;Destroy(pipeline);}}
}
}
