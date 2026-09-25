using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FpsStage2 {
public enum ClassId { Scout, Pyro, Soldier, Heavy }
[Serializable] public sealed class WeaponClock {
 public ClassId Class;
 public int Ammo, Reserve, Shots, Reloads, ReloadSerial;
 public string State = "Idle";
 public double NextShot, Deadline, NextConsume;
 public bool Reloading, Spinning;
 bool pendingAutoReload;
 public int MotorSerial;
 public double NextAirblast;
 public bool TryAirblast(double now){if(Class!=ClassId.Pyro||Ammo<20||now+1e-8<NextAirblast)return false;Ammo-=20;NextAirblast=now+.75;NextShot=System.Math.Max(NextShot,NextAirblast);NextConsume=NextAirblast;State="Aire comprimido";return true;}
 public const float HeavyMinSpread=.015f,HeavyMaxSpread=.04f;
 public double HeavyLastShot=-1000;float heavyHeat;
 public float HeavyHeatAt(double now)=>Mathf.Clamp01(heavyHeat-(float)Math.Max(0,now-HeavyLastShot-.16)/.9f);
 public float SpreadTangent(double now)=>Class==ClassId.Heavy?Mathf.Lerp(HeavyMinSpread,HeavyMaxSpread,HeavyHeatAt(now)):Class==ClassId.Scout?1f/30:0;
 public int Capacity => Class == ClassId.Scout ? 2 : Class == ClassId.Soldier ? 4 : Class == ClassId.Pyro ? 215 : 200;
 public double Interval => Class == ClassId.Scout ? .3125 : Class == ClassId.Soldier ? .8 : Class == ClassId.Heavy ? .105 : .075;
 public bool SpinningReady(double now) => Spinning && now+1e-8>=Deadline;
 public bool ShouldPlayEmpty(double now,bool trigger,int fired) => trigger&&fired==0&&Ammo<=0&&Reserve<=0&&!Reloading&&now+1e-8>=NextShot;
 public WeaponClock(ClassId id) { Class=id; Ammo=Capacity; Reserve=id==ClassId.Scout?32:id==ClassId.Soldier?31:0; }
 public void Cancel() { Reloading=false;Spinning=false;pendingAutoReload=false;State="Idle";Deadline=0; }
 public bool Reload(double now) {
  if (Class!=ClassId.Scout && Class!=ClassId.Soldier || Reloading || Ammo>=Capacity || Reserve<=0 || now+1e-8<NextShot) return false;
  Reloading=true;State="Recarga";Deadline=now+(Class==ClassId.Scout?1.4333:.92);ReloadSerial++;return true;
 }
 public int Step(double now, bool left, bool right, bool reload) {
  while(Reloading && now+1e-8>=Deadline) {
   int n=Math.Min(Class==ClassId.Scout?Capacity-Ammo:1,Reserve);Ammo+=n;Reserve-=n;Reloads++;
   if(Class==ClassId.Soldier && Ammo<Capacity && Reserve>0) { Deadline+=.8;ReloadSerial++; }
   else { Reloading=false;pendingAutoReload=false;State="Idle"; }
  }
  if(Class==ClassId.Heavy) {
   bool motorRequested=left||right;
   if(motorRequested && !Spinning) { Spinning=true;Deadline=now+(left?1.10:.87);State=left?"Bajando":"Acelerando";MotorSerial++; }
   if(!motorRequested && Spinning) { Spinning=false;Deadline=now+.35;State="Frenando";MotorSerial++; }
   if(!motorRequested) { if(now+1e-8>=Deadline)State="Idle";return 0; }
   if(now+1e-8<Deadline)return 0;
   State="Girando";
  }
  // Soldier firing has priority over every reload request and animation deadline.
  if(Class==ClassId.Soldier && left && Ammo>0 && now+1e-8>=NextShot) {
   Reloading=false;Deadline=0;pendingAutoReload=true;
   Ammo--;Shots++;NextShot=now+Interval;State="Disparo";return 1;
  }
  if(reload)Reload(now);
  if(Reloading)return 0;
  if(Ammo==0) { if(!Reload(now))State="Sin municion";return 0; }
  bool ready=left;
  if(Class==ClassId.Pyro) {
   if(!ready) { State="Idle";NextConsume=now;NextShot=Math.Max(NextShot,now);return 0; }
   if(now+1e-8>=NextConsume) { Ammo--;NextConsume=now-NextConsume<.08?NextConsume+.08:now+.08; }
  }
  if(ready && now+1e-8>=NextShot) {
   if(Class==ClassId.Heavy){heavyHeat=Mathf.Min(1,HeavyHeatAt(now)+1f/12);HeavyLastShot=now;}
   if(Class!=ClassId.Pyro)Ammo--;
   Shots++;NextShot=Class==ClassId.Soldier?now+Interval:(now-NextShot<Interval?NextShot+Interval:now+Interval);State="Disparo";if(Class==ClassId.Soldier)pendingAutoReload=true;return 1;
  }
  if(Class==ClassId.Soldier && pendingAutoReload && now+1e-8>=NextShot)Reload(now);
  if(Class!=ClassId.Heavy && !Reloading && now>=NextShot)State="Idle";
  return 0;
 }
}
[Serializable] public class ClassView {
 public ClassId id;
 public Animation animation;
 public Transform muzzle;
 public float health;
}
[RequireComponent(typeof(CharacterController))]
public sealed class FpsStage2Player : MonoBehaviour {
 public Camera playerCamera;
 public ClassView[] views;
 public Material fxMaterial, flameMaterial;
 public ClassId currentClass;
 public WeaponClock[] clocks;
 public float sensitivity=.08f;
 public bool Crouched {get;private set;}
 public float CurrentSpeed {get;private set;}
 public int SwitchCount {get;private set;}
 public string AnimationName {get;private set;}="Idle";
 public float[] speeds={7f,5.7f,4.57f,4.38f};
 public float[] backwards={8.4f,5.13f,4.113f,3.942f};
 public float[] crouchSpeeds={3.08f,1.881f,4.57f/3f,4.38f*77f/230f};
 public int[] maxHealth={125,173,200,300};
 CharacterController controller;
 Stage2Effects effects;
 Stage2WeaponCollision weaponCollision;
 float pitch,fallSpeed;
 Vector3 sceneSpawn;
 public float jumpHeight=1.2f;
 public int JumpsUsed {get;private set;}
 public float VerticalSpeed=>fallSpeed;
 int seenReload,seenMotor;
 double animationUntil;
 Transform heavyBarrel;
 Quaternion heavyBarrelRest;
 Vector3 heavyBarrelPosition,heavyAxis;
 float heavyAngle,heavySpeed;
 public float HeavySpinSpeed=>heavySpeed;
 public int HeavyLowerStarts {get;private set;}
 bool applicationFocused=true;
 readonly RaycastHit[] flameHits=new RaycastHit[24];
 readonly Collider[] explosionHits=new Collider[32];
 readonly System.Collections.Generic.HashSet<Stage2Damageable> damaged=new System.Collections.Generic.HashSet<Stage2Damageable>();
 public WeaponClock Clock=>clocks[(int)currentClass];
 public ClassView View=>views[(int)currentClass];
 public Stage2Effects Effects=>effects;
 public void MatchStopWeapon(){Clock?.Cancel();effects?.StopFlame();}
 public void MatchRespawn(Vector3 spawn){controller.enabled=false;transform.position=spawn;controller.enabled=true;fallSpeed=0;JumpsUsed=0;for(int i=0;i<4;i++){clocks[i]=new WeaponClock((ClassId)i);views[i].health=maxHealth[i];}SelectClass(currentClass);}
 void Awake() {
  maxHealth[(int)ClassId.Pyro]=173;
  controller=GetComponent<CharacterController>();
  sceneSpawn=transform.position;
  weaponCollision=GetComponent<Stage2WeaponCollision>();
  if(weaponCollision==null)weaponCollision=gameObject.AddComponent<Stage2WeaponCollision>();
  weaponCollision.Initialize(this);
  clocks=new WeaponClock[4];for(int i=0;i<4;i++){clocks[i]=new WeaponClock((ClassId)i);views[i].health=maxHealth[i];}
  effects=gameObject.AddComponent<Stage2Effects>();effects.Initialize(playerCamera,fxMaterial,flameMaterial,this);
  foreach(var t in views[(int)ClassId.Heavy].animation.GetComponentsInChildren<Transform>(true))if(t.name=="BarrelSpin")heavyBarrel=t;
  if(heavyBarrel!=null){heavyBarrelRest=heavyBarrel.localRotation;heavyBarrelPosition=heavyBarrel.localPosition;heavyAxis=heavyBarrel.InverseTransformDirection(views[3].muzzle.position-heavyBarrel.position).normalized;}
 }
 void Start(){SelectClass(currentClass);}
 void OnApplicationFocus(bool focus){applicationFocused=focus;if(!focus)Clock.Cancel();}
 void OnDisable(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
 public void SelectClass(ClassId id) {
  if(clocks==null)return;
  Clock.Cancel();currentClass=id;Clock.Cancel();seenReload=Clock.ReloadSerial;seenMotor=Clock.MotorSerial;
  heavySpeed=0;
  for(int i=0;i<views.Length;i++)views[i].animation.gameObject.SetActive(i==(int)id);
  effects?.StopFlame();Play("Idle",0,true);SwitchCount++;
 }
 public float SpeedFor(float forward,bool crouch) => crouch?crouchSpeeds[(int)currentClass]:forward<0?backwards[(int)currentClass]:speeds[(int)currentClass];
 public void MoveInput(Vector2 input,bool crouch,float dt,bool jumpPressed=false) {
  input=Vector2.ClampMagnitude(input,1);
  bool canStand=!Physics.CheckCapsule(transform.position+Vector3.up*.95f,transform.position+Vector3.up*1.55f,.28f,1,QueryTriggerInteraction.Ignore);
  Crouched=crouch || (Crouched&&!canStand);
  float h=Crouched?1.05f:1.8f;controller.height=h;controller.center=Vector3.up*(h*.5f);
  var cp=playerCamera.transform.localPosition;cp.y=Mathf.MoveTowards(cp.y,Crouched?.86f:1.65f,5*dt);playerCamera.transform.localPosition=cp;
  CurrentSpeed=SpeedFor(input.y,Crouched);
  bool grounded=controller.isGrounded && fallSpeed<=0;
  if(grounded){fallSpeed=-2;JumpsUsed=0;}
  if(jumpPressed && (grounded || currentClass==ClassId.Scout && JumpsUsed<2)){
   JumpsUsed=grounded?1:2;fallSpeed=Mathf.Sqrt(2*20*Mathf.Max(.1f,jumpHeight*(currentClass==ClassId.Scout?1.2f:1f)));
  }
  fallSpeed=Mathf.Max(fallSpeed-20*dt,-40);
  var movement=(transform.TransformDirection(new Vector3(input.x,0,input.y))*CurrentSpeed+Vector3.up*fallSpeed)*dt;
  var collisions=controller.Move(movement);
  if((collisions&CollisionFlags.Above)!=0 && fallSpeed>0)fallSpeed=0;
  if((collisions&CollisionFlags.Below)!=0 && fallSpeed<=0){fallSpeed=-2;JumpsUsed=0;}
  if(transform.position.y < -12){controller.enabled=false;transform.position=sceneSpawn;controller.enabled=true;fallSpeed=0;JumpsUsed=0;}
 }
 void Update() {
  if(CPMatch.Instance!=null&&CPMatch.Instance.InputBlocked){CPWebMouse.Read(Vector2.zero,false);return;}
  if(!applicationFocused){CPWebMouse.Read(Vector2.zero,false);return;}
  var keyboard=Keyboard.current;var mouse=Mouse.current;if(keyboard==null||mouse==null)return;
  if(CPMatch.Instance==null&&keyboard.escapeKey.wasPressedThisFrame){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;Clock.Cancel();effects.StopFlame();}
  if(keyboard.commaKey.wasPressedThisFrame){if(CPMatch.Instance!=null)return;SelectClass((ClassId)(((int)currentClass+1)%4));}
  if(mouse.leftButton.wasPressedThisFrame||mouse.rightButton.wasPressedThisFrame){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
  var pointerDelta=CPWebMouse.Read(mouse.delta.ReadValue(),Cursor.lockState==CursorLockMode.Locked);
  if(Cursor.lockState==CursorLockMode.Locked){var look=pointerDelta*sensitivity;transform.Rotate(0,look.x,0);pitch=Mathf.Clamp(pitch-look.y,-80,80);playerCamera.transform.localRotation=Quaternion.Euler(pitch,0,0);}
  var move=new Vector2((keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0),(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0));
  MoveInput(move,keyboard.leftCtrlKey.isPressed||keyboard.rightCtrlKey.isPressed,Time.deltaTime,keyboard.spaceKey.wasPressedThisFrame);
  HandleWeapon(Time.timeAsDouble,Cursor.lockState==CursorLockMode.Locked&&mouse.leftButton.isPressed,Cursor.lockState==CursorLockMode.Locked&&mouse.rightButton.isPressed,keyboard.rKey.wasPressedThisFrame);
 }
 public void HandleWeapon(double now,bool left,bool right,bool reload) {
  if(CPMatch.Instance!=null&&(!CPMatch.Instance.WeaponsAvailable||CPMatch.Instance.InputBlocked)){MatchStopWeapon();return;}
  if(currentClass==ClassId.Pyro&&right&&Clock.TryAirblast(now)){
   effects.StopFlame();CPMatch.Instance?.combat.Airblast(CPMatch.Instance.PlayerActor,playerCamera.transform.forward);return;
  }
  if(currentClass==ClassId.Pyro&&now<Clock.NextAirblast){effects.StopFlame();return;}
  int fired=Clock.Step(now,left,right,reload);
  if(Clock.ShouldPlayEmpty(now,left,fired))CPMatch.Instance?.audioSystem?.EmptyTrigger();
  if(Clock.Reloading && seenReload!=Clock.ReloadSerial){seenReload=Clock.ReloadSerial;Play("Recarga",Math.Max(.01,Clock.Deadline-now),false);}
  if(currentClass==ClassId.Heavy) {
   if(Clock.State=="Bajando"||Clock.State=="Acelerando") {if(seenMotor!=Clock.MotorSerial){seenMotor=Clock.MotorSerial;Play("Bajar",Math.Max(.01,Clock.Deadline-now),false);}}
   else if(Clock.State=="Frenando") {if(seenMotor!=Clock.MotorSerial){seenMotor=Clock.MotorSerial;var st=View.animation[AnimationName];if(st!=null){st.speed=0;st.wrapMode=WrapMode.ClampForever;}}}
   else if(Clock.SpinningReady(now)) {if(AnimationName!="Giro")Play("Giro",.6,true);}
   else if(Clock.State=="Idle"&&AnimationName!="Idle")Play("Idle",0,true);
  }
  else if(currentClass!=ClassId.Pyro && !Clock.Reloading && now>=animationUntil && AnimationName!="Idle")Play("Idle",0,true);
  if(fired>0) {
   if(currentClass==ClassId.Soldier){View.animation.Stop("Recarga");seenReload=Clock.ReloadSerial;animationUntil=now;}
   if(currentClass==ClassId.Scout||currentClass==ClassId.Soldier){double duration=currentClass==ClassId.Soldier?.6:Clock.Interval;Play("Disparo",duration,false);animationUntil=now+duration;}
   View.animation.Sample();weaponCollision?.ResolveWalls();Shoot();
  }
  effects.FlameActive=currentClass==ClassId.Pyro&&left&&Clock.Ammo>0;
  effects.FlameOrigin=View.muzzle;
 }
 void LateUpdate(){
  if(heavyBarrel==null||clocks==null)return;
  float target=currentClass==ClassId.Heavy&&Clock.Spinning?1200:0;
  heavySpeed=Mathf.MoveTowards(heavySpeed,target,(target>0?1400:3500)*Time.deltaTime);
  heavyAngle=Mathf.Repeat(heavyAngle+heavySpeed*Time.deltaTime,360);
  // The spindle only rotates. Its local centre and axis never orbit or translate.
  heavyBarrel.localPosition=heavyBarrelPosition;
  heavyBarrel.localRotation=heavyBarrelRest*Quaternion.AngleAxis(heavyAngle,heavyAxis);
 }
 void Play(string clip,double duration,bool loop) {
  if(currentClass==ClassId.Heavy&&clip=="Bajar")HeavyLowerStarts++;
  var a=View.animation;var st=a[clip];if(st==null)return;st.wrapMode=loop?WrapMode.Loop:WrapMode.Once;st.speed=duration>0?st.length/(float)duration:1;st.time=0;if(currentClass==ClassId.Heavy)a.CrossFade(clip,.18f,PlayMode.StopAll);else a.Play(clip,PlayMode.StopAll);AnimationName=clip;
  animationUntil=duration>0?Time.timeAsDouble+duration:double.PositiveInfinity;
 }
 void Shoot() {
  if(CPMatch.Instance!=null){var actor=CPMatch.Instance.PlayerActor;actor.SyncClass();CPMatch.Instance.combat.Fire(actor,playerCamera.transform.forward);return;}
  Vector3 origin=playerCamera.transform.position,forward=playerCamera.transform.forward;
  if(currentClass==ClassId.Pyro){int count=Physics.SphereCastNonAlloc(origin,.3f,forward,flameHits,5,1,QueryTriggerInteraction.Ignore);damaged.Clear();for(int i=0;i<count;i++){var target=flameHits[i].collider.GetComponentInParent<Stage2Damageable>();if(target!=null&&damaged.Add(target)){float d=Mathf.Lerp(13,6.5f,flameHits[i].distance/5);target.Hit(d);target.Burn(Mathf.Lerp(10,4,flameHits[i].distance/5));}}return;}
  if(currentClass==ClassId.Soldier){effects.Rocket(View.muzzle.position,forward,origin);return;}
  effects.Flash(View.muzzle.position,.12f);
  if(currentClass==ClassId.Scout)effects.ScoutTracers(View.muzzle.position,playerCamera.transform.right,origin+forward*80);
  int pellets=currentClass==ClassId.Scout?12:4;float spread=Clock.SpreadTangent(Time.timeAsDouble);
  for(int i=0;i<pellets;i++){
   Vector2 offset=UnityEngine.Random.insideUnitCircle*spread;
   Vector3 dir=(forward+playerCamera.transform.right*offset.x+playerCamera.transform.up*offset.y).normalized;
   bool didHit=Physics.Raycast(origin,dir,out var hit,100,1,QueryTriggerInteraction.Ignore);
   Vector3 endpoint=didHit?hit.point:origin+dir*100;
   // Each visual bullet follows the same sampled pellet that resolves damage.
   if(currentClass==ClassId.Heavy)effects.HeavyTracer(View.muzzle.position,(View.muzzle.position-heavyBarrel.position).normalized,endpoint,i);
   if(didHit){float baseDamage=currentClass==ClassId.Scout?5.4f:9;float mult=Mathf.Lerp(currentClass==ClassId.Scout?1.75f:1.5f,.528f,Mathf.Clamp01(hit.distance/30));hit.collider.GetComponentInParent<Stage2Damageable>()?.Hit(baseDamage*mult);effects.Impact(hit.point);}
  }
 }
 public void Explode(Vector3 point,Vector3 shotOrigin){effects.Explosion(point);damaged.Clear();int n=Physics.OverlapSphereNonAlloc(point,2.8f,explosionHits,1,QueryTriggerInteraction.Ignore);float travel=Vector3.Distance(point,shotOrigin);float direct=Mathf.Lerp(112,48,Mathf.Clamp01(travel/30));for(int i=0;i<n;i++){var t=explosionHits[i].GetComponentInParent<Stage2Damageable>();if(t==null||!damaged.Add(t))continue;float dist=Vector3.Distance(point,explosionHits[i].ClosestPoint(point));t.Hit(direct*Mathf.Lerp(1,.5f,Mathf.Clamp01(dist/2.8f)));}
  float self=Vector3.Distance(point,transform.position+Vector3.up*.8f);if(self<2.8f){View.health=Mathf.Max(0,View.health-Mathf.Lerp(89,27,self/2.8f));if(View.health<=0){View.health=maxHealth[(int)currentClass];controller.enabled=false;transform.position=sceneSpawn;controller.enabled=true;}}
 }
 void OnGUI(){if(clocks==null||CPMatch.Instance!=null)return;GUI.color=new Color(.08f,.10f,.14f,.9f);GUI.DrawTexture(new Rect(18,18,710,42),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(18,Screen.height-84,260,64),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(30,26,700,28),"WASD mover | ESPACIO saltar (Scout x2) | CTRL agacharse | , clase | R recargar | ESC cursor");GUI.Label(new Rect(30,Screen.height-77,250,24),currentClass+"   SALUD "+Mathf.CeilToInt(View.health)+"   "+(Crouched?"AGACHADO":""));GUI.Label(new Rect(30,Screen.height-51,250,25),"MUNICION "+Clock.Ammo+(Clock.Reserve>0?" / "+Clock.Reserve:"")+"   "+Clock.State);GUI.DrawTexture(new Rect(Screen.width/2-4,Screen.height/2,9,1),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(Screen.width/2,Screen.height/2-4,1,9),Texture2D.whiteTexture);
  if(currentClass==ClassId.Heavy)GUI.Label(new Rect(Screen.width-430,Screen.height-48,420,30),"IZQ: preparar y disparar | DER: mantener giro");}
}
}
