using System;
using UnityEngine;
using UnityEngine.AI;
namespace FpsStage2 {
[Serializable] public sealed class CPAlternativeRoute {
 public string name;
 public Vector3[] points;
 public float length;
}
// A retained, bidirectional route plan. Replanning every decision used to make
// bots abandon flanks or oscillate between the nearest tactical nodes.
public sealed class CPRouteNavigator {
 CPAlternativeRoute route;bool reverse;int index;float expires,retry;Vector3 plannedGoal;
 public string ActiveName=>route!=null?route.name:"";
 public int Plans {get;private set;} public int Completed {get;private set;}
 public void Cancel(float delay=3){route=null;retry=Time.time+delay;}
 NavMeshPath scratch;readonly Vector3[] pathCorners=new Vector3[256];
 float PathLength(NavMeshAgent agent,Vector3 from,Vector3 to){
  if(scratch==null)scratch=new NavMeshPath();
  if(!NavMesh.CalculatePath(from,to,agent.areaMask,scratch)||scratch.status!=NavMeshPathStatus.PathComplete)return float.PositiveInfinity;
  int count=scratch.GetCornersNonAlloc(pathCorners);if(count==pathCorners.Length)return float.PositiveInfinity;
  float total=0;for(int i=1;i<count;i++)total+=Vector3.Distance(pathCorners[i-1],pathCorners[i]);return total;
 }
 public bool Waypoint(CPActor actor,CPMatch match,NavMeshAgent agent,CPRole role,Vector3 goal,out Vector3 next){
  next=goal;
  if(route!=null){
   if(Time.time>expires||Vector3.Distance(goal,plannedGoal)>24){Cancel();return false;}
   var p=route.points[index];
   if(Vector3.Distance(actor.transform.position,p)<1.7f){
    index+=reverse?-1:1;
    if(index<0||index>=route.points.Length){Completed++;Cancel(6);return false;}
    p=route.points[index];
   }
   next=p;return true;
  }
  if(Time.time<retry||match.alternativeRoutes==null||match.alternativeRoutes.Length==0)return false;
  retry=Time.time+UnityEngine.Random.Range(8f,13f);
  float direct=PathLength(agent,actor.transform.position,goal);
  if(direct<20||float.IsInfinity(direct))return false;
  float chance=role==CPRole.Flank?1:role==CPRole.Attack?.55f:.28f;
  if(match.difficulty==CPDifficulty.Facil)chance*=.55f;
  if(UnityEngine.Random.value>chance)return false;
  float best=float.PositiveInfinity;CPAlternativeRoute chosen=null;bool back=false;
  foreach(var candidate in match.alternativeRoutes){
   if(candidate.points==null||candidate.points.Length<2)continue;
   for(int direction=0;direction<2;direction++){
    int first=direction==0?0:candidate.points.Length-1,last=direction==0?candidate.points.Length-1:0;
    // Euclidean distances are lower bounds: reject impossible detours before pathfinding.
    if(Vector3.Distance(actor.transform.position,candidate.points[first])>direct+10||Vector3.Distance(candidate.points[last],goal)>direct-8)continue;
    float lead=PathLength(agent,actor.transform.position,candidate.points[first]);
    float tail=PathLength(agent,candidate.points[last],goal);
    if(lead>direct+10||tail>direct-8)continue;
    float total=lead+candidate.length+tail;
    if(total>direct*1.9f+28)continue;
    int traffic=match.RouteUsers(candidate.name);
    float score=total*(1+traffic*.32f)*UnityEngine.Random.Range(.82f,1.18f);
    if(role==CPRole.Flank)score*=candidate.name.Contains("Service")?.85f:.94f;
    if(score<best){best=score;chosen=candidate;back=direction!=0;}
   }
  }
  if(chosen==null)return false;
  route=chosen;reverse=back;index=reverse?route.points.Length-1:0;plannedGoal=goal;
  expires=Time.time+Mathf.Max(18,(best/Mathf.Max(actor.Speed,2))*2+10);Plans++;
  next=route.points[index];return true;
 }
}
}
