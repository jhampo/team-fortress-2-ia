using UnityEngine;
using UnityEngine.AI;
namespace FpsStage2 {
// Offset short, directly walkable path sections; narrow doors retain the safe centre line.
public static class CPBotSpacing {
 static readonly Vector3[] corners=new Vector3[64];static readonly float[] scales={1f,.65f,.3f};
 public static int Slot(CPActor actor,CPMatch match){int slot=0;foreach(var a in match.actors){if(a==actor)return slot;if(!a.isPlayer&&a.team==actor.team)slot++;}return slot;}
 static bool Clear(Vector3 from,Vector3 candidate,NavMeshAgent agent,out Vector3 result){
  result=candidate;NavMeshHit h;if(!NavMesh.SamplePosition(candidate,out h,.45f,agent.areaMask)||Mathf.Abs(h.position.y-candidate.y)>.55f)return false;
  result=h.position;if(NavMesh.Raycast(from,result,out h,agent.areaMask))return false;
  return !Physics.CheckCapsule(result+Vector3.up*.4f,result+Vector3.up*1.65f,.34f,1,QueryTriggerInteraction.Ignore);
 }
 public static bool TravelTarget(CPActor actor,CPMatch match,NavMeshAgent agent,NavMeshPath path,out Vector3 result){
  result=actor.transform.position;if(path.GetCornersNonAlloc(corners)<2)return false;
  Vector3 first=corners[1],delta=first-actor.transform.position;float distance=delta.magnitude;if(distance<3)return false;
  var direction=Vector3.ProjectOnPlane(delta,Vector3.up).normalized;var right=Vector3.Cross(Vector3.up,direction);var center=Vector3.Lerp(actor.transform.position,first,Mathf.Min(5.5f,distance)/distance);
  int slot=Slot(actor,match);float preferred=((slot%4)-1.5f)*1.2f;
  Vector3 push=Vector3.zero;foreach(var other in match.actors){if(other==actor||!other.Alive||other.team!=actor.team)continue;var away=actor.transform.position-other.transform.position;away.y=0;float d=away.magnitude;if(d>.05f&&d<2.6f)push+=away/d*(2.6f-d)*.65f;}
  preferred=Mathf.Clamp(preferred+Vector3.Dot(push,right),-2.3f,2.3f);
  foreach(float scale in scales){var offset=center+right*(preferred*scale);if(Clear(center,offset,agent,out var candidate)&&Clear(actor.transform.position,candidate,agent,out result))return true;}
  return false;
 }
 public static bool CaptureTarget(CPActor actor,CPMatch match,NavMeshAgent agent,CPZone zone,out Vector3 result){
  result=actor.transform.position;float phase=Slot(actor,match)*2.39996f+Time.time*.57f*(Slot(actor,match)%2==0?1:-1);float best=float.PositiveInfinity;bool found=false;
  for(int i=0;i<12;i++){
   float angle=phase+i*Mathf.PI/6;float radius=zone.radius*(i%2==0?.59f:.35f);var desired=zone.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
   if(!Clear(actor.transform.position,desired,agent,out var p)||!zone.Contains(p))continue;
   float travel=Vector3.Distance(p,actor.transform.position);if(travel<.8f)continue;
   float score=i*.08f+Mathf.Abs(travel-1.8f)*.25f;
   foreach(var other in match.actors){if(other==actor||!other.Alive)continue;float d=Vector3.Distance(p,other.transform.position);score+=Mathf.Max(0,1.25f-d)*4;var brain=other.Brain;if(brain!=null&&brain.CaptureMoving)score+=Mathf.Max(0,1.05f-Vector3.Distance(p,brain.destination))*2;}
   if(score<best){best=score;result=p;found=true;}
  }
  return found;
 }
}
}
