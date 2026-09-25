using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
namespace FpsStage2 {
public static class CPPickupTests {
 static int count;static void Check(bool ok,string message){count++;if(!ok)throw new Exception("CHECK FAILED: "+message);}
 public static string EditorChecks(){
  count=0;var a=UnityEngine.Object.FindObjectsByType<CPActor>(FindObjectsSortMode.None);
  Check(a.Count(x=>!x.isPlayer&&x.team==CPTeam.RED)==7,"RED seven bots");Check(a.Count(x=>!x.isPlayer&&x.team==CPTeam.BLU)==8,"BLU eight bots");Check(a.Count(x=>x.isPlayer&&x.team==CPTeam.RED)==1,"RED player");
  var pickups=UnityEngine.Object.FindObjectsByType<CPCollectible>(FindObjectsSortMode.None);Check(pickups.Length==28,"28 static supplies");Check(pickups.Count(p=>p.kind==CPSupplyKind.HealthLarge)==2,"two full-health kits");
  var m=UnityEngine.Object.FindFirstObjectByType<CPMatch>();var nav=NavMesh.AddNavMeshData(m.navigation);int paths=0;
  try{foreach(var p in pickups){Check(p.respawnSeconds==10,"exact respawn delay");Check(p.lifetimeSeconds==0,"static pickup does not expire");RaycastHit floor;Check(Physics.Raycast(p.transform.position+Vector3.up*.1f,Vector3.down,out floor,.25f,1),"grounded "+p.name);Check(p.GetComponentsInChildren<Collider>().Length==0,"non-blocking pickup");NavMeshHit h;Check(NavMesh.SamplePosition(p.transform.position,out h,.8f,-1),"accessible "+p.name);foreach(var z in m.zones){var path=new NavMeshPath();Check(NavMesh.CalculatePath(h.position,z.position,-1,path)&&path.status==NavMeshPathStatus.PathComplete,"pickup to control point "+p.name);paths++;}}
   foreach(float side in new[]{-1f,1f})foreach(var joint in new[]{new Vector3(38.4f,2.92f,3.66f),new Vector3(54.4f,4.71f,7.66f),new Vector3(78.4f,4.71f,-2.34f),new Vector3(104.4f,4.71f,-2.34f),new Vector3(120.4f,4.71f,9.66f)})for(int ix=-2;ix<=2;ix++)for(int iz=-3;iz<=3;iz++){var point=new Vector3(side*joint.x+ix*.3f,joint.y+.6f,joint.z+iz*.6f);RaycastHit hit;Check(Physics.Raycast(point,Vector3.down,out hit,1.3f,1)&&hit.point.y>=joint.y-.5f,"threshold gap "+point);}
  }finally{nav.Remove();}return count+" scene checks; "+paths+" supply-to-point paths. PASS";
 }
 public static string RuntimeChecks(){
  if(!Application.isPlaying)throw new Exception("Play required");count=0;var m=CPMatch.Instance;m.enabled=false;m.player.enabled=false;m.rules=new ControlPointRules();
  foreach(var a in m.actors.Where(a=>!a.isPlayer)){a.GetComponent<CPBrain>().enabled=false;a.ResetLife();var nav=a.GetComponent<NavMeshAgent>();nav.isStopped=true;}
  foreach(ClassId id in Enum.GetValues(typeof(ClassId))){var a=m.actors.First(x=>!x.isPlayer&&x.characterClass==id);
   foreach(var kind in new[]{CPSupplyKind.HealthSmall,CPSupplyKind.HealthMedium,CPSupplyKind.HealthLarge}){a.health=a.maxHealth*.1f;float expected=Mathf.Min(a.maxHealth,a.health+a.maxHealth*CPCollectible.Fraction(kind));Check(CPCollectible.Grant(a,kind),"grant health");Check(Mathf.Abs(a.health-expected)<.001f,"healing percentage "+id+" "+kind);a.health=a.maxHealth;Check(!CPCollectible.Grant(a,kind),"full health does not consume");}
   foreach(var kind in new[]{CPSupplyKind.AmmoSmall,CPSupplyKind.AmmoMedium,CPSupplyKind.DroppedWeapon}){a.clock=new WeaponClock(id);a.clock.Ammo=0;a.clock.Reserve=0;Check(CPCollectible.Grant(a,kind),"grant ammo");int expected=Mathf.CeilToInt(CPCollectible.ReserveCapacity(id)*CPCollectible.Fraction(kind));Check((id==ClassId.Scout||id==ClassId.Soldier?a.clock.Reserve:a.clock.Ammo)==expected,"ammo amount "+id);if(id==ClassId.Scout||id==ClassId.Soldier)Check(a.clock.Ammo==0,"pickup does not bypass reload");a.clock=new WeaponClock(id);Check(!CPCollectible.Grant(a,kind),"full ammo does not consume");}
  }
  var victim=m.actors.First(a=>!a.isPlayer&&a.characterClass==ClassId.Soldier);var med=UnityEngine.Object.FindObjectsByType<CPCollectible>(FindObjectsSortMode.None).First(p=>p.kind==CPSupplyKind.HealthMedium);victim.health=20;
  double now=Time.timeAsDouble;Check(med.TryCollect(victim,now),"collect medium");Check(!med.Available&&!med.artwork.gameObject.activeSelf,"hide collected kit");Check(Math.Abs(med.ReadyAt-now-10)<1e-7,"10 second deadline");med.Advance(now+9.999);Check(!med.Available,"not before 10 seconds");med.Advance(now+10);Check(med.Available&&med.artwork.gameObject.activeSelf,"visible at 10 seconds");victim.health=20;Check(med.TryCollect(victim,now+10),"second collection");Check(!med.TryCollect(victim,now+10),"only one collection per spawn");
  var attacker=m.actors.First(a=>a.team!=victim.team);victim.Burn(5,attacker);Check(victim.IsBurning,"burning setup");CPCollectible.Grant(victim,CPSupplyKind.HealthSmall);Check(!victim.IsBurning,"health extinguishes");
  return count+" health/ammo/timer checks. PASS (controlled Play session; stop to restore)";
 }
}
}
