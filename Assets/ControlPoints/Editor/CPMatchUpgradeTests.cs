using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace FpsStage2 {
public static class CPMatchUpgradeTests {
 static int assertions;static void Check(bool b,string label){assertions++;if(!b)throw new Exception("Match upgrade: "+label);}
 public static string Geometry(){assertions=0;var m=UnityEngine.Object.FindFirstObjectByType<CPMatch>();Check(m.spawnGates.Length==2,"two gates");Check(m.teamBases.Length==2,"two base volumes");
  for(int i=0;i<2;i++){float sign=i==0?1:-1;var z=m.zones[i==0?0:2];Check(Mathf.Abs(z.position.x-sign*92)<.001f&&Mathf.Abs(z.position.z+4.84f)<.001f,"hatch centred "+i);Check((z.visual.position-z.position).sqrMagnitude<.001f,"capture and visuals aligned");Check(m.spawnGates[i].barrier!=null,"physical shutter");foreach(var a in UnityEngine.Object.FindObjectsByType<CPActor>(FindObjectsSortMode.None).Where(a=>a.team==(i==0?CPTeam.RED:CPTeam.BLU)))Check(m.teamBases[i].Contains(a.transform.position+Vector3.up*.5f),"spawn within own base "+a.name);}
  foreach(var gate in m.spawnGates){var script=new UnityEditor.SerializedObject(gate).FindProperty("m_Script").objectReferenceValue;Check(UnityEditor.AssetDatabase.GetAssetPath(script)=="Assets/ControlPoints/Scripts/CPSpawnGate.cs","persistent gate script asset");}
  var fences=GameObject.Find("Gameplay_Fence_Collisions").GetComponentsInChildren<BoxCollider>();Check(fences.Length==4,"four fence panels");foreach(var f in fences){Check(f.enabled&&!f.isTrigger,"solid fence");var b=f.bounds;bool x=b.size.x<b.size.z;Vector3 axis=x?Vector3.right:Vector3.forward;Check(f.Raycast(new Ray(b.center-axis*2,axis),out var hit,4),"fence ray blocked");}
  var instance=NavMesh.AddNavMeshData(m.navigation);try{foreach(var a in UnityEngine.Object.FindObjectsByType<CPActor>(FindObjectsSortMode.None))foreach(var z in m.zones){var p=new NavMeshPath();Check(NavMesh.CalculatePath(a.transform.position,z.position,-1,p)&&p.status==NavMeshPathStatus.PathComplete,"open route from spawn");}}finally{instance.Remove();}
  return assertions+" geometry, base and navigation checks passed";
 }
 public static string Runtime(){assertions=0;var m=CPMatch.Instance;Check(m!=null&&m.RoundLive,"live match required");Check(m.actors.Length==16,"16 characters");Check(m.actors.Select(a=>a.score.Name).Distinct().Count()==16,"unique names");Check(m.PlayerActor.score.Name=="player-astra-gpt","player name");Check(m.spawnGates.All(g=>!g.barrier.enabled),"gates open");var p=m.PlayerActor;p.ResetLife();int deaths=p.score.Deaths;
  var first=p.characterClass==ClassId.Scout?ClassId.Soldier:ClassId.Scout;m.flow.ChooseClass(first);Check(p.Alive&&p.characterClass==first&&p.score.Deaths==deaths,"safe base class change");Check(Mathf.Abs(p.health-p.maxHealth)<.01f,"class health");
  var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=new Vector3(113,4.75f,5);cc.enabled=true;Check(!m.flow.InOwnBase(p),"outside base");var second=ClassId.Pyro;m.flow.ChooseClass(second);Check(!p.Alive&&m.flow.PendingClass==second,"deferred class outside base");Check(p.score.Deaths==deaths+1,"class change counts death");Check(Mathf.Abs(p.respawnAt-Time.time-m.rules.RespawnDelay(p.team))<.01f,"respawn delay");p.ResetLife();Check(p.Alive&&p.characterClass==second&&m.flow.InOwnBase(p),"requested class after respawn");
  var enemy=m.actors.First(a=>a.team==CPTeam.BLU);var ally=m.actors.First(a=>a.team==CPTeam.RED&&!a.isPlayer);enemy.ResetLife();int kills=p.score.Kills,assist=ally.score.Assists,enemyDeaths=enemy.score.Deaths;enemy.Damage(10,ally);enemy.Damage(10000,p,false,null,false,true);Check(p.score.Kills==kills+1,"kill credited");Check(ally.score.Assists==assist+1,"assist credited");Check(enemy.score.Deaths==enemyDeaths+1,"death counted once");enemy.Damage(10000,p);Check(enemy.score.Deaths==enemyDeaths+1,"no duplicate death");Check(p.score.Points==p.score.Kills*2+p.score.Assists+3*p.score.Captures,"score formula");
  bool muted=m.audioSystem.Muted;m.audioSystem.ToggleMute();Check(m.audioSystem.Muted!=muted,"mute toggled");Check(Mathf.Abs(AudioListener.volume-(muted?1:0))<.001f,"master mute applied");m.audioSystem.ToggleMute();Check(m.audioSystem.Muted==muted,"mute restored");
  Check(UnityEngine.Object.FindObjectsByType<CPFragmentMotion>(FindObjectsSortMode.None).Length>=8,"rocket death fragments");Check(m.audioSystem.SoundsPlayed>0,"sound events emitted");Check(m.audioSystem.DownloadedClipCount==96&&m.audioSystem.BankComplete,"96 downloaded sound clips and complete cue mappings");
  return assertions+" runtime checks passed";
 }
}
}
