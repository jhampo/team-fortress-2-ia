using UnityEngine;
namespace FpsStage2 {
[System.Serializable] public sealed class CPScore {
 public string Name;public int Kills,Deaths,Assists,Captures;public float Damage;
 public int Points=>Kills*2+Assists+Captures*3;
 public void Reset(string name){Name=name;Kills=Deaths=Assists=Captures=0;Damage=0;}
}
public sealed class CPScorekeeper {
 static readonly string[] Names={"AimBot","AmNot","Aperture","GLaDOS","Dog","Divide by Zero","C++","DeadHead","BoomerBile","Black Mesa","Big Mean Muther Hubbard","BeepBeepBoop","Archimedes!","Force of Nature","Delicious Cake","Gentlemanne of Leisure"};
 readonly CPMatch match;readonly float[,] lastHit;readonly CPTeam[] owners=new CPTeam[3];
 public int Revision {get;private set;}
 public CPScorekeeper(CPMatch m){match=m;int n=m.actors.Length;lastHit=new float[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)lastHit[i,j]=-100;int bot=0;for(int i=0;i<n;i++){var a=m.actors[i];a.MatchIndex=i;a.score=new CPScore();a.score.Reset(a.isPlayer?"player-astra-gpt":Names[bot++%Names.Length]);}for(int i=0;i<3;i++)owners[i]=m.rules.Points[i].Owner;}
 bool Registered(CPActor a)=>a!=null&&a.MatchIndex>=0&&a.MatchIndex<match.actors.Length&&match.actors[a.MatchIndex]==a;
 public void Damage(CPActor victim,CPActor attacker,float amount){if(!Registered(victim)||!Registered(attacker)||victim.team==attacker.team)return;lastHit[victim.MatchIndex,attacker.MatchIndex]=Time.time;attacker.score.Damage+=amount;Revision++;}
 public void Death(CPActor victim,CPActor killer){if(!Registered(victim))return;victim.score.Deaths++;bool enemy=Registered(killer)&&killer!=victim&&killer.team!=victim.team;if(enemy)killer.score.Kills++;
  for(int i=0;i<match.actors.Length;i++){var a=match.actors[i];if(enemy&&a!=killer&&a.team==killer.team&&Time.time-lastHit[victim.MatchIndex,i]<=8)a.score.Assists++;lastHit[victim.MatchIndex,i]=-100;}Revision++;
 }
 public void Respawn(CPActor actor){if(!Registered(actor))return;for(int i=0;i<match.actors.Length;i++)lastHit[actor.MatchIndex,i]=-100;Revision++;}
 public void Captures(){for(int i=0;i<3;i++){var owner=match.rules.Points[i].Owner;if(owner==owners[i])continue;owners[i]=owner;foreach(var a in match.actors)if(a.Alive&&a.team==owner&&match.zones[i].Contains(a.transform.position))a.score.Captures++;Revision++;match.audioSystem?.Play(owner==match.PlayerActor.team?CPSound.Capture:CPSound.LostPoint,match.zones[i].position,true);}}
}
}
