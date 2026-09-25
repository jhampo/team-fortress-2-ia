using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FpsStage2 {
public enum CPGraphicsQuality { Low, Medium, Ultra }

// Presentation/input preferences only. No weapon, movement or bot statistics change.
public sealed class CPSettings : MonoBehaviour {
 public const string GraphicsKey="PowerhouseGraphics", SensitivityKey="PowerhouseSensitivity", VolumeKey="PowerhouseVolume";
 public CPGraphicsQuality Graphics {get;private set;}
 public float Sensitivity {get;private set;}
 public float Volume=>match.audioSystem.MasterVolume;
 // A new web preference starts at Medium instead of inheriting the old forced-Ultra setting.
 static string GraphicsPreferenceKey=>CPWebPerformance.Active?"PowerhouseWebGraphicsSelectable":GraphicsKey;
 public static CPGraphicsQuality ReadGraphics()=> (CPGraphicsQuality)Mathf.Clamp(PlayerPrefs.GetInt(GraphicsPreferenceKey,1),0,2);
 CPMatch match;float baseSensitivity,saveAt,originalLod;bool dirty,originalPost;int originalMip;CPQualityEffects effects;
 RenderPipelineAsset originalPipeline;UniversalRenderPipelineAsset nativePipeline,nativeSource,economyPipeline;
 UniversalAdditionalCameraData cameraData;

 public void Initialize(CPMatch value){
  match=value;baseSensitivity=match.player.sensitivity;Graphics=ReadGraphics();originalMip=QualitySettings.globalTextureMipmapLimit;
  Sensitivity=Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey,1),.25f,3);
  match.player.sensitivity=baseSensitivity*Sensitivity;
  if(!CPWebPerformance.Active){
   originalPipeline=QualitySettings.renderPipeline;
   nativeSource=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
   originalLod=QualitySettings.lodBias;cameraData=match.player.playerCamera.GetUniversalAdditionalCameraData();originalPost=cameraData.renderPostProcessing;
   if(nativeSource!=null){nativePipeline=Instantiate(nativeSource);QualitySettings.renderPipeline=nativePipeline;}
   var economy=Resources.Load<UniversalRenderPipelineAsset>("CPEconomyPipeline");if(economy!=null)economyPipeline=Instantiate(economy);
   ApplyNativeGraphics();
  }
  effects=gameObject.AddComponent<CPQualityEffects>();effects.Initialize(this,match.player.playerCamera);ApplyTextures();
 }
 public void SetGraphics(int value){
  Graphics=(CPGraphicsQuality)Mathf.Clamp(value,0,2);PlayerPrefs.SetInt(GraphicsPreferenceKey,(int)Graphics);ScheduleSave();
  if(CPWebPerformance.Active){var web=FindFirstObjectByType<CPWebPerformance>();if(web!=null)web.ApplyQuality(Graphics);}
  else ApplyNativeGraphics();
  effects?.Apply(Graphics);ApplyTextures();
 }
 public void SetSensitivity(float value){Sensitivity=Mathf.Clamp(value,.25f,3);match.player.sensitivity=baseSensitivity*Sensitivity;PlayerPrefs.SetFloat(SensitivityKey,Sensitivity);ScheduleSave();}
 public void SetVolume(float value){match.audioSystem.SetMasterVolume(value);ScheduleSave();}
 public void ResetControlsAndAudio(){SetSensitivity(1);SetVolume(1);}
 void ApplyNativeGraphics(){
  if(nativePipeline==null)return;
  bool low=Graphics==CPGraphicsQuality.Low,ultra=Graphics==CPGraphicsQuality.Ultra;
  var selected=!ultra&&economyPipeline!=null?economyPipeline:nativePipeline;QualitySettings.renderPipeline=selected;
  selected.renderScale=low?.65f:ultra?1:.85f;
  selected.msaaSampleCount=ultra?4:1;selected.shadowDistance=ultra?Mathf.Max(60,nativeSource.shadowDistance):0;
  nativePipeline.mainLightShadowmapResolution=ultra?Mathf.Max(2048,nativeSource.mainLightShadowmapResolution):nativeSource.mainLightShadowmapResolution;
  selected.maxAdditionalLightsCount=low?1:ultra?Mathf.Max(4,nativeSource.maxAdditionalLightsCount):2;
  QualitySettings.lodBias=low?.6f:ultra?Mathf.Max(1.5f,originalLod):originalLod;
  if(cameraData!=null){cameraData.renderPostProcessing=ultra&&originalPost;cameraData.requiresDepthTexture=ultra;cameraData.requiresColorTexture=ultra;cameraData.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.None;}
 }
 void ApplyTextures(){QualitySettings.globalTextureMipmapLimit=Graphics==CPGraphicsQuality.Low?2:Graphics==CPGraphicsQuality.Medium?1:0;}
 void ScheduleSave(){dirty=true;saveAt=Time.unscaledTime+.4f;}
 void Update(){if(dirty&&Time.unscaledTime>=saveAt)Flush();}
 public void Flush(){if(!dirty)return;PlayerPrefs.Save();dirty=false;}
 void OnApplicationPause(bool paused){if(paused)Flush();}
 void OnDisable(){Flush();}
 void OnDestroy(){Flush();QualitySettings.globalTextureMipmapLimit=originalMip;if(nativePipeline!=null){if(QualitySettings.renderPipeline==nativePipeline||QualitySettings.renderPipeline==economyPipeline)QualitySettings.renderPipeline=originalPipeline;QualitySettings.lodBias=originalLod;Destroy(nativePipeline);}if(economyPipeline!=null)Destroy(economyPipeline);}
}
}
