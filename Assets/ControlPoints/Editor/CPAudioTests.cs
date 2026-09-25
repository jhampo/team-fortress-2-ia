using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace FpsStage2 {
public static class CPAudioTests {
 static int checks;static void Check(bool value,string message){checks++;if(!value)throw new Exception("Audio/menu: "+message);}
 public static string Assets(){checks=0;var library=new CPAudioLibrary();Check(library.ClipCount==96,"96 downloaded clips");Check(library.Complete,"every cue mapped");foreach(CPSound cue in Enum.GetValues(typeof(CPSound)))Check(library.Pick(cue)!=null,"cue "+cue);
  float loudest=0;foreach(var clip in Resources.LoadAll<AudioClip>("PowerhouseAudio")){Check(clip.length>.015f&&clip.channels==1&&clip.frequency==44100,"valid decoded PCM "+clip.name);Check(!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(clip)),"persistent clip asset");clip.LoadAudioData();var data=new float[clip.samples];Check(clip.GetData(data,0),"readable samples "+clip.name);double energy=0;float peak=0;foreach(float f in data){Check(!float.IsNaN(f)&&!float.IsInfinity(f),"finite sample");energy+=f*f;peak=Mathf.Max(peak,Mathf.Abs(f));}Check(energy>1e-7&&peak>.0001f,"non-silent "+clip.name);loudest=Mathf.Max(loudest,peak);}
  return checks+" audio asset/sample checks; 96 clips; peak="+loudest.ToString("F4");
 }
 public static string Preparation(){checks=0;var m=CPMatch.Instance;Check(m!=null&&m.flow.Phase==CPRoundPhase.MainMenu,"start on main menu");var previous=m.difficulty;for(int d=0;d<4;d++){m.SelectDifficulty((CPDifficulty)d);Check((int)m.difficulty==d&&m.flow.Phase==CPRoundPhase.MainMenu,"difficulty selected without starting match");}m.SelectDifficulty(previous);
  m.flow.Begin();Check(m.flow.Phase==CPRoundPhase.Preparation&&!m.RoundLive&&m.WeaponsAvailable,"weapons active before gates open");Check(m.spawnGates.All(g=>g.barrier.enabled),"both gates remain solid");Check(m.rules.Remaining==300,"capture timer not started");
  foreach(ClassId cls in Enum.GetValues(typeof(ClassId))){m.flow.ChooseClass(cls);var player=m.player;var c=player.Clock;c.Ammo=c.Capacity;c.NextShot=-1;c.NextConsume=Time.timeAsDouble;if(cls==ClassId.Heavy){c.Spinning=true;c.Deadline=-1;}int shots=c.Shots;player.HandleWeapon(Time.timeAsDouble,true,false,false);Check(c.Shots==shots+1&&c.Ammo<c.Capacity,"preparation shot for "+cls);player.HandleWeapon(Time.timeAsDouble,false,false,false);if(cls==ClassId.Scout||cls==ClassId.Soldier){c.NextShot=-1;player.HandleWeapon(Time.timeAsDouble,false,false,true);Check(c.Reloading,"preparation reload for "+cls);}player.MatchStopWeapon();}
  m.flow.ChooseClass(ClassId.Scout);Check(m.rules.Remaining==300,"test shots do not advance capture time");m.ToggleMenu();Check(m.MenuOpen&&Time.timeScale==0,"ESC pauses preparation");var pause=m.GetComponent<CPHud>().RootCanvas.GetComponentsInChildren<RectTransform>(true).First(t=>t.name=="Pause");var actions=pause.GetComponentsInChildren<Button>(true);Check(actions.Length==1&&actions[0].GetComponentInChildren<Text>().text=="IR AL MENÚ","pause has only return-to-menu action");m.ToggleMenu();Check(!m.MenuOpen&&Time.timeScale==1,"ESC resumes");return checks+" preparation, weapons, difficulty and pause checks passed";
 }
 public static string Runtime(){checks=0;var m=CPMatch.Instance;Check(m.audioSystem.BankComplete,"audio bank complete");Check(m.audioSystem.DownloadedClipCount==96,"all clips preloaded");Check(m.GetComponentsInChildren<AudioSource>().Length==76,"bounded source pool");var original=m.audioSystem.Muted;m.audioSystem.ToggleMute();Check(m.audioSystem.Muted!=original,"mute toggles");m.audioSystem.ToggleMute();Check(m.audioSystem.Muted==original,"mute restored");return checks+" runtime audio checks passed";}
}
}
