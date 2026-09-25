using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FpsStage2;

public static class CPDamageNumberTests {
 public static string Run(){
  var scene=EditorSceneManager.NewPreviewScene();
  var root=new GameObject("DamageNumberTest");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
  try {
   var feedback=root.AddComponent<CPCombatFeedback>();
   var attacker=root.AddComponent<CPActor>();attacker.isPlayer=true;attacker.team=CPTeam.RED;
   var victimObject=new GameObject("Victim");victimObject.transform.SetParent(root.transform);
   var victim=victimObject.AddComponent<CPActor>();victim.team=CPTeam.BLU;
   var flags=BindingFlags.NonPublic|BindingFlags.Instance;
   var pool=(Array)typeof(CPCombatFeedback).GetField("numbers",flags).GetValue(feedback);
   var type=pool.GetType().GetElementType();
   Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);};
   for(int i=0;i<pool.Length;i++){
    var item=Activator.CreateInstance(type,true);var go=new GameObject("Number",typeof(RectTransform),typeof(Text));go.transform.SetParent(root.transform);
    type.GetField("root").SetValue(item,go.GetComponent<RectTransform>());type.GetField("text").SetValue(item,go.GetComponent<Text>());pool.SetValue(item,i);
   }
   feedback.ReportDamage(victim,attacker,10,Vector3.zero,false);
   feedback.ReportDamage(victim,attacker,7,Vector3.zero,false);
   var first=pool.GetValue(0);var label=(Text)type.GetField("text").GetValue(first);
   check(label.text=="-17","Simultaneous pellets must sum");
   check(label.fontStyle==FontStyle.Bold,"Numbers must be bold");
   float born=(float)type.GetField("born").GetValue(first),until=(float)type.GetField("until").GetValue(first);
   check(Mathf.Abs(until-born-1.5f)<.001f,"Lifetime must be 1.5 seconds");
   type.GetField("hitFrame").SetValue(first,-1);
   feedback.ReportDamage(victim,attacker,12,Vector3.zero,false);
   var second=pool.GetValue(1);
   check(label.text=="-17","Later hit must preserve original number");
   check(((Text)type.GetField("text").GetValue(second)).text=="-12","Later hit must get its own number");
   check((Vector2)type.GetField("offset").GetValue(first)!=(Vector2)type.GetField("offset").GetValue(second),"Hits need distinct stable offsets");
   check((float)type.GetField("until").GetValue(first)==until,"Later hits must not extend older labels");
   feedback.ReportDamage(victim,attacker,4,Vector3.zero,true);
   check(((Text)type.GetField("text").GetValue(pool.GetValue(2))).text=="-4","Afterburn must remain separate");
   feedback.Forget(victim);
   for(int i=0;i<pool.Length;i++)check(type.GetField("target").GetValue(pool.GetValue(i))==null,"Respawn must clear old numbers");
   return "PASS: pellets, bold, 1.5s lifetime, separate hits, stable offsets, no lifetime extension, afterburn, respawn cleanup";
  }finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
 }
}
