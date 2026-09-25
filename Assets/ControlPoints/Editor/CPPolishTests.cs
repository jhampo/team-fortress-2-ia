using System;
using System.Linq;
using UnityEngine;
namespace FpsStage2 {
public static class CPPolishTests {
 public static string EditorChecks(){
  int checks=0;Action<bool,string> check=(ok,label)=>{checks++;if(!ok)throw new Exception("Polish check failed: "+label);};
  var rules=new ControlPointRules();check(rules.CenterSeconds==24&&rules.BaseSeconds==32,"four-times capture duration");
  for(int count=1;count<=8;count++){check(Mathf.Abs(rules.CaptureSeconds(1,count)-24*Mathf.Pow(.75f,count-1))<.001f,"centre squad timing");check(Mathf.Abs(rules.CaptureSeconds(0,count)-32*Mathf.Pow(.75f,count-1))<.001f,"base squad timing");}
  rules.Tick(23.99f,new[]{0,1,0},new int[3]);check(rules.Points[1].Owner==CPTeam.Neutral,"not captured early");rules.Tick(.011f,new[]{0,1,0},new int[3]);check(rules.Points[1].Owner==CPTeam.RED,"centre captured at 24 seconds");
  check(CPDeathFall.BodyDuration<=.35f,"abrupt fall");check(CPDeathPresentation.DropLifetime==6,"six-second remains");
  foreach(var actor in UnityEngine.Object.FindObjectsByType<CPActor>(FindObjectsSortMode.None).Where(a=>!a.isPlayer)){
   foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()){check(skin.sharedMesh.isReadable,"fragment skin readable "+actor.name);check(skin.sharedMesh.boneWeights.Length==skin.sharedMesh.vertexCount,"skin weights available");}
   foreach(var filter in actor.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.EndsWith("_Weapon")||f.name=="RotatingBarrels"))check(filter.sharedMesh.isReadable,"dropped weapon readable "+actor.name);
  }
  return checks+" capture/death/fragment readiness checks passed";
 }
}
}
