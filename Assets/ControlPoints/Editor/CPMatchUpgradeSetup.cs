using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace FpsStage2 {
public static class CPMatchUpgradeSetup {
 public static string Apply(){if(Application.isPlaying)throw new Exception("Stop Play first");var match=UnityEngine.Object.FindFirstObjectByType<CPMatch>();var map=GameObject.Find("Powerhouse");
  // Derive the centre of each existing yellow hatch outline, rather than guessing a room offset.
  foreach(int index in new[]{0,2}){float sign=index==0?1:-1;var points=map.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith(index==0?"PH_12_":"PH_11_")&&f.name.Contains("Safety_Ochre")).SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).Where(v=>v.x*sign>85&&v.x*sign<99&&v.y>5&&v.y<5.2f).ToArray();if(points.Length==0)throw new Exception("No hatch outline found");var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);var z=match.zones[index];z.position=new Vector3(b.center.x,5.17f,b.center.z);z.visual.position=z.position;z.visual.localScale=new Vector3(1,1,1.25f);}
  // Thick continuous volumes for the four chain-link fences; central gate gaps remain open.
  var boundary=map.transform.Find("Gameplay_Fence_Collisions");if(boundary==null){boundary=new GameObject("Gameplay_Fence_Collisions").transform;boundary.SetParent(map.transform,false);}
  foreach(float sign in new[]{-1f,1f}){Box(boundary,"Outer_Mesh_"+sign,new Vector3(sign*156,4.2f,-3.66f),new Vector3(.17f,2.4f,68.32f));Box(boundary,"Courtyard_Mesh_"+sign,new Vector3(sign*14.64f,4.2f,-35.38f),new Vector3(19.52f,2.4f,.17f));}
  int added=0;foreach(var f in map.GetComponentsInChildren<MeshFilter>()){bool rail=f.name.Contains("Graphite_Steel")&&f.name.EndsWith("Visual")&&(f.name.StartsWith("PH_06_")||f.name.StartsWith("PH_11_")||f.name.StartsWith("PH_12_")||f.name.StartsWith("PH_15_")||f.name.StartsWith("PH_16_")||f.name.StartsWith("PH_20_")||f.name.StartsWith("PH_21_"));if(rail&&f.GetComponent<Collider>()==null){f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;added++;}}
  var doors=GameObject.Find("Match_Base_Doors");if(doors==null)doors=new GameObject("Match_Base_Doors");match.spawnGates=new CPSpawnGate[2];match.teamBases=new Bounds[2];
  var metal=Mat("GateMetal",new Color(.23f,.26f,.26f));var dark=Mat("GateTrim",new Color(.06f,.065f,.065f));var cream=Mat("GateLettering",new Color(.9f,.85f,.69f));
  for(int i=0;i<2;i++){float sign=i==0?1:-1;string team=i==0?"RED":"BLU";var existing=doors.transform.Find(team);if(existing!=null){var oldGate=existing.GetComponent<CPSpawnGate>();if(oldGate!=null)UnityEngine.Object.DestroyImmediate(oldGate);var gate=existing.gameObject.AddComponent<CPSpawnGate>();gate.shutter=existing.Find("Shutter");gate.shutter.localPosition=Vector3.zero;gate.barrier=gate.shutter.Find("Closed_Door").GetComponent<Collider>();gate.barrier.enabled=true;match.spawnGates[i]=gate;}else{
    var root=new GameObject(team);root.transform.SetParent(doors.transform,false);root.transform.position=new Vector3(sign*120.4f,4.7f,9.66f);var gate=root.AddComponent<CPSpawnGate>();match.spawnGates[i]=gate;
    var panel=new GameObject("Shutter").transform;panel.SetParent(root.transform,false);gate.shutter=panel;
    var plate=Cube(panel,"Closed_Door",new Vector3(0,2.25f,0),new Vector3(.25f,4.5f,6.25f),metal);gate.barrier=plate.GetComponent<Collider>();
    for(int slat=0;slat<14;slat++)Cube(panel,"Rib_"+slat,new Vector3(0,.16f+slat*.32f,0),new Vector3(.32f,.035f,6.25f),dark,false);
    var stripe=Mat("Gate_"+team,CPMatch.TeamColor(i==0?CPTeam.RED:CPTeam.BLU));Cube(panel,"TeamStripe",new Vector3(0,2.3f,0),new Vector3(.34f,.95f,6.22f),stripe,false);
   }
   // One inward-facing label: the legacy font renders both faces, so opposing labels overlap through the shutter.
   var shutter=match.spawnGates[i].shutter;foreach(var oldLabel in shutter.GetComponentsInChildren<TextMesh>().Where(t=>t.name=="Base_Name").ToArray())UnityEngine.Object.DestroyImmediate(oldLabel.gameObject);
   var text=new GameObject("Base_Name");text.transform.SetParent(shutter,false);text.transform.localPosition=new Vector3(sign*.185f,2.3f,0);text.transform.localRotation=Quaternion.Euler(0,-sign*90,0);var label=text.AddComponent<TextMesh>();label.text=team+"  /  BASE";label.fontSize=64;label.characterSize=.065f;label.anchor=TextAnchor.MiddleCenter;label.color=cream.color;
   match.teamBases[i]=new Bounds(new Vector3(sign*129.4f,11.7f,9.66f),new Vector3(17.6f,14,27.0f));
  }
  var gateScript=AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/ControlPoints/Scripts/CPSpawnGate.cs");foreach(var gate in match.spawnGates){var serialized=new SerializedObject(gate);serialized.FindProperty("m_Script").objectReferenceValue=gateScript;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(gate);}
  EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();return "Centred both hatch rings; 4 mesh-fence barriers, "+added+" additional railing colliders, two spawn gates and base volumes.";
 }
 static void Box(Transform parent,string name,Vector3 p,Vector3 size){var old=parent.Find(name);var go=old!=null?old.gameObject:new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=p;var c=go.GetComponent<BoxCollider>();if(c==null)c=go.AddComponent<BoxCollider>();c.size=size;go.isStatic=true;}
 static Material Mat(string name,Color c){string path="Assets/ControlPoints/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",.2f);AssetDatabase.CreateAsset(m,path);}return m;}
 static GameObject Cube(Transform parent,string name,Vector3 p,Vector3 size,Material material,bool collision=true){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;if(!collision)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
}
}
