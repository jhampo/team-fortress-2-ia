using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace FpsStage2 {
public static class CPExpansionSetup {
 const string AssetsRoot="Assets/Powerhouse";
 const string Input="C:/Users/Usuario/Documents/Codex/2026-09-17/di/outputs/Powerhouse/";
 [Serializable] public class Transfer {public Mat[] materials;public Chunk[] chunks;public Lamp[] lights;}
 [Serializable] public class Mat {public string name;public Color color;public float metal,rough;}
 [Serializable] public class Chunk {public string name,material;public bool solid;public Vector3[] vertices,normals;public Vector2[] uv;}
 [Serializable] public class Lamp {public string name;public Vector3 position;public Color color;public float range,intensity;}
 [Serializable] public class Manifest {public string[] collections;public CPAlternativeRoute[] routes;public CheckPoint[] checks;}
 [Serializable] public class CheckPoint {public string name;public Vector3 p;}
 static Transfer transfer;static Manifest manifest;static GameObject staging;static int imported;
 static Dictionary<string,Material> materials;
 static void EditOnly(){if(Application.isPlaying)throw new Exception("Stop Play mode first.");}
 public static string BeginImport(){
  EditOnly();if(GameObject.Find("Powerhouse_V4_Staging")!=null)throw new Exception("An import is already staged.");
  transfer=JsonUtility.FromJson<Transfer>(File.ReadAllText(Input+"UnityTransfer_V4.json"));
  manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Input+"ExpandedRoutes_Manifest.json"));
  materials=new Dictionary<string,Material>();Directory.CreateDirectory(AssetsRoot+"/Meshes_V5");
  foreach(var description in transfer.materials){
   string path=AssetsRoot+"/Materials/"+description.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=description.name};m.SetColor("_BaseColor",description.color.gamma);m.SetFloat("_Metallic",description.metal);m.SetFloat("_Smoothness",1-description.rough);m.SetFloat("_Cull",0);if(description.name.Contains("Indicator")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(.1f,.7f,.2f));}AssetDatabase.CreateAsset(m,path);}
   materials[description.name]=m;
  }
  staging=new GameObject("Powerhouse_V4_Staging");staging.SetActive(false);imported=0;
  return "Prepared "+transfer.chunks.Length+" geometry batches.";
 }
 public static string ImportBatch(int count=75){
  EditOnly();if(transfer==null||staging==null)throw new Exception("BeginImport first.");
  int stop=Mathf.Min(imported+count,transfer.chunks.Length);
  AssetDatabase.StartAssetEditing();
  try{for(;imported<stop;imported++){
   var c=transfer.chunks[imported];string path=AssetsRoot+"/Meshes_V5/"+c.name+".asset";
   var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh{name=c.name};AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
   mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.vertices=c.vertices;mesh.normals=c.normals;mesh.uv=c.uv;mesh.triangles=Enumerable.Range(0,c.vertices.Length).ToArray();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
   var go=new GameObject(c.name);go.transform.SetParent(staging.transform,false);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=materials[c.material];
   if(c.solid)go.AddComponent<MeshCollider>().sharedMesh=mesh;if(c.name.Contains("CollisionOnly"))r.enabled=false;
   if(c.material.Contains("Water")){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;go.AddComponent<PowerhouseWater>().speed=c.material.Contains("Aerated")?new Vector2(0,-.42f):new Vector2(.025f,.045f);}
  }}finally{AssetDatabase.StopAssetEditing();}
  return imported+"/"+transfer.chunks.Length;
 }
 public static string FinishImport(){
  EditOnly();if(imported!=transfer.chunks.Length)throw new Exception("Import all batches first.");
  var root=GameObject.Find("Powerhouse");int replaced=0;
  foreach(var old in root.transform.Cast<Transform>().Where(t=>manifest.collections.Any(prefix=>t.name.StartsWith(prefix+"_"))).ToArray()){UnityEngine.Object.DestroyImmediate(old.gameObject);replaced++;}
  foreach(var child in staging.transform.Cast<Transform>().ToArray())child.SetParent(root.transform,false);
  UnityEngine.Object.DestroyImmediate(staging);
  foreach(var source in transfer.lights){
   var oldLight=root.transform.Find(source.name);var go=oldLight!=null?oldLight.gameObject:new GameObject(source.name);go.transform.SetParent(root.transform,false);go.transform.position=source.position;
   var lamp=go.GetComponent<Light>()??go.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=source.color;lamp.range=source.name.Contains("Turbine")?22:16;lamp.intensity=source.name.Contains("Turbine")?5.5f:5;lamp.shadows=LightShadows.None;
  }
  var match=UnityEngine.Object.FindFirstObjectByType<CPMatch>();match.alternativeRoutes=manifest.routes;EditorUtility.SetDirty(match);
  transfer=null;materials=null;AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
  return "Replaced "+replaced+" geometry batches; added control/turbine rooms and eight route corridors.";
 }
 public static string PopulateTeams(){
  EditOnly();var original=UnityEngine.Object.FindObjectsByType<CPActor>(FindObjectsSortMode.None).Where(a=>!a.isPlayer).ToList();int created=0;
  foreach(var team in new[]{CPTeam.RED,CPTeam.BLU})foreach(ClassId id in Enum.GetValues(typeof(ClassId))){
   var group=original.Where(a=>a.team==team&&a.characterClass==id).ToList();var template=group.FirstOrDefault()??original.First(a=>a.characterClass==id);
   int wanted=team==CPTeam.RED&&id==ClassId.Scout?1:2;while(group.Count>wanted){var extra=group[group.Count-1];group.Remove(extra);original.Remove(extra);UnityEngine.Object.DestroyImmediate(extra.gameObject);}
   while(group.Count<wanted){
    var parent=original.First(a=>a.team==team).transform.parent;
    var go=UnityEngine.Object.Instantiate(template.gameObject,parent);go.name=team+"_"+id+"_Bot_"+(group.Count+1);var actor=go.GetComponent<CPActor>();actor.team=team;actor.isPlayer=false;actor.characterClass=id;
    var identity=go.GetComponent<StaticBotIdentity>();identity.team=team.ToString();identity.characterClass=id;
    if(id==ClassId.Heavy&&team==CPTeam.RED){
     const string path="Assets/Bots/Materials/Heavy_RED.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
     if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Bots/Materials/Heavy_BLU.mat"));material.name="Heavy_RED";material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Bots/Textures/Heavy_Body.png"));AssetDatabase.CreateAsset(material,path);}
     go.GetComponentsInChildren<Renderer>().First(r=>r.name=="Heavy_Body").sharedMaterial=material;
    }
    group.Add(actor);original.Add(actor);created++;
   }
  }
  foreach(var team in new[]{CPTeam.RED,CPTeam.BLU}){
   var bots=original.Where(a=>a.team==team).OrderBy(a=>(int)a.characterClass).ThenBy(a=>a.name).ToArray();
   if(bots.Length!=(team==CPTeam.RED?7:8))throw new Exception("Unexpected bot count for "+team);
   for(int i=0;i<bots.Length;i++){
    float sign=team==CPTeam.RED?1:-1;bots[i].transform.position=new Vector3(sign*(i<4?128.4f:132.4f),4.71f,3.5f+(i%4)*4);
    bots[i].transform.rotation=Quaternion.Euler(0,team==CPTeam.RED?270:90,0);
    var anim=bots[i].GetComponent<CPBrain>().animations;anim["Idle"].clip.SampleAnimation(anim.gameObject,0);anim.playAutomatically=true;
   }
  }
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());return "Created "+created+" bots. RED:8; BLU:8; two per class, plus the RED player.";
 }
 public static string BakeNavigation(){
  EditOnly();var root=GameObject.Find("Powerhouse");var sources=new List<NavMeshBuildSource>();
  NavMeshBuilder.CollectSources(root.transform,1,NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);
  var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.4f;settings.agentHeight=1.9f;settings.agentClimb=.38f;settings.agentSlope=48;
  settings.overrideVoxelSize=true;settings.voxelSize=.10f;settings.overrideTileSize=true;settings.tileSize=256;settings.minRegionArea=1;
  var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(0,16,-40),new Vector3(380,70,260)),Vector3.zero,Quaternion.identity);
  if(data==null)throw new Exception("Navigation bake failed.");
  string path="Assets/ControlPoints/Navigation/PowerhouseExpandedNavigation.asset";var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
  if(existing==null)AssetDatabase.CreateAsset(data,path);else{EditorUtility.CopySerialized(data,existing);UnityEngine.Object.DestroyImmediate(data);data=existing;}
  var match=UnityEngine.Object.FindFirstObjectByType<CPMatch>();match.navigation=data;
  var nav=NavMesh.AddNavMeshData(data);int segments=0;var errors=new List<string>();
  try{
   foreach(var route in match.alternativeRoutes){
    route.length=0;
    for(int i=0;i<route.points.Length;i++){
     NavMeshHit hit;if(NavMesh.SamplePosition(route.points[i],out hit,1.3f,-1))route.points[i]=hit.position;else errors.Add(route.name+" missing node "+i);
     if(i==0)continue;var p=new NavMeshPath();if(!NavMesh.CalculatePath(route.points[i-1],route.points[i],-1,p)||p.status!=NavMeshPathStatus.PathComplete){errors.Add(route.name+" disconnected segment "+i);continue;}
     for(int k=1;k<p.corners.Length;k++)route.length+=Vector3.Distance(p.corners[k-1],p.corners[k]);segments++;
    }
   }
   var nodes=new List<Vector3>(match.strategicNodes??Array.Empty<Vector3>());foreach(var route in match.alternativeRoutes)foreach(var p in route.points)if(nodes.All(q=>(p-q).sqrMagnitude>4))nodes.Add(p);match.strategicNodes=nodes.ToArray();
   foreach(var actor in UnityEngine.Object.FindObjectsByType<CPActor>(FindObjectsSortMode.None).Where(a=>!a.isPlayer)){NavMeshHit hit;if(NavMesh.SamplePosition(actor.transform.position,out hit,1,-1))actor.transform.position=hit.position;else errors.Add(actor.name+" invalid spawn");}
  }finally{nav.Remove();}
  EditorUtility.SetDirty(match);EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
  return "Baked "+sources.Count+" collider sources; "+segments+" connected route segments. "+(errors.Count==0?"PASS":string.Join("; ",errors));
 }
 public static string ValidateExpansion(){
  EditOnly();var m=UnityEngine.Object.FindFirstObjectByType<CPMatch>();var nav=NavMesh.AddNavMeshData(m.navigation);var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Input+"ExpandedRoutes_Manifest.json"));var errors=new List<string>();int checkedPaths=0,doorChecks=0;
  try{
   foreach(var check in manifest.checks){
    NavMeshHit hit;if(!NavMesh.SamplePosition(check.p,out hit,1.3f,-1)){errors.Add(check.name+" no walkable surface");continue;}
    foreach(var zone in m.zones){var p=new NavMeshPath();if(!NavMesh.CalculatePath(hit.position,zone.position,-1,p)||p.status!=NavMeshPathStatus.PathComplete)errors.Add(check.name+" -> "+zone.label);else checkedPaths++;}
    if(check.name.Contains("Door")){Physics.SyncTransforms();if(Physics.CheckCapsule(hit.position+Vector3.up*.45f,hit.position+Vector3.up*1.5f,.34f,1,QueryTriggerInteraction.Ignore))errors.Add(check.name+" blocked capsule");else doorChecks++;}
   }
  }finally{nav.Remove();}
  if(errors.Count>0)return "FAIL: "+string.Join("; ",errors);
  return checkedPaths+" new-area to control-point paths; "+doorChecks+" clear exterior door capsules. PASS.";
 }
 public static string Save(){
  EditOnly();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());return "Saved Powerhouse expanded routes.";
 }
}
}
