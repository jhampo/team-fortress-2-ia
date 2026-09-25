using UnityEngine;
using UnityEngine.UI;
namespace FpsStage2 {
// Pooled, local-player-only feedback; world damage remains authoritative in CPActor.
[DefaultExecutionOrder(250)] public sealed class CPCombatFeedback:MonoBehaviour {
 sealed class Number {public CPActor target;public RectTransform root;public Text text;public float amount,born,until,height;public int frame=-1,hitFrame=-1,lane;public Vector2 offset;public bool burn,anchored;}
 CPMatch match;Canvas canvas;RectTransform frame;Camera cameraView;Font font;CPDamageOverlay direction;Image burnImage;Material burnMaterial;float burnAmount,pulse;bool wasAlive;
 readonly Number[] numbers=new Number[32];int cursor;
 public CPDamageOverlay Direction=>direction;public float BurnOpacity=>burnAmount;public int VisibleDamageNumbers {get;private set;}
 public void Initialize(CPMatch owner,Canvas hud){
  match=owner;canvas=hud;cameraView=match.player.playerCamera;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");frame=(RectTransform)canvas.transform;wasAlive=true;
  var burnGo=new GameObject("Player_Burning_Edges",typeof(RectTransform),typeof(Image));burnGo.layer=5;burnGo.transform.SetParent(frame,false);burnGo.transform.SetAsFirstSibling();burnImage=burnGo.GetComponent<Image>();Stretch(burnImage.rectTransform);burnImage.raycastTarget=false;
  var shader=Resources.Load<Shader>("CP_ScreenBurn");burnMaterial=new Material(shader);burnImage.material=burnMaterial;burnImage.enabled=false;
  var damageGo=new GameObject("Incoming_Damage_Direction",typeof(RectTransform),typeof(CPDamageOverlay));damageGo.layer=5;damageGo.transform.SetParent(frame,false);damageGo.transform.SetSiblingIndex(1);direction=damageGo.GetComponent<CPDamageOverlay>();Stretch(direction.rectTransform);direction.Initialize(cameraView);
  for(int i=0;i<numbers.Length;i++){var n=new Number();n.root=Rect("Damage_Number_"+i,frame,110,48);n.text=Text("",n.root,110,48,28);n.root.gameObject.SetActive(false);numbers[i]=n;}
  Canvas.willRenderCanvases+=PresentNumbers;
  foreach(var actor in match.actors)if(!actor.isPlayer){var visual=actor.GetComponent<CPBurnVisual>();if(visual==null)visual=actor.gameObject.AddComponent<CPBurnVisual>();visual.Initialize(actor,match.player.Effects);}
 }
 static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
 RectTransform Rect(string name,Transform parent,float w,float h){var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(w,h);return r;}
 Text Text(string value,Transform parent,float w,float h,int size){var r=Rect("Text",parent,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=new Color(0,0,0,.86f);outline.effectDistance=new Vector2(1.2f,-1.2f);return t;}
 public void ReportDamage(CPActor victim,CPActor attacker,float damage,Vector3 origin,bool afterburn){
  if(victim.isPlayer){if(afterburn)pulse=1;else direction.Hit(origin,damage);return;}
  if(attacker==null||!attacker.isPlayer||victim.team==attacker.team)return;
  // Combine simultaneous pellets only; subsequent impacts retain separate labels.
  Number number=null;int occupied=0;
  foreach(var n in numbers)if(n.target==victim&&n.until>Time.time){
   occupied|=1<<n.lane;
   if(n.hitFrame==Time.frameCount&&n.burn==afterburn)number=n;
  }
  if(number==null){
   for(int i=0;i<numbers.Length;i++){int index=(cursor+i)%numbers.Length;if(numbers[index].until<=Time.time){number=numbers[index];cursor=(index+1)%numbers.Length;break;}}
   if(number==null){number=numbers[0];foreach(var n in numbers)if(n.born<number.born)number=n;}
   int lane=0;while(lane<30&&(occupied&(1<<lane))!=0)lane++;
   number.target=victim;number.amount=0;number.born=Time.time;number.until=Time.time+1.5f;
   number.burn=afterburn;number.hitFrame=Time.frameCount;number.anchored=false;number.frame=-1;number.lane=lane;
   // Tight, fixed cluster over the head; sustained fire must not build a tall grid.
   int column=lane%3;number.offset=new Vector2(column==0?0:column==1?-8:8,((lane/3)%3)*6);
  }
  number.amount+=damage;
  number.text.text="-"+Mathf.Max(1,Mathf.RoundToInt(number.amount));number.text.color=new Color(1,.12f,.08f);number.text.fontSize=28;number.text.fontStyle=FontStyle.Bold;
 }
 public void Forget(CPActor actor){if(actor.isPlayer){direction?.Clear();burnAmount=pulse=0;}foreach(var n in numbers)if(n!=null&&(actor.isPlayer||n.target==actor)){n.until=0;n.target=null;n.root.gameObject.SetActive(false);}}
 bool ScreenPosition(Vector3 world,out Vector2 local){
  var point=cameraView.WorldToViewportPoint(world);local=Vector2.zero;if(point.z<=.05f||point.x<.04f||point.x>.96f||point.y<.10f||point.y>.88f)return false;
  if(Physics.Linecast(cameraView.transform.position,world,1,QueryTriggerInteraction.Ignore))return false;
  return RectTransformUtility.ScreenPointToLocalPointInRectangle(frame,cameraView.WorldToScreenPoint(world),canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:cameraView,out local);
 }
 void LateUpdate(){
  if(match==null||match.PlayerActor==null)return;var player=match.PlayerActor;bool show=player.Alive&&!match.MenuOpen&&!match.rules.Finished;
  if(!player.Alive&&wasAlive)Forget(player);wasAlive=player.Alive;direction.enabled=show;
  burnAmount=player.Alive?Mathf.MoveTowards(burnAmount,player.IsBurning?1:0,Time.deltaTime*(player.IsBurning?6:9)):0;pulse=Mathf.MoveTowards(pulse,0,Time.deltaTime*5);
  burnImage.enabled=show&&burnAmount>.001f;burnMaterial.SetFloat("_Opacity",burnAmount*.5f);burnMaterial.SetFloat("_BurnTime",Time.time);burnMaterial.SetFloat("_Pulse",pulse);burnMaterial.SetFloat("_Aspect",cameraView.aspect);
 }
 void PresentNumbers(){
  if(match==null||match.PlayerActor==null||cameraView==null)return;
  bool show=match.PlayerActor.Alive&&!match.MenuOpen&&!match.rules.Finished;VisibleDamageNumbers=0;
  foreach(var n in numbers){
   if(n==null)continue;Vector2 pos=Vector2.zero;bool visible=show&&n.target!=null&&Time.time<n.until;
   if(visible){
    float height=n.target.LabelPosition.y-n.target.transform.position.y;
    if(!n.anchored){n.height=height;n.anchored=true;}
    if(n.frame!=Time.frameCount){n.height=Mathf.Lerp(n.height,height,1-Mathf.Exp(-Time.deltaTime/ .09f));n.frame=Time.frameCount;}
    // Follow translation and jumps without lag. Smooth only the relative bone bob.
    visible=ScreenPosition(n.target.transform.position+Vector3.up*n.height,out pos);
   }
   n.root.gameObject.SetActive(visible);if(!visible)continue;VisibleDamageNumbers++;
   n.root.anchoredPosition=pos+n.offset;Color c=n.text.color;c.a=Mathf.Clamp01((n.until-Time.time)/.18f);n.text.color=c;n.root.localScale=Vector3.one;
  }
 }
 void OnDestroy(){Canvas.willRenderCanvases-=PresentNumbers;if(burnMaterial!=null)Destroy(burnMaterial);}
}
}
