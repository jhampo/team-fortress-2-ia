using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
public static class CPWindowsExport {
 public static string PrepareScene(){
  if(EditorApplication.isPlaying||BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Stop play/build first");
  var source=SceneManager.GetSceneByPath("Assets/Powerhouse/Scenes/Powerhouse.unity");
  if(!source.IsValid()||!source.isLoaded||source.isDirty)throw new InvalidOperationException("Open the saved original Powerhouse scene first");
  const string path="Assets/ControlPoints/Generated/Powerhouse_PC.unity";
  EditorSceneManager.SaveScene(source,path,true);var copy=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
  try{
   // CPMatch.Awake registers its NavMesh; CPActor.Initialize then activates every agent.
   foreach(var root in copy.GetRootGameObjects())foreach(var agent in root.GetComponentsInChildren<NavMeshAgent>(true))agent.enabled=false;
   var marker=new GameObject("PCBuildRevision_"+Guid.NewGuid().ToString("N"));marker.tag="EditorOnly";SceneManager.MoveGameObjectToScene(marker,copy);EditorSceneManager.SaveScene(copy);
  }finally{EditorSceneManager.CloseScene(copy,true);}
  return path;
 }
}
