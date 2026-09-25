using UnityEngine;
namespace FpsStage2 {
// Player and bots use the same weapon clock, hit resolution and projectile pool.
public sealed class CPCombat:MonoBehaviour {
 CPMatch match;Stage2Effects fx;readonly RaycastHit[] hits=new RaycastHit[48],fireHits=new RaycastHit[48];readonly System.Collections.Generic.HashSet<CPActor> burned=new System.Collections.Generic.HashSet<CPActor>();
 public void Initialize(CPMatch value){match=value;fx=match.player.Effects;}
 public static bool InAirblast(Vector3 origin,Vector3 forward,Vector3 point){var delta=point-origin;return delta.sqrMagnitude<=3.8f*3.8f&&(delta.sqrMagnitude<.01f||Vector3.Dot(forward.normalized,delta.normalized)>=.55f);}
 public void Airblast(CPActor source,Vector3 direction){
  if(source==null||!source.Alive||!match.WeaponsAvailable||match.rules.Finished)return;
  direction.Normalize();var origin=source.Eye;fx.DeflectRockets(source,origin,direction);fx.Airblast(source.muzzle.position,direction);
  match.audioSystem?.Play(CPSound.FlameEnd,source.Eye,source.isPlayer,.85f);
  foreach(var target in match.actors){if(target==source||!target.Alive||!InAirblast(origin,direction,target.Eye)||!Visible(source,target))continue;
   if(target.team==source.team)target.Extinguish();
   else {var push=target.GetComponent<CPAirblastPush>();if(push==null)push=target.gameObject.AddComponent<CPAirblastPush>();push.Apply(direction);}
  }
 }
 public bool Cast(CPActor source,Vector3 origin,Vector3 direction,float distance,out RaycastHit nearest){int n=Physics.RaycastNonAlloc(origin,direction,hits,distance,257,QueryTriggerInteraction.Ignore);nearest=default;float best=distance+1;bool found=false;for(int i=0;i<n;i++){var a=hits[i].collider.GetComponentInParent<CPActor>();if(a!=null&&(a==source||!a.Alive))continue;if(hits[i].distance<best){nearest=hits[i];best=hits[i].distance;found=true;}}return found;}
 public bool Visible(CPActor source,CPActor target){Vector3 delta=target.Eye-source.Eye;return !Cast(source,source.Eye,delta.normalized,delta.magnitude,out var hit)||hit.collider.GetComponentInParent<CPActor>()==target;}
 public void Fire(CPActor source,Vector3 direction){if(!source.Alive||match.rules.Finished||!match.WeaponsAvailable||(!source.isPlayer&&!match.RoundLive))return;direction.Normalize();Vector3 origin=source.Eye,muzzle=source.muzzle.position;var id=source.characterClass;match.audioSystem?.Shot(source);
  if(id==ClassId.Pyro){burned.Clear();int count=Physics.SphereCastNonAlloc(origin,.3f,direction,fireHits,CPFlameStats.Range,257,QueryTriggerInteraction.Ignore);for(int i=0;i<count;i++){var target=fireHits[i].collider.GetComponentInParent<CPActor>();if(target==null||!target.Alive||target.team==source.team||!burned.Add(target)||!Visible(source,target))continue;float distance=fireHits[i].distance;target.Damage(CPFlameStats.ParticleDamage(distance/CPFlameStats.Range,1,source.flamePower),source);target.Burn(CPFlameStats.BurnDuration(distance/CPFlameStats.Range),source,source.flamePower==CPFlamePower.MiniCritical);}return;}
  if(id==ClassId.Soldier){Vector3 aim=Cast(source,origin,direction,100,out var aimHit)?aimHit.point:origin+direction*100;fx.Rocket(muzzle,(aim-muzzle).normalized,origin,source);return;}
  fx.FlashFor(source.muzzle,id,.12f);Vector3 right=Vector3.Cross(Vector3.up,direction).normalized,up=Vector3.Cross(direction,right);int pellets=id==ClassId.Scout?12:4;float spread=source.Clock.SpreadTangent(Time.timeAsDouble);
  if(id==ClassId.Scout)fx.ScoutTracers(muzzle,right,origin+direction*80);
  for(int i=0;i<pellets;i++){Vector2 offset=Random.insideUnitCircle*spread;var dir=(direction+right*offset.x+up*offset.y).normalized;bool hit=Cast(source,origin,dir,100,out var h);Vector3 end=hit?h.point:origin+dir*100;if(id==ClassId.Heavy)fx.HeavyTracer(muzzle,direction,end,i);if(!hit)continue;float damage=(id==ClassId.Scout?5.4f:9)*Mathf.Lerp(id==ClassId.Scout?1.75f:1.5f,.528f,Mathf.Clamp01(h.distance/30));var target=h.collider.GetComponentInParent<CPActor>();if(target!=null)target.Damage(damage,source);else h.collider.GetComponentInParent<Stage2Damageable>()?.Hit(damage);fx.Impact(h.point);}
 }
 public void Explode(CPActor source,Vector3 point,Vector3 shotOrigin){fx.Explosion(point);float direct=Mathf.Lerp(112,48,Mathf.Clamp01(Vector3.Distance(point,shotOrigin)/30));foreach(var a in match.actors){if(!a.Alive||a.team==source.team&&a!=source)continue;Vector3 center=a.transform.position+Vector3.up*.8f;float distance=Vector3.Distance(point,center);if(distance>2.8f||Physics.Linecast(point,center,1,QueryTriggerInteraction.Ignore))continue;a.Damage(a==source?Mathf.Lerp(89,27,distance/2.8f):direct*Mathf.Lerp(1,.5f,distance/2.8f),source,true,point,false,true);}}
}
}
