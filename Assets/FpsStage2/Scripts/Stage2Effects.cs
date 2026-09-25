using UnityEngine;
namespace FpsStage2 {
// Reusable effect pools and world-space fire particles; no dynamic lights or firing-time Instantiate.
[DefaultExecutionOrder(200)]
public sealed class Stage2Effects:MonoBehaviour {
 public bool FlameActive;
 public Transform FlameOrigin;
 public int ActiveSprites {get;private set;}
 const int Count=48;
 struct Puff {public Transform tr,anchor;public MeshRenderer renderer;public Vector3 velocity;public float age,life,size,roll,flashSeed;public Color color;public bool flash,heavyFlash;}
 struct RocketSlot {public CPActor shooter;public Transform tr;public Vector3 velocity,shotOrigin;public float life;}
 struct Tracer {public LineRenderer line;public Vector3 origin,direction;public float age,distance;public bool active;}
 readonly Puff[] pool=new Puff[Count];
 readonly RocketSlot[] rockets=new RocketSlot[12];
 readonly Tracer[] tracers=new Tracer[32];
 public int TracersEmitted {get;private set;}
 public int ActiveTracers {get;private set;}
 MaterialPropertyBlock block;Material botFireMat;
 Camera cam;FpsStage2Player player;int index,tracerIndex;Material mat,tracerMat,fireMat;Mesh mesh;Texture2D texture,fireTexture;Transform poolRoot;
 ParticleSystem fireParticles,fireWisps,embers,rocketSmoke;
 CPRocketBurst rocketBurst;
 Mesh flashMesh; Material flashMat,smokeMat,rocketMat,rocketGlowMat;
 readonly Transform[] rocketGlows=new Transform[12];
 void LateUpdate(){if(cam==null)return;
  // Follow the animated muzzle after viewmodel wall protection has finished.
  bool emitFire=FlameActive&&FlameOrigin!=null&&FlameOrigin.gameObject.activeInHierarchy;
  UpdateFire(fireParticles,emitFire);UpdateFire(fireWisps,emitFire);UpdateFire(embers,emitFire);
  for(int i=0;i<pool.Length;i++){var p=pool[i];if(!p.flash||p.life<=0||p.anchor==null)continue;if(!p.anchor.gameObject.activeInHierarchy){p.life=0;p.tr.gameObject.SetActive(false);pool[i]=p;continue;}p.tr.position=p.anchor.position;p.tr.rotation=cam.transform.rotation*Quaternion.Euler(0,0,p.roll);}for(int i=0;i<rockets.Length;i++){var glow=rocketGlows[i];if(glow==null)continue;bool visible=rockets[i].life>0;glow.gameObject.SetActive(visible);if(visible){glow.position=rockets[i].tr.position;glow.rotation=cam.transform.rotation;}}}
 public int FireParticleCount=>fireParticles!=null?fireParticles.particleCount+fireWisps.particleCount:0;
 public void Initialize(Camera camera,Material template,Material flameTemplate,FpsStage2Player owner) {
  cam=camera;player=owner;block=new MaterialPropertyBlock();mat=new Material(template);poolRoot=new GameObject("Stage2_WorldEffectsPool").transform;
  texture=new Texture2D(32,32,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;
  var colors=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){float u=(x-15.5f)/15.5f,v=(y-15.5f)/15.5f;float a=Mathf.Pow(Mathf.Clamp01(1-Mathf.Sqrt(u*u+v*v)),1.6f);colors[y*32+x]=new Color(1,1,1,a);}texture.SetPixels(colors);texture.Apply();mat.SetTexture("_MainTex",texture);
  mesh=new Mesh();mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateBounds();
  for(int i=0;i<Count;i++){var go=new GameObject("FX_Pooled_"+i);go.transform.SetParent(poolRoot,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;go.SetActive(false);pool[i]=new Puff{tr=go.transform,renderer=r};}
  rocketMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));rocketMat.SetColor("_BaseColor",new Color(3.2f,2.1f,.035f));
  for(int i=0;i<rockets.Length;i++){var go=GameObject.CreatePrimitive(PrimitiveType.Capsule);go.name="Rocket_Pooled_"+i;Destroy(go.GetComponent<Collider>());go.transform.SetParent(poolRoot,false);go.transform.localScale=new Vector3(.14f,.18f,.14f);go.GetComponent<MeshRenderer>().sharedMaterial=rocketMat;go.SetActive(false);rockets[i]=new RocketSlot{tr=go.transform};}
  tracerMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));tracerMat.SetColor("_BaseColor",new Color(3,1.8f,.45f));
  for(int i=0;i<tracers.Length;i++){var go=new GameObject("Weapon_ThinBullet_"+i);go.transform.SetParent(poolRoot,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=tracerMat;line.positionCount=2;line.useWorldSpace=true;line.startWidth=.0035f;line.endWidth=.0015f;line.numCapVertices=2;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;line.enabled=false;tracers[i]=new Tracer{line=line};}
  fireMat=new Material(flameTemplate);fireTexture=new Texture2D(96,96,TextureFormat.RGBA32,false);fireTexture.wrapMode=TextureWrapMode.Clamp;
  var firePixels=new Color[96*96];for(int y=0;y<96;y++)for(int x=0;x<96;x++){float u=(x+.5f)/96,v=(y+.5f)/96;float curl=(Mathf.PerlinNoise(v*3.1f,2.7f)-.5f)*.32f;float dx=(u-.5f+curl)*2,dy=(v-.45f)*1.8f;float envelope=Mathf.Clamp01(1-dx*dx-dy*dy);float n=.58f*Mathf.PerlinNoise(u*5.3f,v*5.3f)+.28f*Mathf.PerlinNoise(u*11.7f+6,v*11.7f)+.14f*Mathf.PerlinNoise(u*22,v*22+9);float a=Mathf.SmoothStep(0,1,Mathf.Clamp01((envelope*.88f+n*.58f-.53f)*2.6f));a*=Mathf.SmoothStep(0,1,Mathf.Clamp01(v/.15f))*Mathf.SmoothStep(0,1,Mathf.Clamp01((1-v)/.15f))*Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Min(u,1-u)/.12f));firePixels[y*96+x]=new Color(1,1,1,a*.63f);}fireTexture.SetPixels(firePixels);fireTexture.Apply();fireMat.SetTexture("_MainTex",fireTexture);
  CreateShotEffects();
  rocketBurst=poolRoot.gameObject.AddComponent<CPRocketBurst>();rocketBurst.Initialize(cam,fireTexture,texture);
  fireParticles=CreateFireSystem("Pyro_FireParticles",false);fireWisps=CreateFireSystem("Pyro_FlameTongues",false);embers=CreateFireSystem("Pyro_Embers",true);
  var wispsMain=fireWisps.main;wispsMain.maxParticles=96;wispsMain.startSize=new ParticleSystem.MinMaxCurve(.065f,.105f);var wispsEmission=fireWisps.emission;wispsEmission.rateOverTime=65;var wispsRenderer=fireWisps.GetComponent<ParticleSystemRenderer>();wispsRenderer.renderMode=ParticleSystemRenderMode.Stretch;wispsRenderer.velocityScale=0;wispsRenderer.lengthScale=1.15f;
 }
 void CreateShotEffects(){
  rocketGlowMat=new Material(mat);rocketGlowMat.SetColor("_Tint",new Color(1.8f,1.1f,.015f,.26f));
  for(int i=0;i<rocketGlows.Length;i++){var glowObject=new GameObject("Rocket_GoldenGlow_"+i);glowObject.transform.SetParent(poolRoot,false);glowObject.transform.localScale=Vector3.one*.48f;glowObject.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=glowObject.AddComponent<MeshRenderer>();mr.sharedMaterial=rocketGlowMat;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;mr.receiveShadows=false;glowObject.SetActive(false);rocketGlows[i]=glowObject.transform;}
  flashMat=new Material(Shader.Find("Stage2/MuzzleFlame"));
  flashMesh=new Mesh();var v=new Vector3[25];var uv=new Vector2[25];var tri=new int[72];
  for(int i=0;i<24;i++){float a=i*Mathf.PI/12,r=i%2==0?(i%6==0?.62f:.4f):.075f;v[i+1]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);uv[i+1]=Vector2.one*.5f;tri[i*3]=0;tri[i*3+1]=(i+1)%24+1;tri[i*3+2]=i+1;}flashMesh.vertices=v;flashMesh.uv=uv;flashMesh.triangles=tri;flashMesh.RecalculateBounds();
  smokeMat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));smokeMat.SetFloat("_Surface",1);smokeMat.SetFloat("_Blend",0);smokeMat.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);smokeMat.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);smokeMat.SetFloat("_ZWrite",0);smokeMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");smokeMat.renderQueue=3000;smokeMat.SetTexture("_BaseMap",texture);smokeMat.SetColor("_BaseColor",Color.white);
  var go=new GameObject("Soldier_RocketSmoke");go.transform.SetParent(poolRoot,false);rocketSmoke=go.AddComponent<ParticleSystem>();rocketSmoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=rocketSmoke.main;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=2400;main.startLifetime=.85f;main.startSpeed=0;main.startSize=.16f;
  var emission=rocketSmoke.emission;emission.enabled=false;var shape=rocketSmoke.shape;shape.enabled=false;
  var color=rocketSmoke.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.55f,.53f,.50f),0),new GradientColorKey(new Color(.32f,.32f,.32f),1)},new[]{new GradientAlphaKey(.65f,0),new GradientAlphaKey(.35f,.4f),new GradientAlphaKey(0,1)});color.color=gradient;
  var size=rocketSmoke.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,3));var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=smokeMat;renderer.sortMode=ParticleSystemSortMode.Distance;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;rocketSmoke.Play();
 }
 void EmitSmoke(Vector3 start,Vector3 end){int count=CPQualityEffects.Count(Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(start,end)/(CPWebPerformance.Active?.35f:.12f)),1,CPWebPerformance.Active?40:128),true);for(int i=0;i<count;i++){var p=new ParticleSystem.EmitParams();p.position=Vector3.Lerp(start,end,(i+.5f)/count);p.velocity=Vector3.up*.12f+Random.insideUnitSphere*.06f;p.startSize=Random.Range(.13f,.21f);p.startLifetime=Random.Range(.65f,1.05f)*(CPQualityEffects.Current==CPGraphicsQuality.Ultra?1:.6f);p.rotation=Random.Range(0f,360f);rocketSmoke.Emit(p,1);}}
 public ParticleSystem CreateFireSystem(string name,bool sparks){
  var go=new GameObject(name);go.transform.SetParent(poolRoot,false);var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=ps.main;main.loop=true;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;main.maxParticles=sparks?48:180;main.startLifetime=new ParticleSystem.MinMaxCurve(sparks?.2f:.35f,sparks?.4f:.56f);main.startSpeed=new ParticleSystem.MinMaxCurve(9.2f,12.65f);main.startSize=new ParticleSystem.MinMaxCurve(sparks?.012f:.10f,sparks?.022f:.16f);main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.gravityModifier=-.025f;
  var emission=ps.emission;emission.rateOverTime=sparks?18:95;
  var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=sparks?12:4.5f;shape.radius=.014f;shape.length=.02f;
  var noise=ps.noise;noise.enabled=true;noise.quality=ParticleSystemNoiseQuality.Low;noise.strength=sparks?.12f:.18f;noise.frequency=1.6f;noise.scrollSpeed=1.4f;noise.damping=true;
  var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.3f),new Keyframe(.18f,1),new Keyframe(.65f,2.7f),new Keyframe(1,3.6f)));
  var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.15f,.42f,1),0),new GradientColorKey(new Color(1.2f,1,.4f),.12f),new GradientColorKey(new Color(1,.36f,.035f),.42f),new GradientColorKey(new Color(.7f,.07f,.008f),.78f),new GradientColorKey(new Color(.18f,.025f,.01f),1)},new[]{new GradientAlphaKey(.85f,0),new GradientAlphaKey(.85f,.1f),new GradientAlphaKey(.62f,.5f),new GradientAlphaKey(0,1)});color.color=gradient;
  var rotation=ps.rotationOverLifetime;rotation.enabled=true;rotation.z=new ParticleSystem.MinMaxCurve(-1.8f,1.8f);
  var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=fireMat;renderer.renderMode=sparks?ParticleSystemRenderMode.Stretch:ParticleSystemRenderMode.Billboard;renderer.alignment=ParticleSystemRenderSpace.View;renderer.sortMode=ParticleSystemSortMode.Distance;renderer.velocityScale=.035f;renderer.lengthScale=1.8f;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.minParticleSize=0;renderer.maxParticleSize=.45f;
  return ps;
 }
 public ParticleSystem CreateBotFireSystem(string name){
  var ps=CreateFireSystem(name,false);var main=ps.main;main.maxParticles=260;main.startSize=new ParticleSystem.MinMaxCurve(.20f,.30f);main.startLifetime=new ParticleSystem.MinMaxCurve(.4f,.52f);main.startSpeed=new ParticleSystem.MinMaxCurve(10.35f,11.5f);
  var emission=ps.emission;emission.rateOverTime=175;var shape=ps.shape;shape.angle=6;shape.radius=.035f;
  if(botFireMat==null){botFireMat=new Material(fireMat);botFireMat.name="Bot_Flames_Visible";botFireMat.SetFloat("_Opacity",.82f);botFireMat.SetFloat("_Intensity",1);}
  ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=botFireMat;return ps;
 }
 public void StopFlame(){FlameActive=false;if(fireParticles!=null)fireParticles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(fireWisps!=null)fireWisps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(embers!=null)embers.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
 public void ScoutTracers(Vector3 origin,Vector3 cameraRight,Vector3 aimPoint){
  for(int n=0;n<2;n++)EmitTracer(origin+cameraRight*((n==0?-1:1)*.0189f),aimPoint);
 }
 public void HeavyTracer(Vector3 origin,Vector3 axis,Vector3 endpoint,int pellet){
  Vector3 right=Vector3.ProjectOnPlane(cam.transform.right,axis).normalized,up=Vector3.Cross(axis,right).normalized;
  float angle=pellet*Mathf.PI*.5f;
  EmitTracer(origin+(right*Mathf.Cos(angle)+up*Mathf.Sin(angle))*.02f,endpoint,2.4f);
 }
 void EmitTracer(Vector3 origin,Vector3 aimPoint,float width=1){
  int i=tracerIndex++%tracers.Length;var t=tracers[i];t.line.startWidth=.0035f*width;t.line.endWidth=.0015f*width;t.origin=origin;t.direction=(aimPoint-t.origin).normalized;t.age=0;t.distance=12;if(Physics.Raycast(t.origin,t.direction,out var hit,12,1,QueryTriggerInteraction.Ignore))t.distance=hit.distance;t.active=true;t.line.enabled=true;t.line.SetPosition(0,t.origin);t.line.SetPosition(1,t.origin+t.direction*.02f);tracers[i]=t;TracersEmitted++;
 }
 void Emit(Vector3 position,Vector3 velocity,float size,float life,Color color){int i=index++%Count;var p=pool[i];p.age=0;p.life=life;p.size=size;p.color=color;p.velocity=velocity;p.flash=false;p.tr.GetComponent<MeshFilter>().sharedMesh=mesh;p.renderer.sharedMaterial=mat;p.tr.position=position;p.tr.rotation=cam.transform.rotation;p.tr.localScale=Vector3.one*size;block.SetColor("_Tint",color);p.renderer.SetPropertyBlock(block);p.tr.gameObject.SetActive(true);pool[i]=p;}
 public void Flash(Vector3 p,float size){FlashFor(player.View.muzzle,player.currentClass,size);}
 public void FlashFor(Transform anchor,ClassId id,float size){Vector3 p=anchor.position;bool heavy=id==ClassId.Heavy;Emit(p,Vector3.zero,size*(heavy?2.6f:1.9f),heavy?.065f:.06f,Color.white);int i=(index-1)%Count;var fx=pool[i];fx.flash=true;fx.heavyFlash=heavy;fx.flashSeed=Random.Range(0f,100f);fx.anchor=anchor;fx.roll=Random.Range(-18f,18f);fx.tr.GetComponent<MeshFilter>().sharedMesh=mesh;fx.renderer.sharedMaterial=flashMat;block.SetFloat("_Heavy",heavy?1:0);block.SetFloat("_FlashSeed",fx.flashSeed);block.SetFloat("_FlashAge",0);block.SetColor("_Tint",Color.white);fx.renderer.SetPropertyBlock(block);pool[i]=fx;}
 public void Impact(Vector3 p){Emit(p,Vector3.up*.15f,.055f,.15f,new Color(2,1.2f,.4f,1));CPMatch.Instance?.audioSystem?.Impact(p);}
 public void Explosion(Vector3 p){rocketBurst.Emit(p);}
 public int DeflectRockets(CPActor source,Vector3 origin,Vector3 direction){int reflected=0;for(int i=0;i<rockets.Length;i++){var r=rockets[i];if(r.life<=0||r.shooter==null||r.shooter.team==source.team||!CPCombat.InAirblast(origin,direction,r.tr.position))continue;
   if(Vector3.Dot(r.velocity,origin-r.tr.position)<=0||Physics.Linecast(origin,r.tr.position,1,QueryTriggerInteraction.Ignore))continue;
   r.velocity=-r.velocity;r.tr.up=r.velocity.normalized;r.shotOrigin=origin;r.shooter=source;r.life=5.75f;rockets[i]=r;reflected++;
  }return reflected;}
 public void Airblast(Vector3 origin,Vector3 direction){for(int i=0;i<14;i++){var e=new ParticleSystem.EmitParams();e.position=origin+Random.insideUnitSphere*.12f;e.velocity=(direction+Random.insideUnitSphere*.28f)*9;e.startLifetime=.23f;e.startSize=.18f;e.startColor=new Color(.85f,.94f,1,.38f);rocketSmoke.Emit(e,1);}}
 public void Rocket(Vector3 p,Vector3 forward,Vector3 source,CPActor shooter=null){for(int i=0;i<rockets.Length;i++){if(rockets[i].life>0)continue;var r=rockets[i];r.tr.localScale=new Vector3(.14f,.18f,.14f);r.tr.position=p;r.tr.up=forward;r.velocity=forward*42;r.shotOrigin=source;r.shooter=shooter;r.life=5.75f;r.tr.gameObject.SetActive(true);rockets[i]=r;if(shooter==null)Flash(p,.2f);else FlashFor(shooter.muzzle,ClassId.Soldier,.2f);break;}}
 void UpdateFire(ParticleSystem ps,bool emitFire){if(emitFire){ps.transform.position=FlameOrigin.position;ps.transform.rotation=Quaternion.LookRotation((cam.transform.position+cam.transform.forward*CPFlameStats.Range-FlameOrigin.position).normalized);if(!ps.isEmitting)ps.Play();}else if(ps.isEmitting)ps.Stop(true,ParticleSystemStopBehavior.StopEmitting);}
 void Update(){if(cam==null)return;float dt=Time.deltaTime;
  ActiveTracers=0;for(int i=0;i<tracers.Length;i++){var t=tracers[i];if(!t.active)continue;t.age+=dt;float head=t.age*70;if(head>t.distance+.28f){t.active=false;t.line.enabled=false;}else{float tail=Mathf.Max(0,head-.28f);t.line.SetPosition(0,t.origin+t.direction*Mathf.Min(tail,t.distance));t.line.SetPosition(1,t.origin+t.direction*Mathf.Min(head,t.distance));ActiveTracers++;}tracers[i]=t;}
  ActiveSprites=0;
  for(int i=0;i<Count;i++){var p=pool[i];if(p.life<=0)continue;p.age+=dt;if(p.age>=p.life){p.life=0;p.tr.gameObject.SetActive(false);pool[i]=p;continue;}float t=p.age/p.life;p.tr.position+=p.velocity*dt;p.tr.rotation=cam.transform.rotation*Quaternion.Euler(0,0,p.flash?p.roll:0);p.tr.localScale=Vector3.one*p.size*(p.flash?1-t*.4f:1+t*2);Color color=Color.Lerp(p.color,new Color(.75f,.08f,.005f,0),t*t);color.a=p.color.a*(1-t);block.SetColor("_Tint",color);if(p.flash){block.SetFloat("_Heavy",p.heavyFlash?1:0);block.SetFloat("_FlashSeed",p.flashSeed);block.SetFloat("_FlashAge",t);}p.renderer.SetPropertyBlock(block);pool[i]=p;ActiveSprites++;}
  for(int i=0;i<rockets.Length;i++){var r=rockets[i];if(r.life<=0)continue;Vector3 step=r.velocity*dt;RaycastHit h;bool hit=r.shooter!=null&&CPMatch.Instance!=null?CPMatch.Instance.combat.Cast(r.shooter,r.tr.position,step.normalized,step.magnitude,out h):Physics.Raycast(r.tr.position,step.normalized,out h,step.magnitude,1,QueryTriggerInteraction.Ignore);EmitSmoke(r.tr.position,hit?h.point:r.tr.position+step);r.life-=dt;if(hit||r.life<=0){r.life=0;r.tr.gameObject.SetActive(false);if(hit){if(r.shooter!=null&&CPMatch.Instance!=null)CPMatch.Instance.combat.Explode(r.shooter,h.point+r.velocity.normalized*-.02f,r.shotOrigin);else player.Explode(h.point+r.velocity.normalized*-.02f,r.shotOrigin);}}else r.tr.position+=step;rockets[i]=r;}
 }
 void OnDestroy(){if(poolRoot!=null)Destroy(poolRoot.gameObject);if(mat!=null)Destroy(mat);if(tracerMat!=null)Destroy(tracerMat);if(fireMat!=null)Destroy(fireMat);if(botFireMat!=null)Destroy(botFireMat);if(mesh!=null)Destroy(mesh);if(texture!=null)Destroy(texture);if(fireTexture!=null)Destroy(fireTexture);if(flashMesh!=null)Destroy(flashMesh);if(flashMat!=null)Destroy(flashMat);if(smokeMat!=null)Destroy(smokeMat);}
}
}
