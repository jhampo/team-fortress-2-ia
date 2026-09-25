using UnityEngine;
using UnityEngine.UI;
namespace FpsStage2 {
// Code-native white arcs reproduce the supplied references without a black image background.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CPReticle:MaskableGraphic {
 public ClassId Character {get;private set;}
 public float Radius {get;private set;}=15;
 float stroke=1.7f,pixel=1;float radiusVelocity;bool initialized;float drawnRadius=-1,drawnStroke,drawnPixel;ClassId drawnClass;
 public void Present(ClassId id,Camera camera,WeaponClock clock,float dt){float scale=canvas!=null?canvas.scaleFactor:1;float unit=camera.pixelHeight/900f/Mathf.Max(.01f,scale);float target=id==ClassId.Scout?13*unit:15*unit;
  if(id==ClassId.Heavy)target=camera.pixelHeight*.5f/Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)*clock.SpreadTangent(Time.timeAsDouble)/Mathf.Max(.01f,scale);
  if(!initialized||Character!=id){Radius=target;radiusVelocity=0;initialized=true;}else Radius=Mathf.SmoothDamp(Radius,target,ref radiusVelocity,.045f,Mathf.Infinity,dt);
  Character=id;stroke=1.7f*unit;pixel=.65f/Mathf.Max(.01f,scale);raycastTarget=false;color=Color.white;
  if(drawnClass!=id||Mathf.Abs(drawnRadius-Radius)>.025f||Mathf.Abs(drawnStroke-stroke)>.001f||Mathf.Abs(drawnPixel-pixel)>.001f){drawnClass=id;drawnRadius=Radius;drawnStroke=stroke;drawnPixel=pixel;SetVerticesDirty();}
 }
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();if(Character==ClassId.Scout){Arc(vh,145,215);Arc(vh,-35,35);Disc(vh,1.3f*stroke/1.7f);}else for(int i=0;i<4;i++)Arc(vh,i*90-35,i*90+35);}
 void Arc(VertexHelper vh,float start,float end){const int steps=18;for(int i=0;i<steps;i++){float a=Mathf.Lerp(start,end,i/(float)steps)*Mathf.Deg2Rad,b=Mathf.Lerp(start,end,(i+1f)/steps)*Mathf.Deg2Rad;float inner=Radius-stroke*.5f,outer=Radius+stroke*.5f;Band(vh,a,b,inner-pixel,inner,0,1);Band(vh,a,b,inner,outer,1,1);Band(vh,a,b,outer,outer+pixel,1,0);}}
 void Band(VertexHelper vh,float a,float b,float r0,float r1,float alpha0,float alpha1){int n=vh.currentVertCount;vh.AddVert(new Vector3(Mathf.Cos(a)*r0,Mathf.Sin(a)*r0),new Color(1,1,1,alpha0),Vector2.zero);vh.AddVert(new Vector3(Mathf.Cos(b)*r0,Mathf.Sin(b)*r0),new Color(1,1,1,alpha0),Vector2.zero);vh.AddVert(new Vector3(Mathf.Cos(b)*r1,Mathf.Sin(b)*r1),new Color(1,1,1,alpha1),Vector2.zero);vh.AddVert(new Vector3(Mathf.Cos(a)*r1,Mathf.Sin(a)*r1),new Color(1,1,1,alpha1),Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
 void Disc(VertexHelper vh,float radius){for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Band(vh,a,b,0,radius,1,1);Band(vh,a,b,radius,radius+pixel,1,0);}}
}
}
