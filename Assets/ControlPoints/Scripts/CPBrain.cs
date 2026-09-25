using UnityEngine;
using UnityEngine.AI;
namespace FpsStage2 {
public enum CPRole {Attack,Defend,Flank}
public enum CPBotState {Capturar,Defender,Reforzar,Retirarse,Patrullar,Combate}
[RequireComponent(typeof(NavMeshAgent),typeof(CPActor))]
public sealed class CPBrain:MonoBehaviour {
 public CPRole role;public CPBotState state;public int objective;public Animation animations;public Transform spine;
 public float decisionInterval=.35f;public string currentAnimation="Idle";public Vector3 destination;
 CPActor actor,target;CPMatch match;NavMeshAgent agent;ParticleSystem flame;float nextThink,seenAt,nextJump,recoil,reloadStart;public CPJumpMotion jump=new CPJumpMotion();int reloadSerial;Vector3 lastPosition;float stuckTime;
 readonly CPRouteNavigator navigator=new CPRouteNavigator();public string ActiveRoute=>navigator.ActiveName;public int RoutePlans=>navigator.Plans;public int RoutesCompleted=>navigator.Completed;
 bool captureMotion,travelLeg;float nextCaptureStep;Vector3 captureGoal;public bool CaptureMoving=>captureMotion;
 [System.NonSerialized] NavMeshPath travelPath,lanePath;Vector3 lastGoal;float nextPath,visibilityAt;bool cachedVisible;
 static readonly float[] reaction={.8f,.38f,.2f,.12f},aimError={.085f,.04f,.02f,.009f};
 public void Initialize(CPActor a,CPMatch m){actor=a;match=m;travelPath=new NavMeshPath();lanePath=new NavMeshPath();agent=GetComponent<NavMeshAgent>();agent.updateRotation=false;agent.autoTraverseOffMeshLink=true;agent.obstacleAvoidanceType=ObstacleAvoidanceType.HighQualityObstacleAvoidance;agent.stoppingDistance=.16f;agent.avoidancePriority=30+Mathf.Abs(name.GetHashCode()%40);nextThink=Time.time+Random.Range(0,.35f);nextJump=Time.time+Random.Range(3,7);if(a.characterClass==ClassId.Pyro)flame=m.player.Effects.CreateBotFireSystem(name+"_Flames");}
 void Update(){if(actor==null)return;if(!actor.Alive||match.rules.Finished||!match.RoundLive){Stop();if(actor.Alive&&animations!=null&&currentAnimation!="Idle"){animations.CrossFade("Idle",.12f);animations["Idle"].speed=1;currentAnimation="Idle";}return;}if(match.MenuOpen)return;if(!agent.isOnNavMesh)return;agent.isStopped=false;
  // Renew short spacing waypoints before the fast Scout reaches their end.
  if(actor.characterClass==ClassId.Scout&&travelLeg&&agent.hasPath&&agent.remainingDistance<Mathf.Max(2,agent.speed*.6f))nextThink=Mathf.Min(nextThink,Time.time+.04f);
  if(Time.time>=nextThink&&CPWebPerformance.CanThink()){float delay=decisionInterval;if(CPWebPerformance.Active&&target==null&&(transform.position-match.player.transform.position).sqrMagnitude>1600)delay=Mathf.Max(delay,.55f);nextThink=Time.time+delay;Think();}
  if(Time.time>=visibilityAt){visibilityAt=Time.time+.075f;cachedVisible=target!=null&&target.Alive&&match.combat.Visible(actor,target);}bool visible=target!=null&&target.Alive&&cachedVisible;float distance=visible?Vector3.Distance(actor.Eye,target.Eye):1000;
  float range=actor.characterClass==ClassId.Pyro?CPFlameStats.Range:actor.characterClass==ClassId.Scout?25:60;
  Vector3 look=visible?target.Eye-actor.Eye:agent.desiredVelocity;look.y=0;if(look.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(look),Time.deltaTime*300);
  bool shoot=visible&&distance<range&&Time.time-seenAt>=reaction[(int)match.difficulty]&&Vector3.Dot(transform.forward,(target.Eye-actor.Eye).normalized)>.88f;
  bool prepare=actor.characterClass==ClassId.Heavy&&visible&&distance<65;var clock=actor.Clock;
  int fired=clock.Step(Time.timeAsDouble,shoot,prepare,!shoot&&clock.Ammo<clock.Capacity);
  if(clock.Reloading&&reloadSerial!=clock.ReloadSerial){reloadSerial=clock.ReloadSerial;reloadStart=Time.time;}
  if(fired>0){Vector3 aim=target.Eye;if(actor.characterClass==ClassId.Soldier){var other=target.GetComponent<NavMeshAgent>();if(other!=null)aim+=other.velocity*Mathf.Min(distance/42,.8f);aim.y-=.7f;}
   Vector3 dir=(aim-actor.Eye).normalized;dir=(dir+Random.insideUnitSphere*aimError[(int)match.difficulty]).normalized;match.combat.Fire(actor,dir);recoil=1;if(animations!=null&&animations["Fire"]!=null){var shot=animations["Fire"];shot.layer=1;shot.blendMode=AnimationBlendMode.Additive;shot.time=0;animations.Play("Fire",PlayMode.StopSameLayer);}}
  if(flame!=null){bool emit=shoot&&clock.Ammo>0;flame.transform.SetPositionAndRotation(actor.muzzle.position,Quaternion.LookRotation(visible?(target.Eye-actor.muzzle.position).normalized:transform.forward));if(emit&&!flame.isEmitting)flame.Play();else if(!emit&&flame.isEmitting)flame.Stop(true,ParticleSystemStopBehavior.StopEmitting);}
  float speed=Vector3.Dot(agent.desiredVelocity,transform.forward)<-.1f?match.player.backwards[(int)actor.characterClass]:actor.Speed;if(clock.Spinning&&actor.characterClass==ClassId.Heavy)speed*=.37f;agent.speed=captureMotion?Mathf.Min(speed,actor.characterClass==ClassId.Scout?2.6f:1.9f):speed;
  if(!captureMotion&&shoot&&actor.characterClass!=ClassId.Pyro&&actor.characterClass!=ClassId.Scout&&distance>6&&distance<range*.65f&&state!=CPBotState.Retirarse&&match.rules.Points[objective].Owner==actor.team)agent.isStopped=true;
  JumpAndAnimate(visible);if((transform.position-lastPosition).sqrMagnitude<.0001f&&agent.remainingDistance>3&&!agent.isStopped)stuckTime+=Time.deltaTime;else stuckTime=0;lastPosition=transform.position;if(stuckTime>2){agent.ResetPath();nextThink=0;stuckTime=0;}
 }
 void Think(){captureMotion=false;CPActor closest=null;float best=65;foreach(var a in match.actors){if(!a.Alive||a.team==actor.team)continue;float d=Vector3.Distance(actor.Eye,a.Eye);if(d<best&&match.combat.Visible(actor,a)){best=d;closest=a;}}if(closest!=target){target=closest;seenAt=Time.time;}
  int enemies=match.CountNear(ControlPointRules.Enemy(actor.team),transform.position,14),allies=match.CountNear(actor.team,transform.position,14);float friendlyHealth=0,enemyHealth=0;foreach(var unit in match.actors)if(unit.Alive&&Vector3.Distance(unit.transform.position,transform.position)<14){if(unit.team==actor.team)friendlyHealth+=unit.health;else enemyHealth+=unit.health;}if(enemyHealth>friendlyHealth*1.8f)enemies++;bool desperate=match.rules.Remaining<45&&match.rules.Points[1].Owner!=actor.team;
  if(actor.HealthFraction<.28f||actor.Clock.Ammo==0&&actor.Clock.Reserve==0||!desperate&&match.difficulty>=CPDifficulty.Dificil&&enemies>allies+1&&actor.HealthFraction<.6f){navigator.Cancel(3);state=CPBotState.Retirarse;Go(match.SafeStation(actor));return;}
  objective=match.Objective(actor,role);var zone=match.zones[objective];var point=match.rules.Points[objective];int foe=actor.team==CPTeam.RED?point.BlueCount:point.RedCount;int friend=actor.team==CPTeam.RED?point.RedCount:point.BlueCount;
  state=point.Owner==actor.team?(foe>0?CPBotState.Reforzar:CPBotState.Defender):CPBotState.Capturar;Vector3 goal=zone.position;float phase=(Mathf.Abs(name.GetHashCode())%100)*.0628f;goal+=new Vector3(Mathf.Cos(phase),0,Mathf.Sin(phase))*1.6f;
  if(target!=null){state=foe>friend?CPBotState.Reforzar:CPBotState.Combate;if(actor.characterClass==ClassId.Pyro||best>22)goal=target.transform.position;else if(match.difficulty>=CPDifficulty.Dificil&&actor.HealthFraction<.65f){Vector3 cover;if(FindCover(target,out cover))goal=cover;}}
  else if(point.Owner==actor.team&&foe==0&&friend>=2){state=CPBotState.Patrullar;float angle=Time.time*.09f+phase;goal=zone.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*5;}
  Vector3 alternative;if(foe==0&&(target==null||best>22)){if(navigator.Waypoint(actor,match,agent,role,goal,out alternative))goal=alternative;}else navigator.Cancel(3);
  if(!point.Locked&&zone.Contains(transform.position)&&(point.Owner!=actor.team||foe>0)){
   captureMotion=true;navigator.Cancel(2);
   if(Time.time>=nextCaptureStep||Vector3.Distance(captureGoal,transform.position)<.4f){nextCaptureStep=Time.time+Random.Range(1.2f,1.9f);if(!CPBotSpacing.CaptureTarget(actor,match,agent,zone,out captureGoal))captureGoal=transform.position;}
   goal=captureGoal;
  }
  Go(goal);
 }
 public bool Go(Vector3 point){
  float renew=actor.characterClass==ClassId.Scout?Mathf.Max(2,agent.speed*.6f):1.1f;
  if(agent.hasPath&&Time.time<nextPath&&(point-lastGoal).sqrMagnitude<(CPWebPerformance.Active?9:1)&&agent.remainingDistance>renew)return true;
  NavMeshHit h;if(!NavMesh.SamplePosition(point,out h,5,NavMesh.AllAreas))return false;
  var path=travelPath;if(!agent.CalculatePath(h.position,path)||path.status!=NavMeshPathStatus.PathComplete)return false;
  Vector3 lane;travelLeg=false;
  if(!captureMotion&&CPBotSpacing.TravelTarget(actor,match,agent,path,out lane)){
   if(agent.CalculatePath(lane,lanePath)&&lanePath.status==NavMeshPathStatus.PathComplete){path=lanePath;h.position=lane;travelLeg=true;}
  }
  // Keep braking at real goals, not at temporary lane waypoints.
  agent.autoBraking=!(actor.characterClass==ClassId.Scout&&travelLeg);
  destination=h.position;lastGoal=point;nextPath=Time.time+(CPWebPerformance.Active?1.15f:.85f);agent.SetPath(path);return true;
 }
 bool FlankNode(Vector3 goal,out Vector3 best){best=goal;float direct=Vector3.Distance(transform.position,goal),score=float.PositiveInfinity;foreach(var node in match.strategicNodes){float first=Vector3.Distance(transform.position,node),second=Vector3.Distance(node,goal);if(first<4||second>direct-3||first+second>direct*1.45f)continue;float side=Vector3.Cross((goal-transform.position).normalized,node-transform.position).magnitude;if(side<3)continue;float s=first+second-side*.8f-(node.y-transform.position.y)*2;if(s<score){NavMeshPath path=new NavMeshPath();if(agent.CalculatePath(node,path)&&path.status==NavMeshPathStatus.PathComplete){score=s;best=node;}}}return score<float.PositiveInfinity;}
 bool FindCover(CPActor enemy,out Vector3 best){best=transform.position;float score=float.PositiveInfinity;foreach(var node in match.strategicNodes){float d=Vector3.Distance(node,transform.position);if(d>14||d<1)continue;if(!Physics.Linecast(node+Vector3.up*1.2f,enemy.Eye,1,QueryTriggerInteraction.Ignore))continue;if(d<score){NavMeshPath p=new NavMeshPath();if(agent.CalculatePath(node,p)&&p.status==NavMeshPathStatus.PathComplete){score=d;best=node;}}}return score<float.PositiveInfinity;}
 void JumpAndAnimate(bool combat){bool moving=agent.velocity.sqrMagnitude>.15f;
  if(!jump.Busy&&Time.time>=nextJump&&moving&&(agent.isOnOffMeshLink||combat)){
   nextJump=Time.time+Random.Range(actor.characterClass==ClassId.Scout?3.5f:4.5f,8);
   float clearance=1.9f+match.player.jumpHeight*(actor.characterClass==ClassId.Scout?(match.difficulty>=CPDifficulty.Normal?2.4f:1.2f):1);
   if((agent.isOnOffMeshLink||Random.value<.4f+(int)match.difficulty*.18f)&&!Physics.CheckCapsule(transform.position+Vector3.up*1.9f,transform.position+Vector3.up*clearance,.28f,1,QueryTriggerInteraction.Ignore))
    jump.Start(match.player.jumpHeight*(actor.characterClass==ClassId.Scout?1.2f:1),actor.characterClass==ClassId.Scout&&match.difficulty>=CPDifficulty.Normal);
  }
  jump.Tick(Time.deltaTime);agent.baseOffset=jump.Height;
  float groundSpeed=Vector3.ProjectOnPlane(agent.velocity,Vector3.up).magnitude;
  string clip=jump.Busy?"Jump":moving?(groundSpeed<1.8f?"Walk":"Run"):"Idle";
  if(animations!=null&&animations[clip]!=null){
   bool gait=clip=="Walk"||clip=="Run";
   if(currentAnimation!=clip){float phase=(currentAnimation=="Walk"||currentAnimation=="Run")&&animations[currentAnimation]!=null?Mathf.Repeat(animations[currentAnimation].normalizedTime,1):0;animations.CrossFade(clip,.16f);if(gait)animations[clip].normalizedTime=phase;currentAnimation=clip;}
   float direction=Vector3.Dot(agent.velocity,transform.forward)<-.1f?-1:1;
   animations[clip].speed=gait?direction*groundSpeed/CPGaitProfile.CycleDistance(actor.characterClass,clip=="Walk"):clip=="Jump"?0:1;if(clip=="Jump")animations[clip].normalizedTime=jump.ClipTime;
  }
 }
 void LateUpdate(){if(actor==null||!actor.Alive||spine==null||match.MenuOpen)return;recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*(actor.characterClass==ClassId.Soldier?5:9));float reload=actor.Clock.Reloading?Mathf.Sin(Mathf.Clamp01((Time.time-reloadStart)/(float)(actor.characterClass==ClassId.Scout?1.4333:.8))*Mathf.PI):0;float heavyLower=actor.characterClass==ClassId.Heavy&&actor.Clock.Spinning?Mathf.Clamp01(1-(float)(actor.Clock.Deadline-Time.time)/1.1f)*4:0;spine.localRotation*=Quaternion.Euler(reload*5+heavyLower,reload*-3,0);}
 void Stop(){if(agent!=null&&agent.isOnNavMesh){agent.isStopped=true;agent.baseOffset=0;}navigator.Cancel();jump.Reset();if(flame!=null&&flame.isEmitting)flame.Stop(true,ParticleSystemStopBehavior.StopEmitting);}
}
}
