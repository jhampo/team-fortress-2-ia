using System;
using System.Collections.Generic;
using UnityEngine;
namespace FpsStage2 {
// Downloaded Valve audio. Provenance and hashes: ControlPoints/AUDIO_SOURCES.json.
// Loaded before play: no downloads, synthesis or disk I/O during combat.
public sealed class CPAudioLibrary {
 readonly Dictionary<string,AudioClip> files=new Dictionary<string,AudioClip>(StringComparer.Ordinal);
 readonly AudioClip[][] cues=new AudioClip[Enum.GetValues(typeof(CPSound)).Length][];
 readonly int[] last=new int[Enum.GetValues(typeof(CPSound)).Length];
 readonly AudioClip[][] pain=new AudioClip[4][],death=new AudioClip[4][],steps=new AudioClip[6][];
 readonly AudioClip[] countdown=new AudioClip[11];
 public int ClipCount=>files.Count;
 public CPAudioLibrary(){
  foreach(var clip in Resources.LoadAll<AudioClip>("PowerhouseAudio")){files.Add(clip.name,clip);clip.LoadAudioData();}
  for(int i=0;i<last.Length;i++)last[i]=-1;
  Bind(CPSound.ScoutShot,"scatter_gun_double_shoot");Bind(CPSound.SoldierShot,"rocket_shoot");Bind(CPSound.HeavyShot,"minigun_shoot");
  Bind(CPSound.Flame,"flame_thrower_loop");Bind(CPSound.Motor,"minigun_spin");Bind(CPSound.Reload,"rocket_reload");Bind(CPSound.Empty,"buttonclick");
  Bind(CPSound.Step,"concrete1","concrete2","concrete3","concrete4");Bind(CPSound.Jump,"pl_scout_jump1","pl_scout_jump2");
  Bind(CPSound.Land,"body_medium_impact_soft1","body_medium_impact_soft2");Bind(CPSound.Hurt,"scout_painsharp01","scout_painsharp02");Bind(CPSound.Death,"scout_paincrticialdeath01");
  Bind(CPSound.Hit,"hitsound");Bind(CPSound.Health,"smallmedkit1");Bind(CPSound.Ammo,"ammo_pickup","gunpickup2");Bind(CPSound.Respawn,"regenerate");
  Bind(CPSound.Explosion,"explode1","explode2","explode3");Bind(CPSound.Capture,"announcer_we_captured_control");Bind(CPSound.Count,"blip1");Bind(CPSound.Countdown,"clock_tick");
  Bind(CPSound.RoundStart,"announcer_am_roundstart01");Bind(CPSound.RoundEnd,"your_team_stalemate");Bind(CPSound.Click,"buttonclick");Bind(CPSound.Gate,"shutter4");Bind(CPSound.Ambience,"desert_wind_low");
  Bind(CPSound.FlameStart,"flame_thrower_start");Bind(CPSound.FlameEnd,"flame_thrower_end");Bind(CPSound.WindUp,"minigun_wind_up");Bind(CPSound.WindDown,"minigun_wind_down");
  Bind(CPSound.Burn,"fire_small_loop2");Bind(CPSound.Impact,"ric1","ric2","ric3");Bind(CPSound.Kill,"killsound");Bind(CPSound.Overtime,"announcer_overtime");
  Bind(CPSound.Victory,"your_team_won");Bind(CPSound.Defeat,"your_team_lost");Bind(CPSound.LostPoint,"announcer_we_lost_control");
  Bind(CPSound.ScoutOpen,"scatter_gun_double_tube_open");Bind(CPSound.ScoutOut,"scatter_gun_double_shells_out");Bind(CPSound.ScoutIn,"scatter_gun_double_shells_in");Bind(CPSound.ScoutClose,"scatter_gun_double_tube_close");
  string[] classNames={"scout","pyro","soldier","heavy"};
  for(int i=0;i<4;i++){pain[i]=Set(classNames[i]+"_painsharp01",classNames[i]+"_painsharp02",classNames[i]+"_painsevere01");death[i]=Set(classNames[i]+"_paincrticialdeath01");}
  string[] surfaces={"concrete","metalgrate","dirt","wood","slosh","cleats_conc_0"};
  for(int i=0;i<surfaces.Length;i++)steps[i]=Set(surfaces[i]+"1",surfaces[i]+"2",surfaces[i]+"3",surfaces[i]+"4");
  foreach(int sec in new[]{1,2,3,4,5,10})countdown[sec]=Get("announcer_begins_"+sec+"sec");
 }
 public AudioClip Get(string name){if(!files.TryGetValue(name,out var c))throw new InvalidOperationException("Missing downloaded audio: "+name);return c;}
 AudioClip[] Set(params string[] names){var result=new AudioClip[names.Length];for(int i=0;i<names.Length;i++)result[i]=Get(names[i]);return result;}
 void Bind(CPSound cue,params string[] names){cues[(int)cue]=Set(names);}
 public AudioClip Pick(CPSound cue){var group=cues[(int)cue];if(group==null||group.Length==0)throw new InvalidOperationException("Unmapped audio cue: "+cue);int index=group.Length==1?0:(last[(int)cue]+UnityEngine.Random.Range(1,group.Length))%group.Length;last[(int)cue]=index;return group[index];}
 public AudioClip Pain(ClassId cls,int variation)=>pain[(int)cls][variation%pain[(int)cls].Length];
 public AudioClip Death(ClassId cls)=>death[(int)cls][0];
 public AudioClip Step(int surface,int variation)=>steps[Mathf.Clamp(surface,0,5)][variation%4];
 public AudioClip Countdown(int sec)=>sec>=0&&sec<countdown.Length?countdown[sec]:null;
 public bool Complete {get{foreach(var group in cues)if(group==null||group.Length==0)return false;return files.Count>=96;}}
}
}
