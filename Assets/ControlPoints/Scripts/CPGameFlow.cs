using UnityEngine;
using UnityEngine.InputSystem;
namespace FpsStage2 {
public enum CPRoundPhase { MainMenu, Preparation, Playing }
// The preparation clock is separate from the five-minute capture clock.
public sealed class CPGameFlow:MonoBehaviour {
 public static bool SkipMenuOnce;
 public CPRoundPhase Phase {get;private set;}
 public bool ClassMenuOpen {get;private set;}
 public float PreparationRemaining {get;private set;}
 public bool Live=>Phase==CPRoundPhase.Playing;
 public ClassId? PendingClass {get;private set;}
 CPMatch match;int lastSecond=-1;
 public void Initialize(CPMatch m){match=m;CPDeathPresentation.Warmup(m.actors);Phase=CPRoundPhase.MainMenu;foreach(var gate in m.spawnGates)if(gate!=null)gate.Close();SetCursor(true);Time.timeScale=0;if(SkipMenuOnce){SkipMenuOnce=false;Begin();}}
 void Update(){if(match==null)return;var keys=Keyboard.current;if(keys!=null&&keys.commaKey.wasPressedThisFrame&&Phase!=CPRoundPhase.MainMenu&&!match.MenuOpen&&!match.rules.Finished)ToggleClasses();
  if(Phase!=CPRoundPhase.Preparation||match.MenuOpen)return;
  PreparationRemaining=Mathf.Max(0,PreparationRemaining-Time.deltaTime);int sec=Mathf.CeilToInt(PreparationRemaining);
  if(sec!=lastSecond){lastSecond=sec;if(sec>0)match.audioSystem?.Countdown(sec);}
  if(PreparationRemaining>0)return;Phase=CPRoundPhase.Playing;foreach(var gate in match.spawnGates)if(gate!=null)gate.Open();match.audioSystem?.Play(CPSound.RoundStart,Vector3.zero,true);
 }
 public void Begin(){if(Phase!=CPRoundPhase.MainMenu)return;Phase=CPRoundPhase.Preparation;PreparationRemaining=10;lastSecond=-1;Time.timeScale=1;SetCursor(false);match.audioSystem?.Play(CPSound.Click,Vector3.zero,true);}
 public void ToggleClasses(){ClassMenuOpen=!ClassMenuOpen;match.player.MatchStopWeapon();SetCursor(ClassMenuOpen);match.audioSystem?.Play(CPSound.Click,Vector3.zero,true);}
 public void CloseClasses(){ClassMenuOpen=false;SetCursor(match.MenuOpen||match.rules.Finished||Phase==CPRoundPhase.MainMenu);}
 public bool InOwnBase(CPActor actor){int i=actor.team==CPTeam.RED?0:1;return match.teamBases!=null&&i<match.teamBases.Length&&match.teamBases[i].Contains(actor.transform.position+Vector3.up*.5f);}
 public void ChooseClass(ClassId selected){if((int)selected<0||(int)selected>3||Phase==CPRoundPhase.MainMenu)return;var a=match.PlayerActor;CloseClasses();match.audioSystem?.Play(CPSound.Click,Vector3.zero,true);
  if(selected==a.characterClass){PendingClass=null;return;}PendingClass=selected;
  if(!a.Alive)return;
  if(InOwnBase(a)){ApplyPendingClass();match.player.clocks[(int)selected]=new WeaponClock(selected);match.player.View.health=match.player.maxHealth[(int)selected];a.SyncClass();a.HealAndSupply();match.audioSystem?.Play(CPSound.Respawn,a.transform.position,true);}
  else a.Damage(a.health+1,null,true,a.Eye);
 }
 public void ApplyPendingClass(){if(!PendingClass.HasValue)return;var selected=PendingClass.Value;PendingClass=null;match.PlayerActor.characterClass=selected;match.player.SelectClass(selected);}
 public static void SetCursor(bool visible){Cursor.lockState=visible?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=visible;}
}
}
