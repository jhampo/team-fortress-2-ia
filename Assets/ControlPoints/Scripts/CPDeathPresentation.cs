using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace FpsStage2 {
public static class CPDeathPresentation {
 public const float DropLifetime=6;
 sealed class Topology {public int[] vertices;public int[][] triangles;public Vector2[] uv;}
 static readonly Dictionary<Mesh,Topology[]> topology=new Dictionary<Mesh,Topology[]>();
 static readonly Dictionary<Mesh,Mesh> restCache=new Dictionary<Mesh,Mesh>();
 public static void Warmup(CPActor[] actors){foreach(var actor in actors){if(actor.isPlayer)continue;var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);if(skins.All(s=>restCache.ContainsKey(s.sharedMesh)))continue;var bones=skins.SelectMany(s=>s.bones).Distinct().ToArray();var rotations=bones.Select(b=>b.localRotation).ToArray();try{Relax(bones,actor.transform);foreach(var skin in skins){if(!restCache.ContainsKey(skin.sharedMesh))restCache[skin.sharedMesh]=Bake(skin,actor.transform);if(!topology.ContainsKey(skin.sharedMesh))topology[skin.sharedMesh]=BuildTopology(skin);}}finally{for(int i=0;i<bones.Length;i++)bones[i].localRotation=rotations[i];}}}
 public static void ClearCaches(){foreach(var m in restCache.Values)if(m!=null)Object.Destroy(m);restCache.Clear();topology.Clear();}
 static Topology[] BuildTopology(SkinnedMeshRenderer skin){var mesh=skin.sharedMesh;var weights=mesh.boneWeights;var bones=skin.bones;var regionOf=new int[mesh.vertexCount];var uv=mesh.uv;for(int i=0;i<weights.Length;i++){var w=weights[i];int bone=w.boneIndex0;float max=w.weight0;if(w.weight1>max){bone=w.boneIndex1;max=w.weight1;}if(w.weight2>max){bone=w.boneIndex2;max=w.weight2;}if(w.weight3>max)bone=w.boneIndex3;regionOf[i]=bone<bones.Length?Region(bones[bone].name):0;}var result=new Topology[8];var input=new int[mesh.subMeshCount][];for(int s=0;s<input.Length;s++)input[s]=mesh.GetTriangles(s);
  for(int region=0;region<8;region++){var index=new Dictionary<int,int>();var oldIndices=new List<int>();var triangles=new int[input.Length][];for(int s=0;s<input.Length;s++){var tri=new List<int>();for(int t=0;t<input[s].Length;t+=3){int ia=input[s][t],ib=input[s][t+1],ic=input[s][t+2],a=regionOf[ia],b=regionOf[ib],c=regionOf[ic];int group=a==b||a==c?a:b==c?b:a;if(group!=region)continue;for(int j=0;j<3;j++){int old=input[s][t+j];if(!index.TryGetValue(old,out int v)){v=oldIndices.Count;index.Add(old,v);oldIndices.Add(old);}tri.Add(v);}}triangles[s]=tri.ToArray();}result[region]=new Topology{vertices=oldIndices.ToArray(),triangles=triangles,uv=oldIndices.Select(i=>i<uv.Length?uv[i]:Vector2.zero).ToArray()};}return result;
 }
 public static void Spawn(CPActor actor,bool explosive=false,Vector3? blastOrigin=null){
  var source=actor;if(actor.isPlayer)source=CPMatch.Instance.actors.FirstOrDefault(a=>!a.isPlayer&&a.team==actor.team&&a.characterClass==actor.characterClass);if(source==null)return;
  var skins=source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
  if(explosive)Fragments(actor,source,skins,blastOrigin??actor.transform.position-actor.transform.forward);
  else {
   var corpse=new GameObject("Corpse_"+actor.name);corpse.transform.SetPositionAndRotation(actor.transform.position,actor.transform.rotation);
   var meshes=new List<Mesh>();foreach(var skin in skins)Copy(skin,corpse.transform,source.transform,meshes);
   // Release the corpse's limbs; preserve the live rig and all animation clips.
   if(skins.Any(s=>!restCache.ContainsKey(s.sharedMesh)))Warmup(new[]{source});var resting=skins.Select(s=>Object.Instantiate(restCache[s.sharedMesh])).ToList();
   corpse.AddComponent<CPDeathFall>().Initialize(meshes,false,DropLifetime,resting);
  }
  var weaponRoot=source.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r=>r.name.EndsWith("_Weapon"));
  if(weaponRoot!=null){
   var drop=new GameObject("Dropped_"+actor.characterClass);drop.transform.SetPositionAndRotation(actor.transform.position,actor.transform.rotation);var models=new GameObject("Weapon_Model").transform;models.SetParent(drop.transform,false);var meshes=new List<Mesh>();
   foreach(var r in weaponRoot.GetComponentsInChildren<MeshRenderer>(true))Copy(r,models,source.transform,meshes);
   Center(meshes,drop.transform,actor.transform);
   drop.AddComponent<CPDeathFall>().Initialize(meshes,true,DropLifetime);var pickup=drop.AddComponent<CPCollectible>();pickup.kind=CPSupplyKind.DroppedWeapon;pickup.artwork=models;pickup.lifetimeSeconds=DropLifetime;
  }
  if(actor.isPlayer){var camera=actor.GetComponent<CPDeathCamera>();if(camera==null)camera=actor.gameObject.AddComponent<CPDeathCamera>();camera.Begin(actor.GetComponent<FpsStage2Player>().playerCamera);}
 }
 static void Relax(Transform[] bones,Transform root){
  Aim(bones,root,"Hips","Spine",Vector3.up);Aim(bones,root,"Spine","Spine01",Vector3.up);Aim(bones,root,"Spine01","Spine02",Vector3.up);
  foreach(string side in new[]{"Left","Right"}){float sign=side=="Left"?-1:1;
   Aim(bones,root,side+"UpLeg",side+"Leg",new Vector3(sign*.075f,-1,0));
   Aim(bones,root,side+"Leg",side+"Foot",new Vector3(sign*.035f,-1,.025f));
   Aim(bones,root,side+"Arm",side+"ForeArm",new Vector3(sign*.32f,-1,.025f));
   Aim(bones,root,side+"ForeArm",side+"Hand",new Vector3(sign*.16f,-1,.035f));
   Aim(bones,root,side+"Hand",side+"GripFingers",new Vector3(sign*.12f,-1,.03f));
  }
 }
 static void Aim(Transform[] bones,Transform root,string name,string child,Vector3 direction){var a=bones.FirstOrDefault(b=>b.name==name);var b=bones.FirstOrDefault(t=>t.name==child);if(a==null||b==null)return;var from=b.position-a.position;if(from.sqrMagnitude>.000001f)a.rotation=Quaternion.FromToRotation(from,root.TransformDirection(direction))*a.rotation;}
 static Mesh Bake(Renderer source,Transform actor){
  var mesh=new Mesh();if(source is SkinnedMeshRenderer skin)skin.BakeMesh(mesh,true);else{var mf=source.GetComponent<MeshFilter>();if(mf==null){Object.Destroy(mesh);return null;}Object.Destroy(mesh);mesh=Object.Instantiate(mf.sharedMesh);}
  var matrix=actor.worldToLocalMatrix*source.localToWorldMatrix;var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);mesh.vertices=vertices;var normal=matrix.inverse.transpose;var normals=mesh.normals;for(int i=0;i<normals.Length;i++)normals[i]=normal.MultiplyVector(normals[i]).normalized;mesh.normals=normals;mesh.RecalculateBounds();return mesh;
 }
 static void Copy(Renderer source,Transform parent,Transform actor,List<Mesh> owned){var mesh=Bake(source,actor);if(mesh==null)return;owned.Add(mesh);MakeRenderer(source.name,parent,mesh,source.sharedMaterials);}
 static void MakeRenderer(string name,Transform parent,Mesh mesh,Material[] materials){var go=new GameObject(name);go.layer=2;go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=materials;}
 static void Center(List<Mesh> meshes,Transform item,Transform actor){var points=meshes.SelectMany(m=>m.vertices).ToArray();if(points.Length==0)return;var b=new Bounds(points[0],Vector3.zero);foreach(var v in points)b.Encapsulate(v);foreach(var m in meshes){m.vertices=m.vertices.Select(v=>v-b.center).ToArray();m.RecalculateBounds();}item.position=actor.TransformPoint(b.center);}
 static int Region(string bone){
  if(bone=="Head"||bone.StartsWith("head")||bone=="neck")return 1;
  if(bone.StartsWith("Left")&&(bone.Contains("Arm")||bone.Contains("Hand")||bone.Contains("Grip")||bone.Contains("Finger")))return 2;
  if(bone.StartsWith("Right")&&(bone.Contains("Arm")||bone.Contains("Hand")||bone.Contains("Grip")||bone.Contains("Finger")))return 3;
  if(bone=="LeftUpLeg")return 4;if(bone=="RightUpLeg")return 5;
  if(bone.StartsWith("Left")&&(bone.Contains("Leg")||bone.Contains("Foot")||bone.Contains("Toe")))return 6;
  if(bone.StartsWith("Right")&&(bone.Contains("Leg")||bone.Contains("Foot")||bone.Contains("Toe")))return 7;return 0;
 }
 static void Fragments(CPActor actor,CPActor source,SkinnedMeshRenderer[] skins,Vector3 blast){
  foreach(var skin in skins){var whole=Bake(skin,source.transform);var vertices=whole.vertices;var normals=whole.normals;if(!topology.TryGetValue(skin.sharedMesh,out var maps)){maps=BuildTopology(skin);topology[skin.sharedMesh]=maps;}
   for(int region=0;region<8;region++){
    var map=maps[region];if(map.vertices.Length==0)continue;var verts=new Vector3[map.vertices.Length];var norms=new Vector3[verts.Length];for(int i=0;i<verts.Length;i++){int old=map.vertices[i];verts[i]=vertices[old];norms[i]=old<normals.Length?normals[old]:Vector3.up;}var mesh=new Mesh{name="DeathFragment_"+region,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.vertices=verts;mesh.normals=norms;mesh.uv=map.uv;mesh.subMeshCount=map.triangles.Length;for(int s=0;s<map.triangles.Length;s++)mesh.SetTriangles(map.triangles[s],s);mesh.RecalculateBounds();
    var piece=new GameObject("Fragment_"+actor.name+"_"+region);piece.transform.SetPositionAndRotation(actor.transform.position,actor.transform.rotation);var list=new List<Mesh>{mesh};Center(list,piece.transform,actor.transform);MakeRenderer(skin.name,piece.transform,mesh,skin.sharedMaterials);
    piece.AddComponent<CPFragmentMotion>().Initialize(mesh,blast,DropLifetime);
   }Object.Destroy(whole);
  }
 }
}
public sealed class CPDeathFall:MonoBehaviour {
 public const float BodyDuration=.32f;
 Mesh[] owned,resting;Vector3[][] from,to,work,normals;Vector3 start,finish;Quaternion rotation,fallen;float born,duration;bool morphed;public bool Settled=>Time.time-born>=duration;
 public void Initialize(List<Mesh> meshes,bool weapon,float lifetime,List<Mesh> rest=null){
  owned=meshes.ToArray();resting=rest?.ToArray();if(resting!=null){from=owned.Select(m=>m.vertices).ToArray();to=resting.Select(m=>m.vertices).ToArray();normals=resting.Select(m=>m.normals).ToArray();work=from.Select(v=>new Vector3[v.Length]).ToArray();}
  start=transform.position;rotation=transform.rotation;fallen=rotation*Quaternion.Euler(weapon?8:-90,0,weapon?80:0);born=Time.time;duration=weapon?.3f:BodyDuration;
  float minimum=float.PositiveInfinity;foreach(var mesh in resting??owned)foreach(var v in mesh.vertices)minimum=Mathf.Min(minimum,(fallen*v).y);if(float.IsInfinity(minimum))minimum=0;
  finish=start;Vector3 floorPoint=start;RaycastHit hit;if(!weapon){var forward=rotation*Vector3.forward;floorPoint-=forward*.8f;if(Physics.SphereCast(start+Vector3.up*.6f,.25f,-forward,out hit,1.8f,1,QueryTriggerInteraction.Ignore)){finish+=forward*Mathf.Clamp(1.8f-hit.distance,0,1.6f);floorPoint=finish-forward*.8f;}}
  float floor=start.y-(weapon?1:0);if(Physics.Raycast(floorPoint+Vector3.up*2,Vector3.down,out hit,12,1,QueryTriggerInteraction.Ignore))floor=hit.point.y;
  finish.y=floor-minimum+.009f;Destroy(gameObject,lifetime);
 }
 void Update(){float t=Mathf.Clamp01((Time.time-born)/duration);float impact=t*t;transform.rotation=Quaternion.Slerp(rotation,fallen,impact);transform.position=Vector3.Lerp(start,finish,impact);
  if(resting!=null&&!morphed){float relax=Mathf.SmoothStep(0,1,Mathf.Clamp01(t*1.35f));for(int m=0;m<owned.Length;m++){for(int v=0;v<work[m].Length;v++)work[m][v]=Vector3.Lerp(from[m][v],to[m][v],relax);owned[m].vertices=work[m];owned[m].RecalculateBounds();if(relax>=1)owned[m].normals=normals[m];}morphed=relax>=1;}
 }
 void OnDestroy(){if(owned!=null)foreach(var m in owned)if(m!=null)Destroy(m);if(resting!=null)foreach(var m in resting)if(m!=null)Destroy(m);}
}
public sealed class CPFragmentMotion:MonoBehaviour {
 Mesh mesh;Vector3 velocity,spin;bool settled;float age;const float Radius=.10f;
 public void Initialize(Mesh owned,Vector3 blast,float lifetime){mesh=owned;var away=Vector3.ProjectOnPlane(transform.position-blast,Vector3.up).normalized;velocity=away*Random.Range(2.2f,4.5f)+Random.insideUnitSphere*1.4f+Vector3.up*Random.Range(3,5);spin=Random.onUnitSphere*Random.Range(150,390);Destroy(gameObject,lifetime);}
 void Update(){if(settled)return;float dt=Time.deltaTime;age+=dt;velocity+=Vector3.down*19*dt;var step=velocity*dt;RaycastHit hit;if(step.sqrMagnitude>0&&Physics.SphereCast(transform.position,Radius,step.normalized,out hit,step.magnitude,1,QueryTriggerInteraction.Ignore)){transform.position=hit.point+hit.normal*(Radius+.01f);velocity=Vector3.Reflect(velocity,hit.normal)*.24f;spin*=.35f;if(hit.normal.y>.65f&&(age>.65f||velocity.magnitude<1.4f))Settle();}else transform.position+=step;if(!settled)transform.Rotate(spin*dt,Space.World);if(age>2.5f)Settle();}
 void Settle(){float lowest=float.PositiveInfinity;foreach(var v in mesh.vertices)lowest=Mathf.Min(lowest,transform.TransformPoint(v).y);RaycastHit hit;if(Physics.Raycast(transform.position+Vector3.up,Vector3.down,out hit,8,1,QueryTriggerInteraction.Ignore)){transform.position+=Vector3.up*(hit.point.y-lowest+.008f);settled=true;}}
 void OnDestroy(){if(mesh!=null)Destroy(mesh);}
}
[DefaultExecutionOrder(350)] public sealed class CPDeathCamera:MonoBehaviour {
 Camera view;Vector3 position;Quaternion rotation;float born;bool active;
 public void Begin(Camera camera){if(active)Restore();view=camera;position=view.transform.localPosition;rotation=view.transform.localRotation;born=Time.time;active=true;}
 void LateUpdate(){if(!active||view==null)return;float t=Mathf.Clamp01((Time.time-born)/CPDeathFall.BodyDuration);t*=t;var end=position+new Vector3(0,-1.35f,-.30f);view.transform.localPosition=Vector3.Lerp(position,end,t);view.transform.localRotation=rotation*Quaternion.Euler(-55*t,0,-9*t);}
 public void Restore(){if(!active||view==null)return;view.transform.localPosition=position;view.transform.localRotation=rotation;active=false;}
 void OnDisable(){Restore();}
}
}
