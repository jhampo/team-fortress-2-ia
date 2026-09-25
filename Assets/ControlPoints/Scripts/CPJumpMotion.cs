using UnityEngine;
namespace FpsStage2 {
public enum CPJumpPhase { Grounded,Takeoff,Airborne,Landing }
[System.Serializable] public sealed class CPJumpMotion {
 public CPJumpPhase Phase {get;private set;}public float Height {get;private set;}public float Velocity {get;private set;}public float ClipTime {get;private set;}public bool DoubleUsed {get;private set;}
 public bool Busy=>Phase!=CPJumpPhase.Grounded;float elapsed,launchSpeed,apex;bool allowDouble;
 public bool Start(float height,bool doubleJump){if(Busy)return false;Phase=CPJumpPhase.Takeoff;Height=Velocity=ClipTime=elapsed=0;launchSpeed=Mathf.Sqrt(40*Mathf.Max(.1f,height));allowDouble=doubleJump;DoubleUsed=false;apex=height;return true;}
 public void Reset(){Phase=CPJumpPhase.Grounded;Height=Velocity=ClipTime=elapsed=0;DoubleUsed=false;}
 public void Tick(float dt){
  if(dt<=0||!Busy)return;elapsed+=dt;
  if(Phase==CPJumpPhase.Takeoff){ClipTime=.18f*Mathf.Clamp01(elapsed/.12f);if(elapsed>=.12f){Phase=CPJumpPhase.Airborne;Velocity=launchSpeed;Height=.001f;elapsed=0;}return;}
  if(Phase==CPJumpPhase.Landing){ClipTime=Mathf.Lerp(.84f,1,Mathf.Clamp01(elapsed/.18f));if(elapsed>=.18f){Phase=CPJumpPhase.Grounded;ClipTime=1;}return;}
  Height+=Velocity*dt-10*dt*dt;Velocity-=20*dt;apex=Mathf.Max(apex,Height);
  if(allowDouble&&!DoubleUsed&&Velocity<=0){Velocity=launchSpeed;DoubleUsed=true;apex=Height+launchSpeed*launchSpeed/40;}
  float target=Velocity>0?Mathf.Lerp(DoubleUsed?.34f:.18f,.49f,1-Velocity/launchSpeed):Mathf.Lerp(.52f,.83f,1-Mathf.Clamp01(Height/apex));ClipTime=Mathf.MoveTowards(ClipTime,target,dt*3.8f);
  if(Height<=0&&Velocity<0){Height=Velocity=0;Phase=CPJumpPhase.Landing;elapsed=0;ClipTime=.84f;}
 }
}
}
