using UnityEngine;
namespace FpsStage2 {
public enum CPSound { ScoutShot,SoldierShot,HeavyShot,Flame,Motor,Reload,Step,Jump,Land,Hurt,Hit,Death,Health,Ammo,Respawn,Explosion,Capture,Count,Countdown,RoundStart,RoundEnd,Click,Gate,Ambience,
 FlameStart,FlameEnd,WindUp,WindDown,Burn,Impact,Kill,Overtime,Victory,Defeat,LostPoint,ScoutOpen,ScoutOut,ScoutIn,ScoutClose,Empty }
public sealed class CPAudio:MonoBehaviour {
 sealed class ActorAudio {
  public AudioSource weapon,burn;public Vector3 position;public ClassId cls;public float walked,fireUntil,painAt,reloadStart,reloadDuration;public int reloadSerial,step,voice,reloadStage;public bool airborne,spinning,flaming;
 }
 public bool Muted {get;private set;}public float MasterVolume {get;private set;}public int SoundsPlayed {get;private set;}public int DownloadedClipCount=>bank?.ClipCount??0;public bool BankComplete=>bank!=null&&bank.Complete;
 public CPAudioLibrary Library=>bank;
 readonly AudioSource[] voices=new AudioSource[40];ActorAudio[] actors;AudioSource wind,water,turbine,announcer;
 CPMatch match;CPAudioLibrary bank;CharacterController controller;float savedVolume,nextHit,nextImpact,nextCapture,nextEmpty;bool savedPause,ended,overtime;
 public void EmptyTrigger(){if(Time.unscaledTime<nextEmpty)return;nextEmpty=Time.unscaledTime+.35f;Play(CPSound.Empty,Vector3.zero,true,.6f);}
 public void Initialize(CPMatch m){
  match=m;savedVolume=AudioListener.volume;savedPause=AudioListener.pause;Muted=PlayerPrefs.GetInt("PowerhouseMuted",0)!=0;MasterVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(CPSettings.VolumeKey,1));AudioListener.volume=Muted?0:MasterVolume;AudioListener.pause=false;
  bank=new CPAudioLibrary();for(int i=0;i<voices.Length;i++)voices[i]=Source("SFX_"+i);
  actors=new ActorAudio[m.actors.Length];for(int i=0;i<actors.Length;i++){var a=m.actors[i];actors[i]=new ActorAudio{position=a.transform.position,cls=a.characterClass,weapon=Source("Weapon_Loop_"+i),burn=Source("Afterburn_"+i),reloadSerial=a.Clock.ReloadSerial};}
  controller=m.player.GetComponent<CharacterController>();announcer=Source("Announcer");announcer.priority=15;announcer.spatialBlend=0;
  wind=Ambient("Outside_Wind","desert_wind_low",Vector3.zero,0);
  water=Ambient("Waterfall","waterfalloutside",m.zones[1].position,1);water.minDistance=5;water.maxDistance=52;
  turbine=Ambient("Power_Station","turbine1",Vector3.zero,0);
 }
 AudioSource Source(string name){var go=new GameObject(name);go.transform.SetParent(transform,false);var a=go.AddComponent<AudioSource>();a.playOnAwake=false;a.rolloffMode=AudioRolloffMode.Logarithmic;a.minDistance=4;a.maxDistance=75;a.dopplerLevel=0;a.priority=160;return a;}
 AudioSource Ambient(string name,string clip,Vector3 p,float blend){var a=Source(name);a.clip=bank.Get(clip);a.loop=true;a.spatialBlend=blend;a.transform.position=p;a.volume=0;a.priority=230;a.Play();return a;}
 public void ToggleMute(){Muted=!Muted;AudioListener.volume=Muted?0:MasterVolume;PlayerPrefs.SetInt("PowerhouseMuted",Muted?1:0);PlayerPrefs.Save();if(!Muted)Play(CPSound.Click,Vector3.zero,true);}
 public void SetMasterVolume(float value){MasterVolume=Mathf.Clamp01(value);if(MasterVolume>0)Muted=false;AudioListener.volume=Muted?0:MasterVolume;PlayerPrefs.SetFloat(CPSettings.VolumeKey,MasterVolume);PlayerPrefs.SetInt("PowerhouseMuted",Muted?1:0);}
 static float Gain(CPSound cue){switch(cue){case CPSound.Step:return .2f;case CPSound.Land:return .28f;case CPSound.Hit:return .24f;case CPSound.Kill:return .34f;case CPSound.Click:return .45f;case CPSound.Count:return .1f;case CPSound.Explosion:return .62f;case CPSound.ScoutShot:case CPSound.SoldierShot:return .65f;case CPSound.Gate:return .4f;default:return .42f;}}
 static bool Announcement(CPSound k)=>k==CPSound.Capture||k==CPSound.LostPoint||k==CPSound.RoundStart||k==CPSound.Overtime;
 public void Play(CPSound kind,Vector3 position,bool ui=false,float volume=1){if(bank==null)return;var clip=bank.Pick(kind);if(Announcement(kind)){Announce(clip);return;}PlayClip(clip,position,ui,Gain(kind)*volume,kind==CPSound.Step||kind==CPSound.Impact?Random.Range(.97f,1.03f):1);}
 void Announce(AudioClip clip){if(clip==null)return;announcer.Stop();announcer.clip=clip;announcer.volume=.6f;announcer.pitch=1;announcer.Play();SoundsPlayed++;}
 public void Countdown(int seconds){var clip=bank.Countdown(seconds);if(clip!=null)Announce(clip);else if(seconds>0)Play(CPSound.Countdown,Vector3.zero,true,.23f);}
 void PlayClip(AudioClip clip,Vector3 p,bool ui,float gain,float pitch=1){
  if(clip==null||match==null||(!ui&&(p-match.player.transform.position).sqrMagnitude>85*85))return;
  int begin=ui?32:0,end=ui?40:32;AudioSource chosen=null;float weakest=float.PositiveInfinity;
  for(int i=begin;i<end;i++){var candidate=voices[i];if(!candidate.isPlaying){chosen=candidate;break;}float importance=candidate.volume/(1+(candidate.transform.position-match.player.transform.position).magnitude/4);if(importance<weakest){weakest=importance;chosen=candidate;}}
  chosen.Stop();chosen.clip=clip;chosen.transform.position=p;chosen.spatialBlend=ui?0:1;chosen.ignoreListenerPause=ui;chosen.priority=ui?40:150;chosen.volume=Mathf.Clamp01(gain);chosen.pitch=pitch;chosen.loop=false;chosen.Play();SoundsPlayed++;
 }
 public void Shot(CPActor actor){int i=actor.MatchIndex;if(i<0||i>=actors.Length)return;var s=actors[i];if(actor.characterClass==ClassId.Pyro||actor.characterClass==ClassId.Heavy){s.fireUntil=Time.time+.145f;return;}Play(actor.characterClass==ClassId.Scout?CPSound.ScoutShot:CPSound.SoldierShot,actor.muzzle.position,actor.isPlayer,actor.isPlayer?1:.78f);}
 public void Hurt(CPActor victim,CPActor source,bool burning){var s=actors[victim.MatchIndex];if(victim.Alive&&Time.time>=s.painAt){s.painAt=Time.time+(victim.isPlayer?.7f:1.3f);PlayClip(bank.Pain(victim.characterClass,s.voice++),victim.Eye,victim.isPlayer,victim.isPlayer?.5f:.3f);}if(source!=null&&source.isPlayer&&source.team!=victim.team&&Time.time>=nextHit){nextHit=Time.time+.095f;Play(CPSound.Hit,Vector3.zero,true,burning?.65f:1);}}
 public void Death(CPActor actor,CPActor killer){PlayClip(bank.Death(actor.characterClass),actor.Eye,actor.isPlayer,.56f);if(killer!=null&&killer.isPlayer&&killer!=actor&&killer.team!=actor.team)Play(CPSound.Kill,Vector3.zero,true);}
 public void Impact(Vector3 point){if(Time.time<nextImpact)return;nextImpact=Time.time+.08f;Play(CPSound.Impact,point,false,.45f);}
 void SetLoop(AudioSource source,AudioClip clip,CPActor actor,float gain){source.transform.position=actor.Eye;source.spatialBlend=actor.isPlayer?0:1;source.priority=actor.isPlayer?55:165;source.volume=gain;source.pitch=1;if(source.clip!=clip||!source.isPlaying){source.Stop();source.clip=clip;source.loop=true;source.Play();SoundsPlayed++;}}
 void Weapon(CPActor a,ActorAudio s,bool active){
  bool audible=a.isPlayer||(a.transform.position-match.player.transform.position).sqrMagnitude<65*65;
  bool ready=active&&a.Alive&&audible;var c=a.Clock;
  if(s.cls!=a.characterClass){s.weapon.Stop();s.flaming=s.spinning=false;s.fireUntil=0;s.reloadStage=0;s.cls=a.characterClass;s.reloadSerial=c.ReloadSerial;}
  bool spinning=ready&&a.characterClass==ClassId.Heavy&&c.Spinning;
  bool flaming=ready&&a.characterClass==ClassId.Pyro&&Time.time<s.fireUntil;
  float gain=a.isPlayer?.48f:.36f;
  if(spinning){if(!s.spinning)Play(CPSound.WindUp,a.Eye,a.isPlayer,.85f);if(c.SpinningReady(Time.timeAsDouble))SetLoop(s.weapon,bank.Pick(Time.time<s.fireUntil?CPSound.HeavyShot:CPSound.Motor),a,gain);}
  else if(flaming){if(!s.flaming){s.weapon.Stop();s.weapon.clip=bank.Pick(CPSound.FlameStart);s.weapon.loop=false;s.weapon.Play();SoundsPlayed++;}else if(!s.weapon.isPlaying)SetLoop(s.weapon,bank.Pick(CPSound.Flame),a,gain);s.weapon.transform.position=a.Eye;s.weapon.spatialBlend=a.isPlayer?0:1;s.weapon.volume=gain;}
  else {if(s.weapon.isPlaying)s.weapon.Stop();if(ready&&s.flaming)Play(CPSound.FlameEnd,a.Eye,a.isPlayer,.65f);if(ready&&s.spinning)Play(CPSound.WindDown,a.Eye,a.isPlayer,.75f);}
  s.spinning=spinning;s.flaming=flaming;
  if(ready&&a.IsBurning)SetLoop(s.burn,bank.Pick(CPSound.Burn),a,a.isPlayer?.18f:.23f);else s.burn.Stop();
 }
 int Surface(CPActor a){if(!Physics.Raycast(a.transform.position+Vector3.up*.5f,Vector3.down,out var h,2,1,QueryTriggerInteraction.Ignore))return a.characterClass==ClassId.Scout?5:0;string n=h.collider.name;
  if(Has(n,"Water")||Has(n,"River"))return 4;if(Has(n,"Metal")||Has(n,"Steel")||Has(n,"Grate"))return 1;if(Has(n,"Sand")||Has(n,"Rock")||Has(n,"Terrain"))return 2;if(Has(n,"Wood"))return 3;return a.characterClass==ClassId.Scout?5:0;
 }
 static bool Has(string text,string value)=>text.IndexOf(value,System.StringComparison.OrdinalIgnoreCase)>=0;
 void Movement(CPActor a,ActorAudio s){var p=a.transform.position;float distance=Vector3.ProjectOnPlane(p-s.position,Vector3.up).magnitude;bool air=a.isPlayer?!controller.isGrounded&&Mathf.Abs(match.player.VerticalSpeed)>2:a.Brain!=null&&a.Brain.jump.Busy;
  // Jump takeoff is intentionally silent for every class, including bots.
  if(!air&&s.airborne){Play(CPSound.Land,p,a.isPlayer,a.characterClass==ClassId.Heavy?1.25f:1);PlayClip(bank.Step(Surface(a),s.step++),p,a.isPlayer,.22f);}
  s.airborne=air;if(!air&&distance<2){s.walked+=distance;float stride=a.characterClass==ClassId.Scout?2.1f:1.65f;if(s.walked>=stride){s.walked%=stride;PlayClip(bank.Step(Surface(a),s.step++),p,a.isPlayer,a.isPlayer?.26f:.18f,a.characterClass==ClassId.Heavy?.92f:1);}}
 }
 void Reload(CPActor a,ActorAudio s){var c=a.Clock;if(!c.Reloading){s.reloadStage=0;return;}if(s.reloadSerial!=c.ReloadSerial){s.reloadSerial=c.ReloadSerial;s.reloadStart=Time.time;s.reloadDuration=Mathf.Max(.1f,(float)(c.Deadline-Time.time));s.reloadStage=0;if(a.characterClass==ClassId.Scout)Play(CPSound.ScoutOpen,a.Eye,a.isPlayer);}
  float progress=(Time.time-s.reloadStart)/s.reloadDuration;
  if(a.characterClass==ClassId.Soldier){if(s.reloadStage==0&&progress>=.52f){Play(CPSound.Reload,a.Eye,a.isPlayer);s.reloadStage=1;}}
  else if(a.characterClass==ClassId.Scout){if(s.reloadStage==0&&progress>=.20f){Play(CPSound.ScoutOut,a.Eye,a.isPlayer);s.reloadStage=1;}if(s.reloadStage==1&&progress>=.57f){Play(CPSound.ScoutIn,a.Eye,a.isPlayer);s.reloadStage=2;}if(s.reloadStage==2&&progress>=.86f){Play(CPSound.ScoutClose,a.Eye,a.isPlayer);s.reloadStage=3;}}
 }
 void Update(){if(match==null||match.flow==null)return;bool main=match.flow.Phase==CPRoundPhase.MainMenu;AudioListener.pause=match.MenuOpen;bool active=!main&&!match.MenuOpen&&!match.rules.Finished;
  float x=Mathf.Abs(match.player.transform.position.x);wind.volume=main?0:.055f;water.volume=main?0:.22f;turbine.volume=main?0:Mathf.Lerp(.015f,.07f,Mathf.InverseLerp(35,100,x));
  for(int i=0;i<actors.Length;i++){var a=match.actors[i];var s=actors[i];bool running=active&&(a.isPlayer||match.RoundLive);Weapon(a,s,running);if(running&&a.Alive){Movement(a,s);Reload(a,s);}s.position=a.transform.position;}
  if(active&&match.RoundLive&&Time.time>nextCapture){nextCapture=Time.time+.95f;for(int i=0;i<3;i++)if(match.rules.Points[i].Progress>0&&!match.rules.Points[i].Contested)Play(CPSound.Count,match.zones[i].position,false);}
  if(match.rules.Overtime&&!overtime){overtime=true;Play(CPSound.Overtime,Vector3.zero,true);}
  if(match.rules.Finished&&!ended){ended=true;Play(match.rules.Winner==CPTeam.Neutral?CPSound.RoundEnd:match.rules.Winner==match.PlayerActor.team?CPSound.Victory:CPSound.Defeat,Vector3.zero,true);}
 }
 void OnDestroy(){AudioListener.volume=savedVolume;AudioListener.pause=savedPause;}
}
}
