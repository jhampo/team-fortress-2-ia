using UnityEngine;
namespace FpsStage2 {
public static class CPGaitProfile {
 const float ScoutContact=.76f/3.7f;
 // Sprint reference: planted stride, heel recovery, knee drive, airborne reach.
 // Matched endpoint velocities keep the feet continuous at contact and looping.
 static readonly AnimationCurve scoutForward=new AnimationCurve(
  new Keyframe(0,.38f,-3.7f,-3.7f),new Keyframe(ScoutContact,-.38f,-3.7f,-3.7f),
  new Keyframe(.34f,-.60f,0,0),new Keyframe(.50f,-.32f,2.7f,2.7f),
  new Keyframe(.68f,.30f,2.8f,2.8f),new Keyframe(.86f,.62f,0,0),new Keyframe(1,.38f,-3.7f,-3.7f));
 static readonly AnimationCurve scoutHeight=new AnimationCurve(
  new Keyframe(0,0,0,0),new Keyframe(ScoutContact,0,0,0),
  new Keyframe(.34f,.30f,2,2),new Keyframe(.50f,.57f,0,0),
  new Keyframe(.68f,.38f,-1.9f,-1.9f),new Keyframe(.86f,.13f,-1,-1),new Keyframe(1,0,0,0));
 static readonly AnimationCurve scoutPitch=new AnimationCurve(
  new Keyframe(0,-5,0,0),new Keyframe(.06f,0,0,0),new Keyframe(ScoutContact,22,0,0),
  new Keyframe(.42f,45,0,0),new Keyframe(.68f,8,0,0),new Keyframe(.86f,-14,0,0),new Keyframe(1,-5,0,0));
 public static float ScoutPelvisBob(float phase)=>.014f*Mathf.Cos(4*Mathf.PI*(phase-.08f));
 public static float ScoutFootPitch(float phase)=>scoutPitch.Evaluate(Mathf.Repeat(phase,1));
 public static float CycleDistance(ClassId id,bool walk)=>walk?.92f:id==ClassId.Scout?3.7f:id==ClassId.Heavy?2.8f:id==ClassId.Pyro?2.7f:2.55f;
 public static float HalfStride(ClassId id,bool walk)=>walk?.27f:id==ClassId.Scout?.61f:id==ClassId.Heavy?.43f:id==ClassId.Pyro?.39f:.37f;
 public static float FootWidth(ClassId id)=>id==ClassId.Scout?.20f:id==ClassId.Heavy?.36f:id==ClassId.Pyro?.26f:.24f;
 public static float FootLift(ClassId id,bool walk)=>walk?.085f:id==ClassId.Scout?.36f:id==ClassId.Heavy?.13f:id==ClassId.Pyro?.19f:.18f;
 public static float Lean(ClassId id,bool walk)=>(id==ClassId.Scout?8:id==ClassId.Heavy?2.5f:id==ClassId.Pyro?6:5)*(walk?.55f:1);
 // The planted foot travels exactly the distance covered by the root during stance.
 public static Vector2 FootPath(ClassId id,bool walk,float phase){
  phase=Mathf.Repeat(phase,1);
  if(id==ClassId.Scout&&!walk)return new Vector2(scoutForward.Evaluate(phase),scoutHeight.Evaluate(phase));
  float reach=HalfStride(id,walk),stance=2*reach/CycleDistance(id,walk);
  if(phase<stance)return new Vector2(Mathf.Lerp(reach,-reach,phase/stance),0);
  float swing=(phase-stance)/(1-stance);return new Vector2(Mathf.Lerp(-reach,reach,Mathf.SmoothStep(0,1,swing)),FootLift(id,walk)*Mathf.Pow(Mathf.Sin(swing*Mathf.PI),1.5f));
 }
}
}
