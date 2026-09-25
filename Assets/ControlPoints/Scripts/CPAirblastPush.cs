using UnityEngine;
using UnityEngine.AI;
namespace FpsStage2 {
// Short impulse using the existing movement collision, never teleporting through walls.
[DefaultExecutionOrder(400)] public sealed class CPAirblastPush:MonoBehaviour {
 Vector3 velocity;float remaining;CPActor actor;NavMeshAgent agent;CharacterController controller;
 public void Apply(Vector3 direction){actor=GetComponent<CPActor>();agent=GetComponent<NavMeshAgent>();controller=GetComponent<CharacterController>();velocity=Vector3.ProjectOnPlane(direction,Vector3.up).normalized*12;remaining=.5f;}
 void LateUpdate(){if(remaining<=0||actor==null||!actor.Alive)return;float dt=Mathf.Min(Time.deltaTime,remaining);remaining-=dt;Vector3 step=velocity*dt;velocity*=Mathf.Exp(-3*dt);
  if(controller!=null&&controller.enabled){controller.Move(step);return;}
  if(agent==null||!agent.enabled||!agent.isOnNavMesh)return;
  float distance=step.magnitude;if(distance<.001f)return;
  var p=transform.position;
  if(Physics.CapsuleCast(p+Vector3.up*.45f,p+Vector3.up*1.4f,.35f,step.normalized,out var wall,distance+.05f,1,QueryTriggerInteraction.Ignore))step=step.normalized*Mathf.Max(0,wall.distance-.05f);
  if(NavMesh.Raycast(p,p+step,out var edge,agent.areaMask))step=Vector3.ClampMagnitude(edge.position-p,Mathf.Max(0,Vector3.Distance(p,edge.position)-.06f));
  agent.Move(step);
 }
}
}
