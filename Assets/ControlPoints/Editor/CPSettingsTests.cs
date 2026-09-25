using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
namespace FpsStage2 {
public static class CPSettingsTests {
 public static string Run(){
  var match=CPMatch.Instance;if(match==null||!Application.isPlaying)throw new InvalidOperationException("Run in Play Mode.");
  var settings=match.GetComponent<CPSettings>();var menu=match.GetComponent<CPMenus>();
  string[] keys={CPSettings.GraphicsKey,CPSettings.SensitivityKey,CPSettings.VolumeKey,"PowerhouseMuted"};
  bool[] existed=keys.Select(PlayerPrefs.HasKey).ToArray();int oldQuality=PlayerPrefs.GetInt(keys[0],1),oldMute=PlayerPrefs.GetInt(keys[3],0);
  float oldSensitivity=PlayerPrefs.GetFloat(keys[1],1),oldVolume=PlayerPrefs.GetFloat(keys[2],1);
  var runtimeQuality=settings.Graphics;float runtimeSensitivity=settings.Sensitivity,runtimeVolume=settings.Volume,baseSensitivity=match.player.sensitivity/runtimeSensitivity;bool runtimeMute=match.audioSystem.Muted;
  int count=0;Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception("FAILED: "+label);count++;};
  try {
   PlayerPrefs.DeleteKey(keys[0]);check(CPSettings.ReadGraphics()==CPGraphicsQuality.Medium,"default Medium");
   var buttons=menu.GetComponentsInChildren<Button>(true);
   foreach(var name in new[]{"BAJA","MEDIA","ULTRA","CONTINUAR","IR AL MENÚ"})check(buttons.Any(b=>b.name==name),"button "+name);
   buttons.First(b=>b.name=="BAJA").onClick.Invoke();check(settings.Graphics==CPGraphicsQuality.Low,"Low button applies");
   var pipeline=QualitySettings.renderPipeline as UniversalRenderPipelineAsset;check(pipeline!=null&&pipeline.shadowDistance==0&&Mathf.Approximately(pipeline.renderScale,.65f),"Low settings applied");
   check(QualitySettings.globalTextureMipmapLimit==2,"Low quarter-resolution textures");
   check(pipeline.name.Contains("CPEconomy"),"Low safe Forward profile");
   check(CPQualityEffects.Count(100)==25&&CPQualityEffects.Count(100,true)==15,"Low particles/smoke budgets");
   check(match.portraits.All(t=>t.ignoreMipmapLimit),"HUD portraits remain full resolution");
   buttons.First(b=>b.name=="ULTRA").onClick.Invoke();pipeline=QualitySettings.renderPipeline as UniversalRenderPipelineAsset;check(settings.Graphics==CPGraphicsQuality.Ultra&&Mathf.Approximately(pipeline.renderScale,1)&&pipeline.shadowDistance>=60,"Ultra applies");
   buttons.First(b=>b.name=="MEDIA").onClick.Invoke();check(settings.Graphics==CPGraphicsQuality.Medium,"Medium restored");
   check(QualitySettings.globalTextureMipmapLimit==1,"Medium half-resolution textures");
   check((QualitySettings.renderPipeline as UniversalRenderPipelineAsset).shadowDistance==0,"Medium simple shadows only");
   check(CPQualityEffects.Count(100)==50&&CPQualityEffects.Count(100,true)==35,"Medium particles/smoke budgets");
   var sensitivity=menu.GetComponentsInChildren<Slider>(true).First(s=>s.name=="MouseSensitivity");sensitivity.value=1.75f;
   check(Mathf.Approximately(match.player.sensitivity,baseSensitivity*1.75f),"slider changes actual mouse sensitivity");
   var volume=menu.GetComponentsInChildren<Slider>(true).First(s=>s.name=="MasterVolume");volume.value=.37f;
   check(Mathf.Approximately(AudioListener.volume,.37f),"slider changes actual volume");
   if(!match.audioSystem.Muted)match.audioSystem.ToggleMute();check(AudioListener.volume==0,"mute");match.audioSystem.ToggleMute();check(Mathf.Approximately(AudioListener.volume,.37f),"unmute restores chosen volume");
   settings.Flush();check(Mathf.Approximately(PlayerPrefs.GetFloat(keys[1]),1.75f)&&Mathf.Approximately(PlayerPrefs.GetFloat(keys[2]),.37f),"preferences saved");
   settings.SetSensitivity(100);check(settings.Sensitivity==3,"sensitivity upper bound");settings.SetSensitivity(-100);check(settings.Sensitivity==.25f,"sensitivity lower bound");
   settings.ResetControlsAndAudio();check(settings.Sensitivity==1&&settings.Volume==1,"reset controls/audio");
   check(match.actors.Length==16,"roster unchanged");check(CPTests.Rules().StartsWith("35"),"combat rules unchanged");
   return count+" settings/UI assertions passed";
  } finally {
   settings.SetGraphics((int)runtimeQuality);settings.SetSensitivity(runtimeSensitivity);settings.SetVolume(runtimeVolume);if(match.audioSystem.Muted!=runtimeMute)match.audioSystem.ToggleMute();settings.Flush();
   foreach(var slider in menu.GetComponentsInChildren<Slider>(true)){if(slider.name=="MouseSensitivity")slider.SetValueWithoutNotify(runtimeSensitivity);if(slider.name=="MasterVolume")slider.SetValueWithoutNotify(runtimeVolume);}
   PlayerPrefs.SetInt(keys[0],oldQuality);PlayerPrefs.SetFloat(keys[1],oldSensitivity);PlayerPrefs.SetFloat(keys[2],oldVolume);PlayerPrefs.SetInt(keys[3],oldMute);
   for(int i=0;i<keys.Length;i++)if(!existed[i])PlayerPrefs.DeleteKey(keys[i]);PlayerPrefs.Save();
  }
 }
}
}
