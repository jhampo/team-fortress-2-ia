using UnityEngine;
namespace FpsStage2 {
// Fixed spindle: the housing and gripping hands never orbit with the barrels.
public sealed class CPBotMotor:MonoBehaviour {
 public Transform spindle;public Vector3 axis;public Mesh originalMesh;Quaternion rest;Vector3 center;CPActor actor;float angle,speed;
 void Start(){actor=GetComponentInParent<CPActor>();rest=spindle.localRotation;center=spindle.localPosition;}
 void LateUpdate(){if(actor==null||actor.Clock==null)return;float desired=actor.Alive&&actor.Clock.Spinning?1200:0;speed=Mathf.MoveTowards(speed,desired,Time.deltaTime*(desired>0?1400:3500));angle=Mathf.Repeat(angle+speed*Time.deltaTime,360);spindle.localPosition=center;spindle.localRotation=rest*Quaternion.AngleAxis(angle,axis);}
}
}
