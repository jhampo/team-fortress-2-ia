using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace FpsStage2 {
public sealed class CPMenus:MonoBehaviour {
 CPMatch match;Canvas canvas;RectTransform root;Font font;GameObject front,classes,board,muteButton;Text mute,warmup,subtitle;RawImage hero;float nextScore;CPActor[] sorted;
 readonly Text[,] rows=new Text[2,8];readonly Text[,,] cells=new Text[2,8,5];readonly RawImage[,] icons=new RawImage[2,8];readonly Text[] classLabels=new Text[4];
 readonly Image[] difficultyButtons=new Image[4];
 readonly Image[] graphicsButtons=new Image[3];CPSettings settings;GameObject pause;Text graphicsHint,sensitivityValue,volumeValue,pauseMute;Slider sensitivitySlider,volumeSlider;
 readonly Color cream=new Color(.94f,.88f,.73f),ink=new Color(.105f,.12f,.125f),rust=new Color(.65f,.24f,.13f);
 public void Initialize(CPMatch m){match=m;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");sorted=new CPActor[m.actors.Length];settings=gameObject.AddComponent<CPSettings>();settings.Initialize(m);
  var go=new GameObject("Powerhouse_Menus",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.layer=5;go.transform.SetParent(transform,false);canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=220;var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;root=go.GetComponent<RectTransform>();
  BuildFrontEnd();BuildPauseSettings();
  classes=Panel(root,"ClassSelection",0,0,1440,700,new Color(.09f,.105f,.11f,.985f)).gameObject;TextAt(classes.transform,"ELIGE TU CLASE",0,288,1300,65,44,cream);subtitle=TextAt(classes.transform,"",0,229,1310,52,18,cream);
  ClassId[] order={ClassId.Scout,ClassId.Soldier,ClassId.Pyro,ClassId.Heavy};for(int i=0;i<4;i++){var id=order[i];float x=(i-1.5f)*320;var card=Panel(classes.transform,"Class_"+id,x,-10,290,400,new Color(.2f,.19f,.165f));Portrait(card.transform,m.portraits[(int)id],0,30,240,300);classLabels[i]=TextAt(card.transform,id.ToString().ToUpperInvariant(),0,-138,270,50,29,cream);ActionButton(classes.transform,"ELEGIR",x,-254,290,55,()=>m.flow.ChooseClass(id),rust,22);}ActionButton(classes.transform,"VOLVER",595,292,170,43,()=>m.flow.CloseClasses(),new Color(.26f,.28f,.27f),17);
  board=Panel(root,"Scoreboard",0,15,1510,706,new Color(.05f,.06f,.063f,.97f)).gameObject;TextAt(board.transform,"POWERHOUSE  /  MARCADOR",0,298,1350,60,35,cream);TextAt(board.transform,"PUNTOS = 2 × BAJAS + ASISTENCIAS + 3 × CAPTURAS",0,-311,1400,38,17,cream);
  float[] columns={34,96,158,220,287};string[] headings={"BAJAS","MUERT.","ASIST.","CAPT.","PTS"};
  for(int t=0;t<2;t++){float x=t==0?-372:372;var team=t==0?CPTeam.RED:CPTeam.BLU;Panel(board.transform,"Team",x,237,715,52,CPMatch.TeamColor(team));TextAt(board.transform,team.ToString(),x,237,690,47,28,cream);TextAt(board.transform,"JUGADOR",x-145,185,280,35,17,cream);for(int c=0;c<5;c++)TextAt(board.transform,headings[c],x+columns[c],185,62,35,13,cream);
   for(int n=0;n<8;n++){float y=139-n*53;var line=Panel(board.transform,"ScoreRow",x,y,715,49,new Color(.13f,.145f,.15f,n%2==0?.95f:.55f));icons[t,n]=Portrait(line.transform,null,-323,0,35,44);icons[t,n].uvRect=new Rect(.3f,.57f,.4f,.38f);rows[t,n]=TextAt(line.transform,"",-145,0,282,46,18,cream);rows[t,n].alignment=TextAnchor.MiddleLeft;for(int c=0;c<5;c++)cells[t,n,c]=TextAt(line.transform,"0",columns[c],0,62,46,19,cream);}
  }
  muteButton=ActionButton(root,"SONIDO: ON",655,398,234,45,()=>m.audioSystem.ToggleMute(),new Color(.26f,.28f,.27f),17).gameObject;mute=muteButton.GetComponentInChildren<Text>();warmup=TextAt(root,"",0,288,900,100,34,cream);warmup.gameObject.AddComponent<Outline>().effectColor=Color.black;front.SetActive(true);classes.SetActive(false);board.SetActive(false);
 }
 void BuildFrontEnd(){
  front=Panel(root,"FrontEnd",0,0,16400,16400,ink).gameObject;var content=Panel(front.transform,"Composition",0,0,1450,820,ink);
  Panel(content.transform,"RedBacking",-395,-10,420,650,new Color(.27f,.16f,.12f));Panel(content.transform,"Accent",-625,-10,10,650,rust);
  TextAt(content.transform,"RED  /  BLU",-400,340,440,50,24,cream);hero=Portrait(content.transform,match.portraits[0],-380,-45,440,650);
  TextAt(content.transform,"POWERHOUSE",270,260,720,100,72,cream);TextAt(content.transform,"CENTRAL HIDROELÉCTRICA",270,184,720,38,22,new Color(.69f,.68f,.59f));Panel(content.transform,"Rule",270,140,630,3,rust);
  TextAt(content.transform,"DOS EQUIPOS. TRES PUNTOS.\nUN SOLO GANADOR.",260,85,630,75,24,cream);
  TextAt(content.transform,"DIFICULTAD",260,20,630,27,17,cream);string[] levels={"FÁCIL","NORMAL","DIFÍCIL","EXPERTO"};
  for(int d=0;d<4;d++){int selected=d;difficultyButtons[d]=ActionButton(content.transform,levels[d],14+d*164,-25,153,45,()=>match.SelectDifficulty((CPDifficulty)selected),ink,18);}
  TextAt(content.transform,"CALIDAD GRÁFICA",260,-84,630,27,17,cream);string[] graphics={"BAJA","MEDIA","ULTRA"};
  for(int i=0;i<3;i++){int selected=i;graphicsButtons[i]=ActionButton(content.transform,graphics[i],60+i*200,-128,190,45,()=>settings.SetGraphics(selected),ink,19);}
  graphicsHint=TextAt(content.transform,"",260,-176,650,35,16,new Color(.69f,.68f,.59f));
  ActionButton(content.transform,"JUGAR",260,-249,460,75,()=>{settings.Flush();match.flow.Begin();},rust,32);
  TextAt(content.transform,"WASD  mover     ESPACIO  saltar\n,  elegir clase     TAB  marcador     ESC  ajustes",260,-332,680,58,18,new Color(.66f,.65f,.59f));
  TextAt(content.transform,"POWERHOUSE  /  CONTROL POINTS",0,-395,1400,25,14,new Color(.46f,.47f,.44f));
 }
 void BuildPauseSettings(){
  pause=Panel(root,"PauseSettings",0,0,800,610,new Color(.07f,.08f,.085f,.985f)).gameObject;
  TextAt(pause.transform,"AJUSTES",0,248,720,65,43,cream);TextAt(pause.transform,"Se guardan automáticamente · ESC para continuar",0,197,730,36,18,cream);
  TextAt(pause.transform,"SENSIBILIDAD DEL RATÓN",-100,144,540,34,21,cream).alignment=TextAnchor.MiddleLeft;
  sensitivityValue=TextAt(pause.transform,"",300,91,130,40,24,cream);
  sensitivitySlider=SettingSlider(pause.transform,"MouseSensitivity",-40,91,560,.25f,3,settings.Sensitivity,v=>settings.SetSensitivity(v));
  TextAt(pause.transform,"VOLUMEN GENERAL",-100,24,540,34,21,cream).alignment=TextAnchor.MiddleLeft;
  volumeValue=TextAt(pause.transform,"",300,-28,130,40,24,cream);
  volumeSlider=SettingSlider(pause.transform,"MasterVolume",-40,-28,560,0,1,settings.Volume,v=>settings.SetVolume(v));
  ActionButton(pause.transform,"RESTABLECER CONTROLES Y AUDIO",-116,-116,470,44,()=>{settings.ResetControlsAndAudio();sensitivitySlider.SetValueWithoutNotify(settings.Sensitivity);volumeSlider.SetValueWithoutNotify(settings.Volume);},new Color(.23f,.25f,.24f),17);
  pauseMute=ActionButton(pause.transform,"SILENCIAR",262,-116,170,44,()=>match.audioSystem.ToggleMute(),new Color(.23f,.25f,.24f),17).GetComponentInChildren<Text>();
  ActionButton(pause.transform,"CONTINUAR",-190,-218,330,57,()=>{settings.Flush();match.Resume();},rust,23);
  ActionButton(pause.transform,"IR AL MENÚ",190,-218,330,57,()=>{settings.Flush();match.ReturnToMainMenu();},new Color(.23f,.25f,.24f),23);
  TextAt(pause.transform,"Volver al menú termina la partida actual",0,-272,720,27,16,new Color(.69f,.68f,.59f));pause.SetActive(false);
 }
 Slider SettingSlider(Transform parent,string name,float x,float y,float width,float min,float max,float value,UnityEngine.Events.UnityAction<float> changed){
  var area=Panel(parent,name,x,y,width,36,new Color(0,0,0,0));area.raycastTarget=true;
  Panel(area.transform,"Track",0,0,width,7,new Color(.25f,.27f,.26f));
  var fillArea=new GameObject("FillArea",typeof(RectTransform)).GetComponent<RectTransform>();fillArea.SetParent(area.transform,false);fillArea.anchorMin=new Vector2(0,.5f);fillArea.anchorMax=new Vector2(1,.5f);fillArea.offsetMin=new Vector2(0,-3.5f);fillArea.offsetMax=new Vector2(0,3.5f);
  var fill=Panel(fillArea,"Fill",0,0,width,7,rust);fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
  var handle=Panel(area.transform,"Handle",0,0,20,30,cream);handle.raycastTarget=true;
  var slider=area.gameObject.AddComponent<Slider>();slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=min;slider.maxValue=max;slider.SetValueWithoutNotify(value);slider.onValueChanged.AddListener(changed);return slider;
 }
 Image Panel(Transform parent,string name,float x,float y,float w,float h,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.layer=5;go.transform.SetParent(parent,false);var p=go.GetComponent<Image>();p.color=color;p.raycastTarget=false;Place(p.rectTransform,x,y,w,h);return p;}
 static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
 Text TextAt(Transform parent,string text,float x,float y,float w,float h,int size,Color c){var go=new GameObject("Label",typeof(RectTransform),typeof(Text));go.layer=5;go.transform.SetParent(parent,false);var t=go.GetComponent<Text>();t.text=text;t.font=font;t.fontSize=size;t.color=c;t.raycastTarget=false;t.alignment=TextAnchor.MiddleCenter;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;Place(t.rectTransform,x,y,w,h);return t;}
 RawImage Portrait(Transform parent,Texture texture,float x,float y,float w,float h){var go=new GameObject("Portrait",typeof(RectTransform),typeof(RawImage));go.layer=5;go.transform.SetParent(parent,false);var p=go.GetComponent<RawImage>();p.texture=texture;p.raycastTarget=false;Place(p.rectTransform,x,y,w,h);if(texture!=null){float aspect=(float)texture.width/texture.height;p.rectTransform.sizeDelta=new Vector2(Mathf.Min(w,h*aspect),Mathf.Min(h,w/aspect));}return p;}
 Image ActionButton(Transform parent,string text,float x,float y,float w,float h,UnityEngine.Events.UnityAction action,Color color,int size){var p=Panel(parent,text,x,y,w,h,color);p.raycastTarget=true;var b=p.gameObject.AddComponent<Button>();b.targetGraphic=p;var colors=b.colors;colors.highlightedColor=new Color(1.3f,1.25f,1.15f);b.colors=colors;b.onClick.AddListener(action);TextAt(p.transform,text,0,0,w-20,h-4,size,cream);return p;}
 void LateUpdate(){if(match==null)return;bool main=match.flow.Phase==CPRoundPhase.MainMenu;front.SetActive(main);if(main)for(int d=0;d<4;d++)difficultyButtons[d].color=d==(int)match.difficulty?rust:new Color(.20f,.22f,.22f);classes.SetActive(match.flow.ClassMenuOpen);bool show=!main&&!match.flow.ClassMenuOpen&&!match.MenuOpen&&Keyboard.current!=null&&Keyboard.current.tabKey.isPressed;board.SetActive(show);match.GetComponent<CPHud>().RootCanvas.gameObject.SetActive(!main);muteButton.SetActive(main||match.flow.ClassMenuOpen);mute.text=match.audioSystem.Muted?"SONIDO: OFF":"SONIDO: ON";
  if(main){for(int i=0;i<3;i++)if(graphicsButtons[i]!=null)graphicsButtons[i].color=i==(int)settings.Graphics?rust:new Color(.20f,.22f,.22f);graphicsHint.text=settings.Graphics==CPGraphicsQuality.Low?"Menor detalle para priorizar fluidez":settings.Graphics==CPGraphicsQuality.Ultra?"Máxima nitidez · requiere más potencia gráfica":"Gráficos medios · opción predeterminada";}
  bool paused=match.MenuOpen&&!main;pause.SetActive(paused);if(paused){sensitivityValue.text=settings.Sensitivity.ToString("0.00")+"×";volumeValue.text=Mathf.RoundToInt(settings.Volume*100)+" %";pauseMute.text=match.audioSystem.Muted?"ACTIVAR":"SILENCIAR";}
  warmup.text=match.flow.Phase==CPRoundPhase.Preparation&&!match.flow.ClassMenuOpen&&!match.MenuOpen?"PREPARACIÓN  "+Mathf.CeilToInt(match.flow.PreparationRemaining)+"\n<size=18>Las puertas se abrirán al comenzar la ronda</size>":"";
  if(match.flow.ClassMenuOpen)subtitle.text=match.flow.InOwnBase(match.PlayerActor)?"ESTÁS EN TU BASE · puedes cambiar sin morir":"FUERA DE TU BASE · cambiar de clase provoca muerte y reaparición";
  if(show&&Time.unscaledTime>=nextScore){nextScore=Time.unscaledTime+.15f;RefreshScore();}
 }
 public static Color ScoreRowColor(bool player,int row)=>player?new Color(.38f,.28f,.10f,.98f):new Color(.13f,.145f,.15f,row%2==0?.95f:.55f);
public void RefreshScore(){Array.Copy(match.actors,sorted,sorted.Length);Array.Sort(sorted,(a,b)=>{int c=b.score.Points.CompareTo(a.score.Points);return c!=0?c:string.CompareOrdinal(a.score.Name,b.score.Name);});for(int t=0;t<2;t++){int row=0;foreach(var a in sorted){if(a.team!=(t==0?CPTeam.RED:CPTeam.BLU))continue;var s=a.score;var text=rows[t,row];text.text=s.Name;text.transform.parent.GetComponent<Image>().color=ScoreRowColor(a.isPlayer,row);text.fontStyle=a.isPlayer?FontStyle.Bold:FontStyle.Normal;text.resizeTextForBestFit=true;text.resizeTextMinSize=13;text.resizeTextMaxSize=18;text.color=a.Alive?(a.isPlayer?new Color(1,.8f,.35f):cream):new Color(.47f,.48f,.46f);cells[t,row,0].text=s.Kills.ToString();cells[t,row,1].text=s.Deaths.ToString();cells[t,row,2].text=s.Assists.ToString();cells[t,row,3].text=s.Captures.ToString();cells[t,row,4].text=s.Points.ToString();for(int c=0;c<5;c++)cells[t,row,c].color=text.color;icons[t,row].texture=match.portraits[(t==0?0:4)+(int)a.characterClass];icons[t,row].color=a.Alive?Color.white:new Color(.35f,.35f,.35f);row++;if(row==8)break;}}}
}
}
