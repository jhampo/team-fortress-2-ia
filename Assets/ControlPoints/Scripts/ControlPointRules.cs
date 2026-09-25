using System;
namespace FpsStage2 {
public enum CPTeam { Neutral, RED, BLU }
public enum CPDifficulty { Facil, Normal, Dificil, Experto }
[Serializable] public sealed class CPPointState {
 public CPTeam Owner, Capturing; public bool Locked; public float Progress; public int RedCount, BlueCount;
 public bool Contested => RedCount>0 && BlueCount>0;
 public int Attackers => Locked?0:Owner==CPTeam.RED?BlueCount:Owner==CPTeam.BLU?RedCount:RedCount+BlueCount;
}
// Pure state machine: no scene, frame-rate, UI, or AI dependencies.
[Serializable] public sealed class ControlPointRules {
 public CPPointState[] Points; public float Remaining=300; public bool Overtime, Finished; public CPTeam Winner;
 public float CenterSeconds=24, BaseSeconds=32, CenterBonus=60, BaseRespawn=5, DefenderPenalty=4;
 public int CenterCaptures; public string EndReason="";
 readonly float[] rates=new float[3];
 public ControlPointRules(){Reset();}
 public void Reset(){Points=new[]{new CPPointState{Owner=CPTeam.RED,Locked=true},new CPPointState(),new CPPointState{Owner=CPTeam.BLU,Locked=true}};Remaining=300;Overtime=Finished=false;Winner=CPTeam.Neutral;CenterCaptures=0;EndReason="";}
 public static CPTeam Enemy(CPTeam t)=>t==CPTeam.RED?CPTeam.BLU:t==CPTeam.BLU?CPTeam.RED:CPTeam.Neutral;
 public float CaptureSeconds(int index,int count)=>(index==1?CenterSeconds:BaseSeconds)*(float)Math.Pow(.75,Math.Max(0,count-1));
 public float RespawnDelay(CPTeam t)=>BaseRespawn+(!Points[t==CPTeam.RED?0:2].Locked?DefenderPenalty:0);
 public bool CanCapture(int i,CPTeam t)=>!Finished&&t!=CPTeam.Neutral&&!Points[i].Locked&&Points[i].Owner!=t;
 public void Tick(float dt,int[] red,int[] blue){
  if(Finished||dt<=0)return;
  for(int i=0;i<3;i++){Points[i].RedCount=red[i];Points[i].BlueCount=blue[i];}
  // Resolve capture events and timer expiry in chronological order, including ties.
  float left=dt;
  while(left>0.00001f&&!Finished){
   if(Remaining<=.00001f&&!Overtime){Remaining=0;if(HasAttacker())Overtime=true;else{ResolveTerritory();break;}}
   if(Overtime&&!HasAttacker()){ResolveTerritory();break;}
   float step=Overtime?left:Math.Min(left,Remaining);Array.Clear(rates,0,3);
   for(int i=0;i<3;i++){
    var p=Points[i];if(p.Locked){p.Progress=0;p.Capturing=CPTeam.Neutral;continue;}
    if(p.Contested)continue;
    var team=p.RedCount>0?CPTeam.RED:p.BlueCount>0?CPTeam.BLU:CPTeam.Neutral;
    if(team!=CPTeam.Neutral&&team!=p.Owner){if(p.Capturing!=team){p.Progress=0;p.Capturing=team;}int count=team==CPTeam.RED?p.RedCount:p.BlueCount;rates[i]=1/Math.Max(.02f,CaptureSeconds(i,count));step=Math.Min(step,(1-p.Progress)/rates[i]);}
    else if(p.Progress>0)rates[i]=-.25f;
   }
   if(step<.000001f)step=Math.Min(left,.000001f);
   if(!Overtime)Remaining=Math.Max(0,Remaining-step);left-=step;
   for(int i=0;i<3&&!Finished;i++){if(rates[i]>0){Points[i].Progress=Math.Min(1,Points[i].Progress+rates[i]*step);if(Points[i].Progress>=.999999f)Capture(i,Points[i].Capturing);}else if(rates[i]<0){Points[i].Progress=Math.Max(0,Points[i].Progress+rates[i]*step);if(Points[i].Progress==0)Points[i].Capturing=CPTeam.Neutral;}}
  }
  if(!Finished&&Remaining<=.00001f){Remaining=0;if(HasAttacker())Overtime=true;else ResolveTerritory();}
 }
 bool HasAttacker(){foreach(var p in Points)if(p.Attackers>0)return true;return false;}
 void Capture(int i,CPTeam team){var p=Points[i];p.Owner=team;p.Progress=0;p.Capturing=CPTeam.Neutral;
  if(Overtime){Finish(team,"Captura en overtime");return;}
  if(i!=1){Finish(team,"Base enemiga capturada");return;}
  CenterCaptures++;Remaining+=CenterBonus;Points[0].Locked=team!=CPTeam.BLU;Points[2].Locked=team!=CPTeam.RED;
  for(int j=0;j<3;j++)if(Points[j].Locked){Points[j].Progress=0;Points[j].Capturing=CPTeam.Neutral;}
 }
 void ResolveTerritory(){Finish(Points[1].Owner,"Tiempo agotado: control territorial");}
 void Finish(CPTeam team,string reason){Winner=team;Finished=true;Overtime=false;EndReason=reason;}
}
}
