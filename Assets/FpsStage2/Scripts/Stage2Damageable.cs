using UnityEngine;
namespace FpsStage2 {
public sealed class Stage2Damageable:MonoBehaviour {
 public float health=200;
 float burnUntil,nextBurn;
 public void Hit(float damage){health=Mathf.Max(0,health-damage);}
 public void Burn(float duration){burnUntil=Mathf.Max(burnUntil,Time.time+Mathf.Clamp(duration,4,10));}
 void Update(){if(Time.time<burnUntil&&Time.time>=nextBurn){Hit(4);nextBurn=Time.time+.5f;}}
}
}
