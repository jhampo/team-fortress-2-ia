using UnityEngine;
namespace FpsStage2 {
public sealed class CPSpawnGate:MonoBehaviour {
 public Transform shutter;public Collider barrier;public float lift=5.2f;Vector3 closed;bool opening;
 void Awake(){closed=shutter.localPosition;}
 public void Close(){closed=shutter.localPosition;opening=false;barrier.enabled=true;}
 public void Open(){opening=true;barrier.enabled=false;CPMatch.Instance?.audioSystem?.Play(CPSound.Gate,transform.position);}
 void Update(){if(opening)shutter.localPosition=Vector3.MoveTowards(shutter.localPosition,closed+Vector3.up*lift,Time.deltaTime*5);}
}
}
