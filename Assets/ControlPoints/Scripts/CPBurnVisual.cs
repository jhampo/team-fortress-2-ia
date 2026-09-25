using System.Linq;
using UnityEngine;
namespace FpsStage2 {
public sealed class CPBurnVisual:MonoBehaviour {
 CPActor actor;ParticleSystem flames,embers;Transform[] anchors;float accumulator,sparks;bool wasBurning;int anchorIndex;Material flameMaterial;
 public bool Visible=>flames!=null&&flames.particleCount>0;
 public int ParticleCount=>flames!=null?flames.particleCount+embers.particleCount:0;
 public void Initialize(CPActor owner,Stage2Effects effects){
  actor=owner;string[] names={"Hips","Spine02","Spine","Head","LeftArm","RightArm","LeftForeArm","RightForeArm","LeftHand","RightHand","LeftUpLeg","RightUpLeg","LeftLeg","RightLeg","LeftFoot","RightFoot"};
  anchors=GetComponentsInChildren<Transform>(true).Where(t=>names.Contains(t.name)).ToArray();
  flames=effects.CreateFireSystem("Burn_Body_"+name,false);embers=effects.CreateFireSystem("Burn_Embers_"+name,true);Configure(flames,false);Configure(embers,true);flameMaterial=new Material(Resources.Load<Shader>("CP_BodyFlame"));flameMaterial.SetTexture("_MainTex",flames.GetComponent<ParticleSystemRenderer>().sharedMaterial.GetTexture("_MainTex"));flames.GetComponent<ParticleSystemRenderer>().sharedMaterial=flameMaterial;embers.GetComponent<ParticleSystemRenderer>().sharedMaterial=flameMaterial;
 }
 void Configure(ParticleSystem ps,bool spark){
  ps.transform.SetParent(transform,false);ps.transform.localPosition=Vector3.zero;ps.transform.localRotation=Quaternion.identity;
  var m=ps.main;m.simulationSpace=ParticleSystemSimulationSpace.Local;m.maxParticles=spark?48:180;m.startSpeed=0;m.startLifetime=spark?.4f:.38f;m.gravityModifier=0;m.startRotation=new ParticleSystem.MinMaxCurve(-.18f,.18f);
  var em=ps.emission;em.enabled=false;var shape=ps.shape;shape.enabled=false;
  var size=ps.sizeOverLifetime;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.65f),new Keyframe(.16f,1),new Keyframe(.72f,.85f),new Keyframe(1,.05f)));
  var n=ps.noise;n.strength=spark?.06f:.12f;n.frequency=2.8f;n.scrollSpeed=2;
  var color=ps.colorOverLifetime;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(new Color(2.2f,1.1f,.12f),0),new GradientColorKey(new Color(1.9f,.62f,.035f),.35f),new GradientColorKey(new Color(1.4f,.3f,.014f),.75f),new GradientColorKey(new Color(.75f,.08f,.002f),1)},new[]{new GradientAlphaKey(.55f,0),new GradientAlphaKey(.78f,.12f),new GradientAlphaKey(.65f,.55f),new GradientAlphaKey(0,1)});color.color=g;
  var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.maxParticleSize=.25f;if(!spark){renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=1.7f;renderer.velocityScale=.02f;}
 }
 void LateUpdate(){
  if(actor==null)return;bool burning=actor.IsBurning&&actor.Alive;
  if(!burning){if(wasBurning){flames.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);embers.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}wasBurning=false;accumulator=sparks=0;return;}
  if(!wasBurning){flames.Play();embers.Play();}wasBurning=true;
  accumulator+=Time.deltaTime*(CPWebPerformance.Active?90:145);sparks+=Time.deltaTime*(CPWebPerformance.Active?8:13);int count=Mathf.Min(64,Mathf.FloorToInt(accumulator));accumulator=Mathf.Min(1,accumulator-count);
  for(int i=0;i<count;i++)Emit(flames,false);int specks=Mathf.Min(6,Mathf.FloorToInt(sparks));sparks-=specks;for(int i=0;i<specks;i++)Emit(embers,true);
 }
 void OnDestroy(){if(flameMaterial!=null)Destroy(flameMaterial);}
 void Emit(ParticleSystem ps,bool spark){
  Transform bone=anchors.Length>0?anchors[anchorIndex++%anchors.Length]:transform;Vector3 p=bone.position;
  if(bone.parent!=null&&bone.name!="Hips"&&bone.name!="Head")p=Vector3.Lerp(p,bone.parent.position,Random.value*.8f);
  bool torso=bone.name=="Hips"||bone.name.StartsWith("Spine");float radius=torso?.24f:bone.name=="Head"?.15f:bone.name.EndsWith("Foot")?.08f:.12f;radius*=actor.characterClass==ClassId.Heavy?1.15f:actor.characterClass==ClassId.Scout?.78f:1;
  Vector3 axis=bone.parent!=null&&!torso?(bone.position-bone.parent.position).normalized:Vector3.up;Vector3 surface=Vector3.ProjectOnPlane(Random.onUnitSphere,axis).normalized*radius;
  var param=new ParticleSystem.EmitParams{position=transform.InverseTransformPoint(p+surface)+Random.insideUnitSphere*.025f,velocity=Vector3.up*Random.Range(spark?1.1f:.55f,spark?2:1.1f)+Random.insideUnitSphere*.12f,startSize=Random.Range(spark?.018f:.25f,spark?.028f:.36f),startLifetime=Random.Range(spark?.25f:.43f,spark?.5f:.65f),rotation=Random.Range(-12f,12f)};
  ps.Emit(param,1);
 }
}
}
