using UnityEngine;
namespace FpsStage2 {
public sealed class CPPointAtmosphere:MonoBehaviour {
 ParticleSystem rain;Material particleMaterial,ringMaterial;Texture2D texture;Mesh ringMesh;Transform ring;Light halo;CPTeam owner=CPTeam.Neutral;float changed;float radius;
 public ParticleSystem Rain=>rain;
 public void Initialize(CPZone zone){
  transform.position=zone.position+Vector3.up*.025f;radius=zone.radius*.70f;
  ringMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));ringMaterial.name="CP_TeamHalo";
  var disc=new GameObject("Luminous_Capture_Rim");disc.transform.SetParent(transform,false);ring=disc.transform;ringMesh=new Mesh();var v=new Vector3[128];var triangles=new int[384];
  for(int i=0;i<64;i++){float a=i*Mathf.PI/32;v[i*2]=new Vector3(Mathf.Cos(a)*(radius-.075f),.014f,Mathf.Sin(a)*(radius-.075f));v[i*2+1]=new Vector3(Mathf.Cos(a)*(radius+.075f),.014f,Mathf.Sin(a)*(radius+.075f));int j=(i+1)%64;int t=i*6;triangles[t]=i*2;triangles[t+1]=j*2;triangles[t+2]=i*2+1;triangles[t+3]=i*2+1;triangles[t+4]=j*2;triangles[t+5]=j*2+1;}
  ringMesh.vertices=v;ringMesh.triangles=triangles;ringMesh.RecalculateNormals();ringMesh.RecalculateBounds();disc.AddComponent<MeshFilter>().sharedMesh=ringMesh;disc.AddComponent<MeshRenderer>().sharedMaterial=ringMaterial;
  texture=new Texture2D(32,32,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;var pixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){float u=(x-15.5f)/15.5f,z=(y-15.5f)/15.5f;float a=Mathf.Pow(Mathf.Clamp01(1-u*u-z*z),2);pixels[y*32+x]=new Color(1,1,1,a);}texture.SetPixels(pixels);texture.Apply();
  particleMaterial=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));particleMaterial.SetFloat("_Surface",1);particleMaterial.SetFloat("_Blend",0);particleMaterial.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);particleMaterial.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);particleMaterial.SetFloat("_ZWrite",0);particleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");particleMaterial.renderQueue=3000;particleMaterial.SetTexture("_BaseMap",texture);particleMaterial.SetColor("_BaseColor",Color.white);
  var stream=new GameObject("Team_Particles_Downward");stream.transform.SetParent(transform,false);stream.transform.localPosition=Vector3.up*3.1f;rain=stream.AddComponent<ParticleSystem>();rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=rain.main;main.playOnAwake=false;main.loop=true;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.startLifetime=2.05f;main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.045f,.11f);main.maxParticles=160;
  var emission=rain.emission;emission.rateOverTime=44;var shape=rain.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.rotation=new Vector3(90,0,0);shape.radius=radius;shape.radiusThickness=.3f;
  var velocity=rain.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=new ParticleSystem.MinMaxCurve(0,0);velocity.y=new ParticleSystem.MinMaxCurve(-1.50f,-1.38f);velocity.z=new ParticleSystem.MinMaxCurve(0,0);
  var size=rain.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.15f),new Keyframe(.15f,1),new Keyframe(.85f,1),new Keyframe(1,0)));
  var colors=rain.colorOverLifetime;colors.enabled=true;var fade=new Gradient();fade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.85f,.15f),new GradientAlphaKey(.72f,.8f),new GradientAlphaKey(0,1)});colors.color=fade;
  var renderer=stream.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=particleMaterial;renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.velocityScale=.08f;renderer.lengthScale=2.3f;renderer.sortMode=ParticleSystemSortMode.Distance;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
  var lamp=new GameObject("Team_Halo_Light");lamp.transform.SetParent(transform,false);lamp.transform.localPosition=Vector3.up*.65f;halo=lamp.AddComponent<Light>();halo.type=LightType.Point;halo.range=6;halo.shadows=LightShadows.None;
 }
 public void Present(CPPointState state){
  if(rain==null)return;
  if(owner!=state.Owner){owner=state.Owner;changed=Time.time;rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=rain.main;main.startColor=Bright(owner);if(owner!=CPTeam.Neutral)rain.Play();}
  Color c=state.Capturing!=CPTeam.Neutral?Color.Lerp(Bright(owner),Bright(state.Capturing),state.Progress):Bright(owner);
  float pulse=1+.10f*Mathf.Sin(Time.time*3)+.45f*Mathf.Clamp01(1-(Time.time-changed)/1.2f);ringMaterial.SetColor("_BaseColor",c*pulse);ring.localScale=Vector3.one*(1+.01f*Mathf.Sin(Time.time*2.7f));halo.color=c;halo.intensity=owner==CPTeam.Neutral?.45f:2.1f*pulse;
 }
 static Color Bright(CPTeam team)=>team==CPTeam.RED?new Color(2.1f,.12f,.055f):team==CPTeam.BLU?new Color(.12f,1.15f,2.25f):new Color(.95f,.78f,.40f);
 void OnDestroy(){if(particleMaterial!=null)Destroy(particleMaterial);if(ringMaterial!=null)Destroy(ringMaterial);if(texture!=null)Destroy(texture);if(ringMesh!=null)Destroy(ringMesh);}
}
}
