using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace FpsStage2 {
// Presentation only: never changes damage, projectile speed, burn ticks or collision.
public sealed class CPQualityEffects:MonoBehaviour {
 public static CPGraphicsQuality Current {get;private set;}=CPGraphicsQuality.Medium;
 public static float Density=>Current==CPGraphicsQuality.Low?.25f:Current==CPGraphicsQuality.Medium?.5f:1;
 public static float SmokeDensity=>Current==CPGraphicsQuality.Low?.15f:Current==CPGraphicsQuality.Medium?.35f:1;
 public static int Count(int original,bool smoke=false)=>Mathf.Max(1,Mathf.RoundToInt(original*(smoke?SmokeDensity:Density)));
 class ParticleBudget {public ParticleSystem ps;public float rate,distance;public int maximum;public bool noise;}
 struct Lamp {public Light light;public bool enabled;public LightShadows shadows;}
 struct Surface {public Renderer renderer;public Material[] materials;public ReflectionProbeUsage probes;}
 readonly Dictionary<ParticleSystem,ParticleBudget> particles=new Dictionary<ParticleSystem,ParticleBudget>();
 readonly List<ParticleSystem> expired=new List<ParticleSystem>();readonly List<Lamp> lamps=new List<Lamp>();
 readonly List<Surface> surfaces=new List<Surface>();readonly Dictionary<Material,Material> dull=new Dictionary<Material,Material>();
 readonly Dictionary<Texture2D,bool> textureLimits=new Dictionary<Texture2D,bool>();
 ReflectionProbe[] probes;bool[] probeEnabled;CPSettings settings;Camera cameraView;float discoverAt,lightsAt;bool initialized,originalReflections;
 public void Initialize(CPSettings value,Camera camera){settings=value;cameraView=camera;originalReflections=QualitySettings.realtimeReflectionProbes;
  var map=GameObject.Find("Powerhouse");if(map!=null){
   foreach(var l in map.GetComponentsInChildren<Light>(true))lamps.Add(new Lamp{light=l,enabled=l.enabled,shadows=l.shadows});
   foreach(var r in map.GetComponentsInChildren<Renderer>(true)){var mats=r.sharedMaterials;surfaces.Add(new Surface{renderer=r,materials=mats,probes=r.reflectionProbeUsage});foreach(var m in mats)if(m!=null&&!dull.ContainsKey(m)&&m.HasProperty("_Smoothness")&&m.GetFloat("_Smoothness")>.05f){var clone=new Material(m);clone.name=m.name+"_Economy";clone.SetFloat("_Smoothness",.02f);if(clone.HasProperty("_EnvironmentReflections"))clone.SetFloat("_EnvironmentReflections",0);clone.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");dull.Add(m,clone);}}
  }
  foreach(var surface in surfaces)foreach(var m in surface.materials)if(m!=null)foreach(var property in m.GetTexturePropertyNames())if(m.GetTexture(property) is Texture2D texture&&texture.mipmapCount>1){if(!textureLimits.ContainsKey(texture))textureLimits.Add(texture,texture.ignoreMipmapLimit);texture.ignoreMipmapLimit=false;}
  foreach(var portrait in CPMatch.Instance.portraits)if(portrait!=null){if(!textureLimits.ContainsKey(portrait))textureLimits.Add(portrait,portrait.ignoreMipmapLimit);portrait.ignoreMipmapLimit=true;}
  probes=FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);probeEnabled=new bool[probes.Length];for(int i=0;i<probes.Length;i++)probeEnabled[i]=probes[i].enabled;
  initialized=true;Apply(value.Graphics);gameObject.AddComponent<CPBlobShadows>();
 }
 public void Apply(CPGraphicsQuality quality){Current=quality;if(!initialized)return;bool ultra=quality==CPGraphicsQuality.Ultra;
  QualitySettings.realtimeReflectionProbes=ultra&&originalReflections;
  for(int i=0;i<probes.Length;i++)if(probes[i]!=null)probes[i].enabled=ultra&&probeEnabled[i];
  foreach(var s in surfaces){if(s.renderer==null)continue;var assigned=(Material[])s.materials.Clone();if(!ultra)for(int i=0;i<assigned.Length;i++)if(assigned[i]!=null&&dull.TryGetValue(assigned[i],out var simple))assigned[i]=simple;s.renderer.sharedMaterials=assigned;s.renderer.reflectionProbeUsage=ultra?s.probes:ReflectionProbeUsage.Off;}
  foreach(var p in particles.Values)ApplyParticle(p);discoverAt=lightsAt=0;
 }
 void ApplyParticle(ParticleBudget p){if(p.ps==null)return;bool smoke=p.ps.name.IndexOf("Smoke",System.StringComparison.OrdinalIgnoreCase)>=0;float density=smoke?SmokeDensity:Density;
  var emission=p.ps.emission;emission.rateOverTimeMultiplier=p.rate*density;emission.rateOverDistanceMultiplier=p.distance*density;
  var main=p.ps.main;main.maxParticles=Mathf.Max(8,Mathf.RoundToInt(p.maximum*density));var noise=p.ps.noise;noise.enabled=p.noise&&Current==CPGraphicsQuality.Ultra;
 }
 void Update(){if(!initialized)return;
  if(Time.unscaledTime>=discoverAt){discoverAt=Time.unscaledTime+1;expired.Clear();foreach(var pair in particles)if(pair.Value.ps==null)expired.Add(pair.Key);foreach(var id in expired)particles.Remove(id);
   foreach(var ps in FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include)){if(particles.ContainsKey(ps))continue;var p=new ParticleBudget{ps=ps,rate=ps.emission.rateOverTimeMultiplier,distance=ps.emission.rateOverDistanceMultiplier,maximum=ps.main.maxParticles,noise=ps.noise.enabled};particles.Add(ps,p);ApplyParticle(p);}
  }
  if(Time.unscaledTime<lightsAt)return;lightsAt=Time.unscaledTime+.35f;
  bool ultra=Current==CPGraphicsQuality.Ultra;int budget=Current==CPGraphicsQuality.Low?3:8;var pos=cameraView.transform.position;
  lamps.Sort((a,b)=>((a.light!=null?a.light.transform.position:Vector3.positiveInfinity)-pos).sqrMagnitude.CompareTo(((b.light!=null?b.light.transform.position:Vector3.positiveInfinity)-pos).sqrMagnitude));
  int used=0;foreach(var l in lamps){if(l.light==null)continue;l.light.shadows=ultra?l.shadows:LightShadows.None;if(l.light.type==LightType.Directional){l.light.enabled=l.enabled;continue;}if(ultra){if(!CPWebPerformance.Active)l.light.enabled=l.enabled;continue;}bool visible=l.enabled&&used<budget&&(l.light.transform.position-pos).sqrMagnitude<900;l.light.enabled=visible;if(visible)used++;}
 }
 void OnDestroy(){QualitySettings.realtimeReflectionProbes=originalReflections;foreach(var t in textureLimits)if(t.Key!=null)t.Key.ignoreMipmapLimit=t.Value;foreach(var s in surfaces)if(s.renderer!=null){s.renderer.sharedMaterials=s.materials;s.renderer.reflectionProbeUsage=s.probes;}foreach(var l in lamps)if(l.light!=null){l.light.enabled=l.enabled;l.light.shadows=l.shadows;}if(probes!=null)for(int i=0;i<probes.Length;i++)if(probes[i]!=null)probes[i].enabled=probeEnabled[i];foreach(var m in dull.Values)Destroy(m);}
}
}
