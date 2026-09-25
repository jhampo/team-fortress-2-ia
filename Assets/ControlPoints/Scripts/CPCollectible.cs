using System.Collections.Generic;
using UnityEngine;
namespace FpsStage2 {
public enum CPSupplyKind { HealthSmall,HealthMedium,HealthLarge,AmmoSmall,AmmoMedium,AmmoLarge,DroppedWeapon }
public sealed class CPCollectible:MonoBehaviour {
 public CPSupplyKind kind;public Transform artwork;public float respawnSeconds=10,lifetimeSeconds;public double ReadyAt {get;private set;} public double ExpiresAt {get;private set;}
 public bool Available {get;private set;}=true;public bool IsHealth=>kind<=CPSupplyKind.HealthLarge;
 public static readonly HashSet<CPCollectible> All=new HashSet<CPCollectible>();
 double pickupAfter;bool consumed;double nextScan;
 void OnEnable(){All.Add(this);}void OnDisable(){All.Remove(this);}
 void Start(){if(lifetimeSeconds>0){ExpiresAt=Time.timeAsDouble+lifetimeSeconds;pickupAfter=Time.timeAsDouble+.3;}}
 public static float Fraction(CPSupplyKind k)=>k==CPSupplyKind.HealthSmall||k==CPSupplyKind.AmmoSmall?.205f:k==CPSupplyKind.HealthLarge||k==CPSupplyKind.AmmoLarge?1:.5f;
 public static int ReserveCapacity(ClassId c)=>c==ClassId.Scout?32:c==ClassId.Soldier?31:c==ClassId.Pyro?215:200;
 public static bool Needs(CPActor a,CPSupplyKind k){if(a==null||!a.Alive)return false;if(k<=CPSupplyKind.HealthLarge)return a.health<a.maxHealth-.001f||a.IsBurning;return a.characterClass==ClassId.Scout||a.characterClass==ClassId.Soldier?a.Clock.Reserve<ReserveCapacity(a.characterClass):a.Clock.Ammo<a.Clock.Capacity;}
 public bool UsefulFor(CPActor a)=>Available&&!consumed&&Needs(a,kind);
 public static bool Grant(CPActor actor,CPSupplyKind k){
  if(!Needs(actor,k))return false;float f=Fraction(k);
  if(k<=CPSupplyKind.HealthLarge){actor.health=Mathf.Min(actor.maxHealth,actor.health+actor.maxHealth*f);actor.Extinguish();if(actor.isPlayer)actor.GetComponent<FpsStage2Player>().View.health=actor.health;}
  else{var c=actor.Clock;int capacity=ReserveCapacity(actor.characterClass),amount=Mathf.CeilToInt(capacity*f);if(actor.characterClass==ClassId.Scout||actor.characterClass==ClassId.Soldier)c.Reserve=Mathf.Min(capacity,c.Reserve+amount);else c.Ammo=Mathf.Min(capacity,c.Ammo+amount);}
  return true;
 }
 public bool TryCollect(CPActor actor,double now){
  if(!Available||consumed||now<pickupAfter||ExpiresAt>0&&now>=ExpiresAt||!Grant(actor,kind))return false;
  Available=false;if(artwork!=null)artwork.gameObject.SetActive(false);
  CPMatch.Instance?.audioSystem?.Play(IsHealth?CPSound.Health:CPSound.Ammo,transform.position,actor.isPlayer);
  if(lifetimeSeconds>0){consumed=true;Destroy(gameObject);}else ReadyAt=now+respawnSeconds;
  return true;
 }
 public void Advance(double now){if(lifetimeSeconds>0&&ExpiresAt>0&&now>=ExpiresAt){consumed=true;Available=false;Destroy(gameObject);return;}if(!consumed&&!Available&&now>=ReadyAt){Available=true;if(artwork!=null)artwork.gameObject.SetActive(true);}}
 void Update(){
  double now=Time.timeAsDouble;Advance(now);var m=CPMatch.Instance;if(now<nextScan||!Available||consumed||m==null||m.actors==null||m.MenuOpen||m.rules.Finished||!m.RoundLive)return;nextScan=now+.1;
  CPActor nearest=null;float best=.95f*.95f;
  foreach(var a in m.actors){if(!UsefulFor(a))continue;var delta=a.transform.position-transform.position;if(Mathf.Abs(delta.y)>1.3f)continue;delta.y=0;float d=delta.sqrMagnitude;if(d>=best)continue;if(Physics.Linecast(a.transform.position+Vector3.up*.55f,transform.position+Vector3.up*.3f,1,QueryTriggerInteraction.Ignore))continue;best=d;nearest=a;}
  if(nearest!=null)TryCollect(nearest,now);
 }
}
}
