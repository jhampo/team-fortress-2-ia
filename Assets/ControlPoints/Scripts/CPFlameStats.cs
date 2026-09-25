using UnityEngine;
namespace FpsStage2 {
public enum CPFlamePower { Normal,MiniCritical,Critical }
public static class CPFlameStats {
 public const float AttackInterval=.075f,AmmoInterval=.08f,AirblastInterval=.75f,Range=5.75f;
 // The requested per-particle ranges. Full contact keeps the existing weapon's maximum.
 public static float ParticleDamage(float range01,float strength,CPFlamePower power=CPFlamePower.Normal){
  strength=Mathf.Clamp01(strength);
  if(power==CPFlamePower.Critical)return Mathf.Lerp(9.75f,19.5f,strength);
  if(power==CPFlamePower.MiniCritical)return Mathf.Lerp(4.3875f,8.775f,strength);
  return Mathf.Lerp(6.5f,13,strength)*Mathf.Lerp(1,.5f,Mathf.Clamp01(range01));
 }
 public static float BurnDuration(float range01)=>Mathf.Lerp(10,4,Mathf.Clamp01(range01));
}
}
