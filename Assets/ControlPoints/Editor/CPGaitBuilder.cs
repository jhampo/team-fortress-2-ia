using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace FpsStage2 {
public static class CPGaitBuilder {
 const string Folder="Assets/ControlPoints/Animations/";
 const int Samples=64;
 static readonly string[] Channels={"localRotation.x","localRotation.y","localRotation.z","localRotation.w","localPosition.x","localPosition.y","localPosition.z"};
 sealed class Leg {
  public Transform upper,knee,foot,toe;public float thigh,shin,ankleY;public Quaternion flatRotation;public int side;
  public Leg(Transform[] bones,Transform root,string prefix,int sign){
   side=sign;upper=bones.First(t=>t.name==prefix+"UpLeg");knee=bones.First(t=>t.name==prefix+"Leg");foot=bones.First(t=>t.name==prefix+"Foot");toe=bones.First(t=>t.name==prefix+"ToeBase");
   thigh=Vector3.Distance(upper.position,knee.position);shin=Vector3.Distance(knee.position,foot.position);ankleY=root.InverseTransformPoint(foot.position).y;
   var heading=Vector3.ProjectOnPlane(toe.position-foot.position,root.up).normalized;
   flatRotation=Quaternion.FromToRotation(heading,root.forward)*foot.rotation;
  }
  public void Solve(Transform root,Vector3 target,float pitch){
   Vector3 origin=upper.position,delta=target-origin;float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(thigh-shin)+.001f,(thigh+shin)*.987f);Vector3 dir=delta.normalized;
   Vector3 pole=Vector3.ProjectOnPlane(root.forward+root.right*(side*.07f),dir).normalized;
   float along=(thigh*thigh-shin*shin+d*d)/(2*d);Vector3 bend=origin+dir*along+pole*Mathf.Sqrt(Mathf.Max(0,thigh*thigh-along*along));
   upper.rotation=Quaternion.FromToRotation(knee.position-origin,bend-origin)*upper.rotation;
   knee.rotation=Quaternion.FromToRotation(foot.position-knee.position,origin+dir*d-knee.position)*knee.rotation;
   foot.rotation=Quaternion.AngleAxis(pitch,root.right)*flatRotation;
  }
 }
 [MenuItem("Powerhouse/Rebuild Bot Locomotion")]
 public static string RebuildAll(){
  if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode before rebuilding.");
  var bots=UnityEngine.Object.FindObjectsByType<CPBrain>(FindObjectsSortMode.None);
  foreach(var brain in bots)Build(brain,brain.GetComponent<CPActor>().characterClass);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
  return "Rebuilt walk/run/jump for "+bots.Length+" bots. Idle, arms and weapons preserved.";
 }
 [MenuItem("Powerhouse/Rebuild Scout Run Legs Only")]
 public static string RebuildScoutRun(){
  if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode before rebuilding.");
  var bots=UnityEngine.Object.FindObjectsByType<CPBrain>(FindObjectsSortMode.None).Where(b=>b.GetComponent<CPActor>().characterClass==ClassId.Scout).ToArray();
  foreach(var brain in bots)Build(brain,ClassId.Scout,true);
  AssetDatabase.SaveAssets();return "Scout run legs rebuilt for "+bots.Length+" bots; upper body, walk, jump and other classes preserved.";
 }
 public static void Build(CPBrain brain,ClassId id,bool scoutRunOnly=false){
  var a=brain.animations;var idle=a["Idle"].clip;var root=brain.transform;idle.SampleAnimation(a.gameObject,0);
  var bones=a.GetComponentsInChildren<Transform>(true).Where(t=>t!=a.transform).ToArray();
  var positions=bones.Select(t=>t.localPosition).ToArray();var rotations=bones.Select(t=>t.localRotation).ToArray();
  var hips=bones.First(t=>t.name=="Hips");var chest=bones.First(t=>t.name=="Spine02");
  var legs=new[]{new Leg(bones,root,"Left",-1),new Leg(bones,root,"Right",1)};
  Vector3 restHip=root.InverseTransformPoint(hips.position);
  foreach(bool walk in new[]{false,true}){
   if(scoutRunOnly&&walk)continue;
   for(int b=0;b<bones.Length;b++){bones[b].localPosition=positions[b];bones[b].localRotation=rotations[b];}
   string name=walk?"Walk":"Run",path=Folder+id+"_"+name+".anim";
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
   if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
   if(!scoutRunOnly)clip.ClearCurves();clip.name=id+"_"+name;clip.legacy=true;clip.wrapMode=WrapMode.Loop;clip.frameRate=60;
   var curves=bones.Select(t=>Enumerable.Range(0,7).Select(_=>new AnimationCurve()).ToArray()).ToArray();
   float half=CPGaitProfile.HalfStride(id,walk),width=CPGaitProfile.FootWidth(id),bob=walk?.009f:id==ClassId.Scout?.026f:id==ClassId.Heavy?.014f:.019f;
   float pelvisY=restHip.y+(walk?.035f:.015f);
   foreach(var leg in legs){
    float upperOffset=root.InverseTransformPoint(leg.upper.position).y-restHip.y;
    float length=(leg.thigh+leg.shin)*.96f;
    pelvisY=Mathf.Min(pelvisY,leg.ankleY+Mathf.Sqrt(Mathf.Max(.01f,length*length-half*half))-upperOffset-bob);
   }
   if(id==ClassId.Scout&&!walk){
    // Find one reachable pelvis baseline for the entire cycle. The previous
    // per-frame minimum switched supporting legs abruptly and shook the body.
    pelvisY=restHip.y-.002f;
    for(int f=0;f<512;f++)foreach(var leg in legs){
     float phase=f/512f;var fp=CPGaitProfile.FootPath(id,false,phase+(leg.side==1?.5f:0));
     var upper=root.InverseTransformPoint(leg.upper.position);float dx=leg.side*width*.5f-upper.x-Mathf.Sin(phase*Mathf.PI*2)*.007f,dz=restHip.z+fp.x-upper.z;
     float length=(leg.thigh+leg.shin)*.985f;
     float limit=leg.ankleY+fp.y+Mathf.Sqrt(Mathf.Max(.01f,length*length-dx*dx-dz*dz))-(upper.y-restHip.y);
     pelvisY=Mathf.Min(pelvisY,limit-CPGaitProfile.ScoutPelvisBob(phase));
    }
   }
   for(int f=0;f<=Samples;f++){
    float t=f/(float)Samples,angle=t*Mathf.PI*2;
    for(int b=0;b<bones.Length;b++){bones[b].localPosition=positions[b];bones[b].localRotation=rotations[b];}
    float rise=id==ClassId.Scout&&!walk?CPGaitProfile.ScoutPelvisBob(t):bob*Mathf.Cos(angle*2);
    hips.position=root.TransformPoint(new Vector3(restHip.x+Mathf.Sin(angle)*(id==ClassId.Heavy?.014f:.007f),pelvisY+rise,restHip.z));
    chest.rotation=Quaternion.AngleAxis(Mathf.Sin(angle)*(id==ClassId.Soldier||id==ClassId.Pyro?1.8f:1),root.up)*Quaternion.AngleAxis(CPGaitProfile.Lean(id,walk)+Mathf.Cos(angle*2)*.6f,root.right)*Quaternion.AngleAxis(Mathf.Sin(angle)*(id==ClassId.Heavy?1.6f:.65f),root.forward)*chest.rotation;
    for(int i=0;i<2;i++){
     var leg=legs[i];float phase=Mathf.Repeat(t+i*.5f,1);var foot=CPGaitProfile.FootPath(id,walk,phase);
     float stance=2*half/CPGaitProfile.CycleDistance(id,walk),swing=Mathf.Clamp01((phase-stance)/(1-stance));
     float contact=Mathf.Clamp01(phase/stance);
     float pitch=phase<stance?Mathf.Lerp(-5,0,Mathf.SmoothStep(0,1,contact/.22f))+12*Mathf.SmoothStep(0,1,(contact-.72f)/.28f):Mathf.Lerp(12,-5,Mathf.SmoothStep(0,1,swing));
     if(id==ClassId.Scout&&!walk)pitch=CPGaitProfile.ScoutFootPitch(phase);
     leg.Solve(root,root.TransformPoint(new Vector3(leg.side*width*.5f,leg.ankleY+foot.y,restHip.z+foot.x)),pitch);
    }
    for(int b=0;b<bones.Length;b++){var q=bones[b].localRotation;var p=bones[b].localPosition;float[] v={q.x,q.y,q.z,q.w,p.x,p.y,p.z};for(int c=0;c<7;c++)curves[b][c].AddKey(t,v[c]);}
   }
   for(int b=0;b<bones.Length;b++)for(int c=0;c<7;c++){
    if(scoutRunOnly&&!(bones[b]==hips&&c>=4)&&!(c<4&&legs.Any(l=>bones[b]==l.upper||bones[b]==l.knee||bones[b]==l.foot)))continue;
    var curve=curves[b][c];for(int k=0;k<curve.length;k++)curve.SmoothTangents(k,0);
    var keys=curve.keys;float tangent=(keys[1].value-keys[Samples-1].value)/(2f/Samples);
    var first=keys[0];first.inTangent=first.outTangent=tangent;curve.MoveKey(0,first);var last=keys[Samples];last.inTangent=last.outTangent=tangent;curve.MoveKey(Samples,last);
    clip.SetCurve(AnimationUtility.CalculateTransformPath(bones[b],a.transform),typeof(Transform),Channels[c],curve);
   }
   clip.EnsureQuaternionContinuity();EditorUtility.SetDirty(clip);a.AddClip(clip,name);a[name].wrapMode=WrapMode.Loop;
  }
  idle.SampleAnimation(a.gameObject,0);if(!scoutRunOnly)BuildJump(brain,id);idle.SampleAnimation(a.gameObject,0);a.playAutomatically=true;EditorUtility.SetDirty(a);
 }
 static void BuildJump(CPBrain brain,ClassId id){
  var a=brain.animations;var root=brain.transform;var idle=a["Idle"].clip;idle.SampleAnimation(a.gameObject,0);
  var bones=a.GetComponentsInChildren<Transform>(true).Where(t=>t!=a.transform).ToArray();var positions=bones.Select(t=>t.localPosition).ToArray();var rotations=bones.Select(t=>t.localRotation).ToArray();
  var hips=bones.First(t=>t.name=="Hips");var chest=bones.First(t=>t.name=="Spine02");var restHip=root.InverseTransformPoint(hips.position);
  var legs=new[]{new Leg(bones,root,"Left",-1),new Leg(bones,root,"Right",1)};var restFeet=legs.Select(l=>root.InverseTransformPoint(l.foot.position)).ToArray();
  string path=Folder+id+"_Jump.anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
  clip.ClearCurves();clip.name=id+"_Jump";clip.legacy=true;clip.wrapMode=WrapMode.ClampForever;clip.frameRate=60;
  var curves=bones.Select(t=>Enumerable.Range(0,7).Select(_=>new AnimationCurve()).ToArray()).ToArray();
  var crouch=new AnimationCurve(new Keyframe(0,0),new Keyframe(.10f,-.07f),new Keyframe(.22f,.025f),new Keyframe(.72f,.018f),new Keyframe(.92f,-.09f),new Keyframe(1,0));
  var tuck=new AnimationCurve(new Keyframe(0,0),new Keyframe(.2f,0),new Keyframe(.48f,1),new Keyframe(.64f,.85f),new Keyframe(.83f,0),new Keyframe(1,0));
  for(int f=0;f<=Samples;f++){
   float t=f/(float)Samples,morph=Mathf.SmoothStep(0,1,t/.17f)*(1-Mathf.SmoothStep(0,1,(t-.85f)/.15f));
   for(int b=0;b<bones.Length;b++){bones[b].localPosition=positions[b];bones[b].localRotation=rotations[b];}
   hips.position=root.TransformPoint(restHip+Vector3.up*crouch.Evaluate(t));
   chest.rotation=Quaternion.AngleAxis((id==ClassId.Heavy?3:6)*morph-crouch.Evaluate(t)*35,root.right)*chest.rotation;
   for(int i=0;i<2;i++){
    var leg=legs[i];var upper=leg.upper.localRotation;var knee=leg.knee.localRotation;var foot=leg.foot.localRotation;
    float lift=tuck.Evaluate(t)*(id==ClassId.Scout?.28f:id==ClassId.Heavy?.13f:.20f);
    Vector3 target=new Vector3(leg.side*CPGaitProfile.FootWidth(id)*.5f,leg.ankleY+lift,restHip.z+(id==ClassId.Scout?(i==0?.20f:-.18f):i==0?.09f:-.07f)*tuck.Evaluate(t));
    leg.Solve(root,root.TransformPoint(Vector3.Lerp(restFeet[i],target,morph)),10*tuck.Evaluate(t));
    leg.upper.localRotation=Quaternion.Slerp(upper,leg.upper.localRotation,morph);leg.knee.localRotation=Quaternion.Slerp(knee,leg.knee.localRotation,morph);leg.foot.localRotation=Quaternion.Slerp(foot,leg.foot.localRotation,morph);
   }
   for(int b=0;b<bones.Length;b++){var q=bones[b].localRotation;var p=bones[b].localPosition;float[] v={q.x,q.y,q.z,q.w,p.x,p.y,p.z};for(int c=0;c<7;c++)curves[b][c].AddKey(t,v[c]);}
  }
  for(int b=0;b<bones.Length;b++)for(int c=0;c<7;c++){var curve=curves[b][c];for(int k=0;k<curve.length;k++)curve.SmoothTangents(k,0);clip.SetCurve(AnimationUtility.CalculateTransformPath(bones[b],a.transform),typeof(Transform),Channels[c],curve);}
  clip.EnsureQuaternionContinuity();EditorUtility.SetDirty(clip);a.AddClip(clip,"Jump");a["Jump"].wrapMode=WrapMode.ClampForever;idle.SampleAnimation(a.gameObject,0);
 }

}
}
