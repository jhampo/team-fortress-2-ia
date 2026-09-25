using System;
using UnityEngine;
using UnityEditor.SceneManagement;
using FpsStage2;
public static class CPAirblastTests {
 public static string Run(){int checks=0;Action<bool,string> check=(ok,msg)=>{checks++;if(!ok)throw new Exception(msg);};
  var c=new WeaponClock(ClassId.Pyro);check(c.TryAirblast(1)&&c.Ammo==195,"20 ammo");check(!c.TryAirblast(1.74)&&c.Ammo==195,"Cooldown");check(c.TryAirblast(1.75),"Cooldown ends");c.Ammo=19;check(!c.TryAirblast(3),"Insufficient ammo");c.Ammo=20;check(c.TryAirblast(3)&&c.Ammo==0,"Last 20");check(!c.TryAirblast(4),"Empty");check(!new WeaponClock(ClassId.Scout).TryAirblast(1),"Pyro only");
  check(CPCombat.InAirblast(Vector3.zero,Vector3.forward,Vector3.forward*3),"Front");check(!CPCombat.InAirblast(Vector3.zero,Vector3.forward,Vector3.back*2),"Behind");check(!CPCombat.InAirblast(Vector3.zero,Vector3.forward,Vector3.forward*5),"Range");
  var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("AirblastTest");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
  try {var fx=root.AddComponent<Stage2Effects>();var source=root.AddComponent<CPActor>();source.team=CPTeam.RED;
   var other=new GameObject("Enemy");other.transform.SetParent(root.transform);var enemy=other.AddComponent<CPActor>();enemy.team=CPTeam.BLU;
   var field=typeof(Stage2Effects).GetField("rockets",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var slots=(Array)field.GetValue(fx);var type=slots.GetType().GetElementType();var slot=slots.GetValue(0);
   var rocket=new GameObject("Rocket");rocket.transform.SetParent(root.transform);Vector3 origin=Vector3.up*10000;rocket.transform.position=origin+Vector3.forward*2;
   type.GetField("tr").SetValue(slot,rocket.transform);type.GetField("life").SetValue(slot,3f);type.GetField("velocity").SetValue(slot,Vector3.back*42);type.GetField("shooter").SetValue(slot,enemy);slots.SetValue(slot,0);
   check(fx.DeflectRockets(source,origin,Vector3.forward)==1,"Reflect incoming");slot=slots.GetValue(0);check((Vector3)type.GetField("velocity").GetValue(slot)==Vector3.forward*42,"Reverse trajectory and retain speed");check(type.GetField("shooter").GetValue(slot)==source,"Credit Pyro");check(fx.DeflectRockets(source,origin,Vector3.forward)==0,"Do not reflect friendly rocket");
  }finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
  return checks+" airblast checks passed";
 }
}
