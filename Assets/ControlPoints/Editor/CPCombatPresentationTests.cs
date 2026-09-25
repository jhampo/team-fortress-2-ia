using System;
using System.Linq;
using UnityEngine;
namespace FpsStage2 {
public static class CPCombatPresentationTests {
 static int count;static void Check(bool condition,string label){if(!condition)throw new Exception("FAILED: "+label);count++;}
 static bool Near(float a,float b,float tolerance=.002f)=>Mathf.Abs(a-b)<tolerance;
 public static string EditorChecks(){
  count=0;
  Check(Near(CPDamageOverlay.Bearing(Vector3.forward,Vector3.forward),0),"front damage above reticle");
  Check(Near(CPDamageOverlay.Bearing(Vector3.forward,Vector3.right),Mathf.PI/2),"right damage on right");
  Check(Near(Mathf.Abs(CPDamageOverlay.Bearing(Vector3.forward,Vector3.back)),Mathf.PI),"rear damage below");
  Check(Near(CPDamageOverlay.Bearing(Vector3.forward,Vector3.left),-Mathf.PI/2),"left damage on left");
  Check(Near(CPDamageOverlay.Bearing(Vector3.right,Vector3.right),0),"damage rotates with camera");
  Check(Near(CPFlameStats.ParticleDamage(0,0),6.5f)&&Near(CPFlameStats.ParticleDamage(0,1),13),"close particle damage");
  Check(Near(CPFlameStats.ParticleDamage(1,0),3.25f)&&Near(CPFlameStats.ParticleDamage(1,1),6.5f),"far particle damage");
  Check(Near(CPFlameStats.ParticleDamage(0,0,CPFlamePower.Critical),9.75f)&&Near(CPFlameStats.ParticleDamage(0,1,CPFlamePower.Critical),19.5f),"critical particle range");
  Check(Near(CPFlameStats.ParticleDamage(0,0,CPFlamePower.MiniCritical),4.3875f)&&Near(CPFlameStats.ParticleDamage(0,1,CPFlamePower.MiniCritical),8.775f),"minicritical particle range");
  Check(Near(CPFlameStats.BurnDuration(0),10)&&Near(CPFlameStats.BurnDuration(1),4),"burn duration bounds");
  Check(Near((float)new WeaponClock(ClassId.Pyro).Interval,CPFlameStats.AttackInterval),"attack interval unchanged");
  foreach(ClassId id in Enum.GetValues(typeof(ClassId))){
   var jump=new CPJumpMotion();float height=id==ClassId.Scout?1.44f:1.2f;Check(jump.Start(height,id==ClassId.Scout),"all classes can jump");
   bool air=false,land=false;float peak=0;for(int i=0;i<600;i++){jump.Tick(1f/120);air|=jump.Phase==CPJumpPhase.Airborne;land|=jump.Phase==CPJumpPhase.Landing;peak=Mathf.Max(peak,jump.Height);Check(jump.Height>=0&&jump.ClipTime>=0&&jump.ClipTime<=1,"bounded jump motion");if(!jump.Busy)break;}
   Check(air&&land&&!jump.Busy&&jump.Height==0,"complete takeoff flight landing "+id);
   Check(Mathf.Abs(peak-height*(id==ClassId.Scout?2:1))<.04f,"physical jump apex "+id);
   Check(jump.DoubleUsed==(id==ClassId.Scout),"Scout-only double jump");
  }
  foreach(var brain in UnityEngine.Object.FindObjectsByType<CPBrain>(FindObjectsSortMode.None)){
   var a=brain.animations;var bones=a.GetComponentsInChildren<Transform>();var idle=a["Idle"].clip;var jump=a["Jump"].clip;Check(jump.wrapMode==WrapMode.ClampForever,"jump is not a looping idle");
   idle.SampleAnimation(a.gameObject,0);var rest=bones.Select(t=>t.localRotation).ToArray();
   try{foreach(float time in new[]{0f,1f}){jump.SampleAnimation(a.gameObject,time);for(int b=0;b<bones.Length;b++)Check(Quaternion.Angle(rest[b],bones[b].localRotation)<.15f,"jump starts and ends in supported pose");}
    if(brain.GetComponent<CPActor>().characterClass==ClassId.Scout){var hip=bones.First(b=>b.name=="LeftUpLeg");var knee=bones.First(b=>b.name=="LeftLeg");var foot=bones.First(b=>b.name=="LeftFoot");float min=180;for(int f=0;f<=64;f++){a["Run"].clip.SampleAnimation(a.gameObject,f/64f);min=Mathf.Min(min,Vector3.Angle(knee.position-hip.position,foot.position-knee.position));}Check(min<25,"Scout extends knee on long stride");}
   }finally{idle.SampleAnimation(a.gameObject,0);}
  }
  return count+" jump/direction/fire specification checks passed";
 }
 public static string RuntimeBurn(){
  if(!Application.isPlaying||Time.time<4.1f)throw new Exception("Run in Play mode after 4.1 seconds.");
  count=0;var go=new GameObject("CP_QA_BurnTarget");var sourceGo=new GameObject("CP_QA_FlameSource");var target=go.AddComponent<CPActor>();var source=sourceGo.AddComponent<CPActor>();
  try{
   target.team=CPTeam.BLU;target.characterClass=ClassId.Soldier;target.Initialize();source.team=CPTeam.RED;source.characterClass=ClassId.Pyro;source.Initialize();
   target.Burn(4,source);Check(target.IsBurning&&Near(target.nextBurn-Time.time,.5f),"first burn tick after half second");
   float next=target.nextBurn;target.Burn(10,source);Check(Near(next,target.nextBurn),"reignite preserves tick phase");
   target.burnUntil=Time.time;target.nextBurn=Time.time-3.5f;float hp=target.health;target.Tick();Check(Near(hp-target.health,32)&&!target.IsBurning,"four seconds delivers eight ticks and extinguishes");
   target.Burn(4,source,true);target.nextBurn=Time.time;hp=target.health;target.Tick();Check(Near(hp-target.health,5),"mini afterburn tick five HP");
   target.HealAndSupply();Check(!target.IsBurning&&target.nextBurn==0,"supply clears fire state");
   target.characterClass=ClassId.Pyro;target.Burn(10,source);Check(!target.IsBurning,"Pyro afterburn immunity preserved");
   target.characterClass=ClassId.Soldier;target.team=CPTeam.RED;target.Burn(10,source);Check(!target.IsBurning,"no friendly ignition");target.team=CPTeam.BLU;
   target.Burn(10,source);target.Damage(1000,source);Check(!target.Alive&&!target.IsBurning,"death extinguishes");target.ResetLife();Check(target.Alive&&!target.IsBurning,"respawn clean");
   return count+" burn lifecycle checks passed";
  }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(sourceGo);}
 }
}
}
