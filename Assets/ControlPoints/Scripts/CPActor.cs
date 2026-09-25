using UnityEngine;
using UnityEngine.AI;
namespace FpsStage2 {
public sealed class CPActor:MonoBehaviour {
 public CPScore score=new CPScore();public int MatchIndex;public CPBrain Brain {get;private set;}
 public CPFlamePower flamePower=CPFlamePower.Normal;
 public CPTeam team;public ClassId characterClass;public bool isPlayer;public Transform muzzle;
 public float health,maxHealth;public bool Alive=>health>0;public float respawnAt;public Vector3 spawn;public Quaternion spawnRotation;
 public WeaponClock clock;public float burnUntil,nextBurn;CPActor burnSource;Renderer[] body;Collider[] colliders;NavMeshAgent agent;FpsStage2Player player;Transform head;
 public const float BurnTickInterval=.5f;float burnTickDamage=4;
 public bool IsBurning=>Alive&&characterClass!=ClassId.Pyro&&burnUntil>Time.time;
 public float BurnRemaining=>IsBurning?burnUntil-Time.time:0;
 public bool BurningFrom(CPActor source)=>IsBurning&&burnSource==source;
 // Keep the label over the actor's centre, not the animated head's sideways sway.
 public Vector3 LabelPosition=>new Vector3(transform.position.x,head!=null?head.position.y+.32f:transform.position.y+2,transform.position.z);
 public Vector3 Eye=>isPlayer?player.playerCamera.transform.position:transform.position+Vector3.up*1.5f;
 public float Speed=>CPMatch.Instance.player.speeds[(int)characterClass];
 public float HealthFraction=>maxHealth>0?health/maxHealth:0;
 public WeaponClock Clock=>isPlayer?player.Clock:clock;
 public void Initialize(){Brain=GetComponent<CPBrain>();player=GetComponent<FpsStage2Player>();agent=GetComponent<NavMeshAgent>();body=GetComponentsInChildren<Renderer>(true);colliders=GetComponents<Collider>();foreach(var bone in GetComponentsInChildren<Transform>(true))if(bone.name=="Head"){head=bone;break;}spawn=transform.position;spawnRotation=transform.rotation;ResetLife();}
 public void SyncClass(){if(!isPlayer)return;characterClass=player.currentClass;maxHealth=player.maxHealth[(int)characterClass];health=player.View.health;muzzle=player.View.muzzle;if(characterClass==ClassId.Pyro)Extinguish();}
 public void ResetLife(){if(isPlayer)CPMatch.Instance.flow?.ApplyPendingClass();CPMatch.Instance.feedback?.Forget(this);CPMatch.Instance.scores?.Respawn(this);CPMatch.Instance.audioSystem?.Play(CPSound.Respawn,spawn,isPlayer);maxHealth=CPMatch.Instance.player.maxHealth[(int)characterClass];health=maxHealth;clock=new WeaponClock(characterClass);Extinguish();respawnAt=0;
  if(agent!=null){agent.enabled=true;agent.baseOffset=0;if(agent.isOnNavMesh)agent.Warp(spawn);else{NavMeshHit h;if(NavMesh.SamplePosition(spawn,out h,4,-1))agent.Warp(h.position);}}
  if(isPlayer){GetComponent<CPDeathCamera>()?.Restore();player.MatchRespawn(spawn);}else{transform.position=agent!=null&&agent.isOnNavMesh?agent.nextPosition:spawn;transform.rotation=spawnRotation;foreach(var r in body)r.enabled=true;foreach(var c in colliders)c.enabled=true;}
 }
 public void Damage(float amount,CPActor source,bool selfDamage=false,Vector3? attackOrigin=null,bool afterburn=false,bool explosive=false){
  if(!Alive||CPMatch.Instance==null||!CPMatch.Instance.WeaponsAvailable||CPMatch.Instance.rules.Finished||amount<=0)return;if(source!=null&&source.team==team&&!(selfDamage&&source==this))return;
  float dealt=Mathf.Min(health,amount);health=Mathf.Max(0,health-dealt);if(isPlayer)player.View.health=health;
  CPMatch.Instance.scores?.Damage(this,source,dealt);CPMatch.Instance.audioSystem?.Hurt(this,source,afterburn);
  CPMatch.Instance.feedback?.ReportDamage(this,source,dealt,attackOrigin??(source!=null?source.Eye:Eye),afterburn);
  if(health>0)return;CPMatch.Instance.scores?.Death(this,source);CPMatch.Instance.audioSystem?.Death(this,source);CPDeathPresentation.Spawn(this,explosive,attackOrigin);Extinguish();Clock.Cancel();respawnAt=Time.time+CPMatch.Instance.rules.RespawnDelay(team);
  if(agent!=null&&agent.isOnNavMesh){agent.ResetPath();agent.isStopped=true;}
  if(isPlayer){player.MatchStopWeapon();foreach(var v in player.views)v.animation.gameObject.SetActive(false);}else{foreach(var r in body)r.enabled=false;foreach(var c in colliders)c.enabled=false;}
 }
 public void Burn(float seconds,CPActor source,bool miniCritical=false){
  if(characterClass==ClassId.Pyro||!Alive||source!=null&&source.team==team)return;
  if(!IsBurning)nextBurn=Time.time+BurnTickInterval;
  burnUntil=Mathf.Max(burnUntil,Time.time+Mathf.Clamp(seconds,4,10));burnSource=source;burnTickDamage=miniCritical?5:4;
 }
 public void Extinguish(){burnUntil=nextBurn=0;burnSource=null;burnTickDamage=4;}
 public void HealAndSupply(){if(!Alive)return;health=maxHealth;if(isPlayer)player.View.health=health;var c=Clock;c.Ammo=c.Capacity;c.Reserve=characterClass==ClassId.Scout?32:characterClass==ClassId.Soldier?31:0;Extinguish();}
 public void Tick(){
  if(!Alive){if(Time.time>=respawnAt&&!CPMatch.Instance.rules.Finished)ResetLife();return;}
  if(isPlayer)SyncClass();
  // Retain tick phase when flames refresh the duration; catch up after a slow frame.
  while(Alive&&burnUntil>0&&nextBurn>0&&nextBurn<=Time.time+1e-5f&&nextBurn<=burnUntil+1e-5f){nextBurn+=BurnTickInterval;Damage(burnTickDamage,burnSource,false,null,true);}
  if(burnUntil>0&&Time.time>=burnUntil)Extinguish();
 }
}
}
