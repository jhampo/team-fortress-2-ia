using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace FpsStage2 {
public static class CPPresentationTests {
 static int count;static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
 public static string Run(){
  count=0;var clock=new WeaponClock(ClassId.Heavy);
  Check(Math.Abs(clock.SpreadTangent(0)-WeaponClock.HeavyMinSpread)<1e-5,"Cold Heavy spread");
  clock.Step(0,true,false,false);Check(clock.HeavyHeatAt(1)==0,"Spinning alone does not widen spread");
  float previous=0;for(int i=0;i<12;i++){double now=1.1+i*.105;Check(clock.Step(now,true,false,false)==1,"Heavy cadence "+i);float spread=clock.SpreadTangent(now);Check(spread>previous&&spread<=WeaponClock.HeavyMaxSpread+1e-5,"Progressive bounded spread "+i);previous=spread;}
  Check(Math.Abs(previous-WeaponClock.HeavyMaxSpread)<1e-5,"Original maximum spread preserved");
  Check(Math.Abs(clock.SpreadTangent(4)-WeaponClock.HeavyMinSpread)<1e-5,"Spread recovers after firing");
  Check(Math.Abs(new WeaponClock(ClassId.Scout).SpreadTangent(0)-1f/30)<1e-5,"Scout spread unchanged");
  foreach(var brain in UnityEngine.Object.FindObjectsByType<CPBrain>(FindObjectsSortMode.None)){
   var id=brain.GetComponent<CPActor>().characterClass;var a=brain.animations;var bones=a.GetComponentsInChildren<Transform>(true);
   var legs=new[]{new[]{"LeftUpLeg","LeftLeg","LeftFoot","LeftToeBase"},new[]{"RightUpLeg","RightLeg","RightFoot","RightToeBase"}}.Select(names=>names.Select(n=>bones.First(b=>b.name==n)).ToArray()).ToArray();
   var hand=bones.Where(t=>t.name=="LeftHand"||t.name=="RightHand").ToArray();var weapon=bones.First(t=>t.name.EndsWith("_Weapon"));
   a["Idle"].clip.SampleAnimation(a.gameObject,0);var grip=hand.Select(t=>weapon.InverseTransformPoint(t.position)).ToArray();
   var lengths=legs.Select(l=>new[]{Vector3.Distance(l[0].position,l[1].position),Vector3.Distance(l[1].position,l[2].position)}).ToArray();
   try{foreach(bool walk in new[]{false,true}){
    var clip=a[walk?"Walk":"Run"].clip;Check(clip.legacy&&Math.Abs(clip.length-1)<1e-5,brain.name+" clip length");
    clip.SampleAnimation(a.gameObject,0);var first=bones.Select(t=>t.localRotation).ToArray();
    for(int f=0;f<=64;f++){
     clip.SampleAnimation(a.gameObject,f/64f);
     for(int leg=0;leg<2;leg++){
      var l=legs[leg];for(int segment=0;segment<2;segment++)Check(Math.Abs(Vector3.Distance(l[segment].position,l[segment+1].position)-lengths[leg][segment])<.001f,brain.name+" no stretched legs");
      Check(brain.transform.InverseTransformPoint(l[3].position).y>-.005f,brain.name+" toe above ground");
     }
     for(int h=0;h<hand.Length;h++)Check(Vector3.Distance(weapon.InverseTransformPoint(hand[h].position),grip[h])<.0001f,brain.name+" preserved weapon grip");
     float width=Math.Abs(brain.transform.InverseTransformPoint(legs[0][2].position).x-brain.transform.InverseTransformPoint(legs[1][2].position).x);
     Check(Math.Abs(width-CPGaitProfile.FootWidth(id))<.008f,brain.name+" narrow stance");
    }
    for(int b=0;b<bones.Length;b++)Check(Quaternion.Angle(first[b],bones[b].localRotation)<.1f,brain.name+" seamless cycle "+bones[b].name);
    float travel=CPGaitProfile.FootPath(id,walk,.08f).x-CPGaitProfile.FootPath(id,walk,.12f).x;
    Check(Math.Abs(travel/.04f-CPGaitProfile.CycleDistance(id,walk))<.001f,brain.name+" stance/root speed agreement");
   }}finally{a["Idle"].clip.SampleAnimation(a.gameObject,0);}
  }
  return count+" presentation/weapon/gait checks passed";
 }
}
}
