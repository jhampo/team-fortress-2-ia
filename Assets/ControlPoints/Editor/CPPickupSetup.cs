using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace FpsStage2 {
public static class CPPickupSetup {
 const string Root="Assets/ControlPoints/Pickups";
 static Material teal,white,red,black,metal,wood,gold;
 static int serial;static string model;
 [Serializable] class LampOnly {public CPExpansionSetup.Lamp[] lights;}
 public static string SyncLights(){var input=JsonUtility.FromJson<LampOnly>(File.ReadAllText("C:/Users/Usuario/Documents/Codex/2026-09-17/di/outputs/Powerhouse/UnityTransfer_V4.json"));var root=GameObject.Find("Powerhouse").transform;int n=0;foreach(var lamp in input.lights){var t=root.Find(lamp.name);if(t!=null){t.position=lamp.position;n++;}}return n+" light positions synchronized.";}
 static Material Mat(string name,Color color){string path=Root+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.24f);EditorUtility.SetDirty(m);return m;}
 static GameObject Part(Transform parent,string name,Mesh mesh,Material material){mesh.RecalculateNormals();mesh.RecalculateBounds();string path=Root+"/"+model+"_"+serial+++".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;EditorUtility.SetDirty(old);}var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go;}
 static Mesh Rings(List<Vector3[]> rings){var v=rings.SelectMany(r=>r).ToArray();int n=rings[0].Length;var triangles=new List<int>();for(int r=0;r<rings.Count-1;r++)for(int i=0;i<n;i++){int j=(i+1)%n,a=r*n+i,b=r*n+j,c=(r+1)*n+j,d=(r+1)*n+i;triangles.AddRange(new[]{a,c,b,a,d,c});}for(int i=1;i<n-1;i++){triangles.AddRange(new[]{0,i,i+1,(rings.Count-1)*n,(rings.Count-1)*n+i+1,(rings.Count-1)*n+i});}return new Mesh{vertices=v,triangles=triangles.ToArray()};}
 static GameObject Box(Transform p,string name,Vector3 center,Vector3 size,Material mat,float bevel=.025f){
  float x=size.x/2,z=size.z/2,b=Mathf.Min(bevel,Mathf.Min(x,z)*.4f);var rings=new List<Vector3[]>();foreach(var level in new[]{new Vector2(-size.y/2,.94f),new Vector2(-size.y/2+b,1),new Vector2(size.y/2-b,1),new Vector2(size.y/2,.94f)}){float xx=x*level.y,zz=z*level.y;var pts=new[]{new Vector2(-xx+b,-zz),new Vector2(xx-b,-zz),new Vector2(xx,-zz+b),new Vector2(xx,zz-b),new Vector2(xx-b,zz),new Vector2(-xx+b,zz),new Vector2(-xx,zz-b),new Vector2(-xx,-zz+b)};rings.Add(pts.Select(v=>new Vector3(v.x,level.x,v.y)+center).ToArray());}return Part(p,name,Rings(rings),mat);
 }
 static GameObject Lathe(Transform p,string n,Vector2[] profile,Material mat,Vector3 center,int segments=24){var rings=new List<Vector3[]>();foreach(var level in profile){var ring=new Vector3[segments];for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments;ring[i]=center+new Vector3(Mathf.Cos(a)*level.y,level.x,Mathf.Sin(a)*level.y);}rings.Add(ring);}return Part(p,n,Rings(rings),mat);}
 static void Cross(Transform p,Vector3 center,float size,Material mat,bool top){Box(p,"Medical_Cross",center,top?new Vector3(size,.008f,size*.31f):new Vector3(size,size*.31f,.008f),mat,.002f);Box(p,"Medical_Cross",center,top?new Vector3(size*.31f,.009f,size):new Vector3(size*.31f,size,.009f),mat,.002f);}
 static void Handle(Transform p,Vector3 center,float width,float height,Material mat){Box(p,"Handle_Left",center+new Vector3(-width/2,height/2,0),new Vector3(.047f,height,.06f),mat,.008f);Box(p,"Handle_Right",center+new Vector3(width/2,height/2,0),new Vector3(.047f,height,.06f),mat,.008f);Box(p,"Handle_Top",center+Vector3.up*height,new Vector3(width+.04f,.06f,.06f),mat,.015f);}
 public static string BuildPrefabs(){
  if(Application.isPlaying)throw new Exception("Stop Play first");Directory.CreateDirectory(Root);
  teal=Mat("Medical_Teal",new Color(.025f,.37f,.39f));white=Mat("Medical_Ivory",new Color(.77f,.79f,.73f));red=Mat("Medical_Red",new Color(.70f,.035f,.028f));black=Mat("Handle_Black",new Color(.035f,.043f,.039f));metal=Mat("Case_Metal",new Color(.30f,.33f,.31f));wood=Mat("Ammo_Olive",new Color(.37f,.34f,.21f));gold=Mat("Bullet_Brass",new Color(.63f,.47f,.13f));
  foreach(var kind in new[]{CPSupplyKind.HealthSmall,CPSupplyKind.HealthMedium,CPSupplyKind.HealthLarge,CPSupplyKind.AmmoSmall,CPSupplyKind.AmmoMedium}){
   model=kind.ToString();serial=0;var go=new GameObject(model);var art=new GameObject("Model").transform;art.SetParent(go.transform,false);var collectible=go.AddComponent<CPCollectible>();collectible.kind=kind;collectible.artwork=art;collectible.respawnSeconds=10;
   if(kind==CPSupplyKind.HealthSmall){
    Lathe(art,"Green_Bottle",new[]{new Vector2(0,.12f),new Vector2(.025f,.15f),new Vector2(.42f,.15f),new Vector2(.49f,.105f),new Vector2(.51f,.08f)},teal,Vector3.zero);
    Lathe(art,"White_Label",new[]{new Vector2(.105f,.152f),new Vector2(.345f,.152f)},white,Vector3.zero);
    Lathe(art,"White_Cap",new[]{new Vector2(.49f,.085f),new Vector2(.50f,.105f),new Vector2(.63f,.105f),new Vector2(.645f,.093f)},white,Vector3.zero,16);
    Cross(art,new Vector3(0,.225f,-.155f),.195f,red,false);
   }else if(kind==CPSupplyKind.HealthMedium){
    Box(art,"Ivory_Case",new Vector3(0,.12f,0),new Vector3(.74f,.24f,.54f),white,.035f);Box(art,"Teal_Lid",new Vector3(0,.275f,0),new Vector3(.78f,.09f,.58f),teal,.045f);
    Lathe(art,"Red_Lid_Disc",new[]{new Vector2(.32f,.221f),new Vector2(.325f,.221f)},red,Vector3.zero);
    Cross(art,new Vector3(0,.332f,0),.36f,white,true);Handle(art,new Vector3(0,.05f,-.302f),.27f,.12f,metal);
   }else if(kind==CPSupplyKind.HealthLarge){
    Box(art,"Teal_Cooler",new Vector3(0,.245f,0),new Vector3(1.06f,.49f,.61f),teal,.065f);Box(art,"Lid_Rim",new Vector3(0,.495f,0),new Vector3(1.12f,.055f,.66f),teal,.04f);Box(art,"Ivory_Lid",new Vector3(0,.586f,0),new Vector3(1.07f,.15f,.625f),white,.055f);Handle(art,new Vector3(0,.65f,0),.35f,.24f,black);
    var disk=Lathe(art,"White_Medallion",new[]{new Vector2(-.004f,.17f),new Vector2(.004f,.17f)},white,Vector3.zero);disk.transform.localPosition=new Vector3(-.28f,.26f,-.311f);disk.transform.localRotation=Quaternion.Euler(90,0,0);Cross(art,new Vector3(-.28f,.26f,-.32f),.255f,red,false);
   }else{
    Box(art,"Ammo_Crate",new Vector3(0,.19f,0),new Vector3(.71f,.38f,.46f),wood,.026f);Box(art,"Ammo_Lid",new Vector3(0,.39f,0),new Vector3(.75f,.045f,.50f),metal,.009f);
    foreach(float x in new[]{-.24f,.24f})Box(art,"Ammo_Band",new Vector3(x,.2f,-.238f),new Vector3(.045f,.36f,.022f),metal,.004f);
    for(int i=0;i<3;i++){var bullet=Lathe(art,"Cartridge_Emblem",new[]{new Vector2(0,.018f),new Vector2(.14f,.018f),new Vector2(.20f,0)},gold,new Vector3((i-1)*.064f,.095f,-.248f),8);}
    if(kind==CPSupplyKind.AmmoSmall)art.localScale=Vector3.one*.68f;
   }
   PrefabUtility.SaveAsPrefabAsset(go,Root+"/"+model+".prefab");UnityEngine.Object.DestroyImmediate(go);
  }AssetDatabase.SaveAssets();return "Five grounded supply prefabs built.";
 }
 public static string Place(){
  if(Application.isPlaying)throw new Exception("Stop Play first");var m=UnityEngine.Object.FindFirstObjectByType<CPMatch>();var nav=NavMesh.AddNavMeshData(m.navigation);
  var old=GameObject.Find("Map_Pickups");if(old!=null)UnityEngine.Object.DestroyImmediate(old);var root=new GameObject("Map_Pickups");
  foreach(var t in m.transform.Cast<Transform>().Where(t=>t.name.StartsWith("Supply_")).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
  int count=0;try{
   foreach(float side in new[]{-1f,1f}){
    Spawn(root.transform,side,CPSupplyKind.HealthSmall,21,3,-7);Spawn(root.transform,side,CPSupplyKind.HealthSmall,42,2.9f,8);
    Spawn(root.transform,side,CPSupplyKind.HealthMedium,63,4.7f,4);Spawn(root.transform,side,CPSupplyKind.HealthMedium,73,8.9f,39);Spawn(root.transform,side,CPSupplyKind.HealthMedium,116,4.7f,10);Spawn(root.transform,side,CPSupplyKind.HealthMedium,55,2.9f,-27);
    Spawn(root.transform,side,CPSupplyKind.HealthLarge,96,4.7f,-47);
    Spawn(root.transform,side,CPSupplyKind.AmmoSmall,21,3,-10);Spawn(root.transform,side,CPSupplyKind.AmmoSmall,43,2.9f,3);
    foreach(var p in new[]{new Vector3(66,4.7f,3),new Vector3(90,4.7f,-22),new Vector3(75,8.9f,38),new Vector3(108,4.7f,-44),new Vector3(57,2.9f,-25)})Spawn(root.transform,side,CPSupplyKind.AmmoMedium,p.x,p.y,p.z);
   }count=root.transform.childCount;
  }finally{nav.Remove();}
  var excess=GameObject.Find("RED_Scout_Bot_2");if(excess!=null)UnityEngine.Object.DestroyImmediate(excess);
  var map=GameObject.Find("Powerhouse");var guard=map.transform.Find("CP_OuterSafety");if(guard!=null)UnityEngine.Object.DestroyImmediate(guard.gameObject);var guards=new GameObject("CP_OuterSafety").transform;guards.SetParent(map.transform,false);
  foreach(float sign in new[]{-1f,1f})Barrier(guards,new Vector3(sign*156,13,-9.5f),new Vector3(.8f,24,151));
  Barrier(guards,new Vector3(0,13,66),new Vector3(313,24,.8f));Barrier(guards,new Vector3(0,13,-85),new Vector3(313,24,.8f));
  m.healthStations=root.GetComponentsInChildren<CPCollectible>().Where(p=>p.IsHealth).Select(p=>p.transform.position).ToArray();EditorUtility.SetDirty(m);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());return count+" pickups, two full-health coolers; RED seven bots plus player; perimeter safety.";
 }
 static void Barrier(Transform parent,Vector3 p,Vector3 size){var go=new GameObject("Boundary_Clip");go.transform.SetParent(parent,false);go.transform.position=p;go.AddComponent<BoxCollider>().size=size;}
 static void Spawn(Transform root,float side,CPSupplyKind kind,float x,float y,float z){
  var desired=new Vector3(side*x,y,z);NavMeshHit hit;Vector3 point=desired;bool found=false;
  foreach(var offset in new[]{Vector3.zero,Vector3.right,Vector3.left,Vector3.forward,Vector3.back,Vector3.right*2,Vector3.forward*2}){
   if(!NavMesh.SamplePosition(desired+offset,out hit,1.1f,-1))continue;point=hit.position;RaycastHit floor;if(Physics.Raycast(point+Vector3.up*.2f,Vector3.down,out floor,1,1,QueryTriggerInteraction.Ignore))point.y=floor.point.y+.015f;
   if(Physics.CheckCapsule(point+Vector3.up*.26f,point+Vector3.up*.8f,.23f,1,QueryTriggerInteraction.Ignore))continue;found=true;break;
  }
  if(!found)throw new Exception("Blocked supply position "+kind+" "+desired);
  var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+kind+".prefab"));go.name=(side>0?"RED":"BLU")+"_"+kind+"_"+root.childCount;go.transform.SetParent(root,false);go.transform.position=point;go.transform.rotation=Quaternion.Euler(0,side>0?25:-25,0);
 }
}
}
