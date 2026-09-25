using UnityEngine;
using UnityEngine.UI;
namespace FpsStage2 {
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CPDamageOverlay:MaskableGraphic {
 struct Signal {public Vector3 origin;public float amount,born,until;}
 readonly Signal[] signals=new Signal[8];int cursor;Camera view;
 public int ActiveSignals {get;private set;}
 public static float Bearing(Vector3 cameraForward,Vector3 direction){
  cameraForward=Vector3.ProjectOnPlane(cameraForward,Vector3.up).normalized;direction=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
  return Mathf.Atan2(Vector3.Dot(direction,Vector3.Cross(Vector3.up,cameraForward)),Vector3.Dot(direction,cameraForward));
 }
 public void Initialize(Camera camera){view=camera;raycastTarget=false;}
 public void Hit(Vector3 origin,float amount){
  float now=Time.time;int index=-1;
  for(int i=0;i<signals.Length;i++)if(signals[i].until>now&&now-signals[i].born<.12f&&Vector3.Angle(signals[i].origin-view.transform.position,origin-view.transform.position)<22){index=i;break;}
  if(index<0){index=cursor++%signals.Length;signals[index]=new Signal{origin=origin,born=now};}
  var hit=signals[index];hit.amount=Mathf.Min(150,hit.amount+amount);hit.until=now+Mathf.Lerp(.65f,1.15f,Mathf.Clamp01(hit.amount/90));signals[index]=hit;SetVerticesDirty();
 }
 public void Clear(){for(int i=0;i<signals.Length;i++)signals[i]=default;ActiveSignals=0;SetVerticesDirty();}
 void LateUpdate(){if(view==null)return;bool active=ActiveSignals>0;for(int i=0;i<signals.Length&&!active;i++)active=signals[i].until>Time.time;if(active)SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();ActiveSignals=0;if(view==null)return;var rect=rectTransform.rect;float unit=rect.height/900;var radii=new Vector2(rect.width*.43f,rect.height*.36f);
  foreach(var hit in signals){if(hit.until<=Time.time)continue;ActiveSignals++;float power=Mathf.Clamp01(hit.amount/90),age=Time.time-hit.born,fade=Mathf.Clamp01((hit.until-Time.time)/.28f)*Mathf.Clamp01(age/.025f);float flash=.85f+.15f*Mathf.Cos(age*33);
   float angle=Bearing(view.transform.forward,hit.origin-view.transform.position),span=Mathf.Lerp(24,43,power)*Mathf.Deg2Rad,thickness=Mathf.Lerp(40,82,power)*unit;
   for(int i=0;i<32;i++){float a=i/32f,b=(i+1)/32f;float ca=angle+Mathf.Lerp(-span,span,a),cb=angle+Mathf.Lerp(-span,span,b);float aa=Mathf.Pow(Mathf.Sin(a*Mathf.PI),.65f)*fade*flash,ab=Mathf.Pow(Mathf.Sin(b*Mathf.PI),.65f)*fade*flash;
    Band(vh,radii,ca,cb,-thickness*1.35f,-thickness*.20f,0,.90f,aa,ab);Band(vh,radii,ca,cb,-thickness*.20f,thickness*.19f,.90f,1,aa,ab);Band(vh,radii,ca,cb,thickness*.19f,thickness*.55f,1,0,aa,ab);
   }
  }
 }
 static Vector3 Point(Vector2 radius,float angle,float offset)=>new Vector3(Mathf.Sin(angle)*(radius.x+offset),Mathf.Cos(angle)*(radius.y+offset),0);
 static void Band(VertexHelper vh,Vector2 r,float a,float b,float inner,float outer,float alpha0,float alpha1,float fade0,float fade1){
  int n=vh.currentVertCount;vh.AddVert(Point(r,a,inner),new Color(1,.025f,.008f,alpha0*fade0),Vector2.zero);vh.AddVert(Point(r,b,inner),new Color(1,.025f,.008f,alpha0*fade1),Vector2.zero);vh.AddVert(Point(r,b,outer),new Color(1,.075f,.02f,alpha1*fade1),Vector2.zero);vh.AddVert(Point(r,a,outer),new Color(1,.075f,.02f,alpha1*fade0),Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
 }
}
}
