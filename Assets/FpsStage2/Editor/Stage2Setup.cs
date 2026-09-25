using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Linq;
using System.IO;
namespace FpsStage2 {
public static class Stage2Setup {
 public const string Root="Assets/FpsStage2";
 public static string Configure() {
  foreach(string path in Directory.GetFiles(Root+"/Models","*.fbx")) {
   var importer=(ModelImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
   importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;importer.bakeAxisConversion=true;
   importer.animationCompression=ModelImporterAnimationCompression.Off;importer.materialImportMode=ModelImporterMaterialImportMode.None;
   string kind=Path.GetFileNameWithoutExtension(path).Split('_')[1];var clips=importer.defaultClipAnimations;
   foreach(var c in clips){c.name=kind;c.loopTime=kind=="Idle"||kind=="Giro";c.wrapMode=c.loopTime?WrapMode.Loop:WrapMode.ClampForever;}
   importer.clipAnimations=clips;importer.SaveAndReimport();
  }
  return "Ten FBX clips configured";
 }
 static Material Mat(string name,Color color,string texture=null,float metal=0) {
  string path=Root+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
  mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",.22f);
  if(texture!=null)mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+texture+".png"));
  return mat;
 }
 [MenuItem("Stage 2/Build Four-Class Test")]
 public static void Build() {
  foreach(string folder in new[]{"Scenes","Materials","Prefabs"})Directory.CreateDirectory(Root+"/"+folder);AssetDatabase.Refresh();
  var old=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(old.isDirty)EditorSceneManager.SaveScene(old,AssetDatabase.GenerateUniqueAssetPath(Root+"/Scenes/PreviousScene_Backup.unity"),true);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var root=new GameObject("FPS_Player_Stage2");root.layer=2;root.transform.position=new Vector3(0,.1f,0);
  var cc=root.AddComponent<CharacterController>();cc.height=1.8f;cc.center=Vector3.up*.9f;cc.radius=.3f;cc.skinWidth=.025f;cc.stepOffset=.25f;cc.minMoveDistance=0;
  var co=new GameObject("PlayerCamera");co.tag="MainCamera";co.transform.SetParent(root.transform,false);co.transform.localPosition=Vector3.up*1.65f;
  var camera=co.AddComponent<Camera>();co.AddComponent<AudioListener>();camera.fieldOfView=42.55f;camera.nearClipPlane=.01f;camera.farClipPlane=150;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.20f,.26f);
  var player=root.AddComponent<FpsStage2Player>();player.playerCamera=camera;player.views=new ClassView[4];player.currentClass=ClassId.Scout;
  var fx=new Material(Shader.Find("Stage2/LightweightFire"));AssetDatabase.CreateAsset(fx,AssetDatabase.GenerateUniqueAssetPath(Root+"/Materials/LightweightFire.mat"));player.fxMaterial=fx;
  var flame=new Material(Shader.Find("Stage2/FireParticles"));AssetDatabase.CreateAsset(flame,AssetDatabase.GenerateUniqueAssetPath(Root+"/Materials/FireParticles.mat"));player.flameMaterial=flame;
  for(int i=0;i<4;i++) {
   string name=((ClassId)i).ToString();var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+name+"_Idle.fbx");
   var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab);model.name=name+"_FirstPerson";model.transform.SetParent(co.transform,false);
   foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=2;
   var animation=model.GetComponent<Animation>()??model.AddComponent<Animation>();animation.cullingType=AnimationCullingType.AlwaysAnimate;animation.playAutomatically=true;
   var kinds=i==0||i==2?new[]{"Idle","Disparo","Recarga"}:i==3?new[]{"Idle","Giro","Bajar"}:new[]{"Idle"};
   foreach(var kind in kinds){var clip=AssetDatabase.LoadAllAssetsAtPath(Root+"/Models/"+name+"_"+kind+".fbx").OfType<AnimationClip>().First(x=>!x.name.StartsWith("__preview__"));animation.AddClip(clip,kind);if(kind=="Idle")animation.clip=clip;}
   var arms=Mat(name+"_Arms",Color.white,name+"_Arms");var gun=Mat(name+"_Weapon",Color.white,name+"_Weapon",.28f);
   // The open break-action exposes both sides of the Scout barrel shell.
   if(i==0){gun.SetFloat("_Cull",0);gun.doubleSidedGI=true;EditorUtility.SetDirty(gun);}
   var round=Mat("Round_Red",new Color(.4f,.045f,.016f));var brass=Mat("Round_Brass",new Color(.64f,.4f,.08f),null,.6f);
   var missile=Mat("ReloadMissile_Olive",new Color(.16f,.20f,.075f),null,.3f);
   foreach(var renderer in model.GetComponentsInChildren<Renderer>()){renderer.sharedMaterial=(renderer.name.Contains("Bearing")||renderer.name.Contains("Shroud")||renderer.name.Contains("Ring"))?Mat("Heavy_BearingSteel",new Color(.045f,.05f,.055f),null,.5f):renderer.name.Contains("Arms")?arms:renderer.name.Contains("ReloadMissile")?missile:renderer.name.Contains("_Round_")?(renderer.name.EndsWith("Cap")?brass:round):gun;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;if(renderer is SkinnedMeshRenderer sk){sk.updateWhenOffscreen=true;sk.localBounds=new Bounds(Vector3.zero,Vector3.one*6);}}
   if(i==0){var steel=Mat("Scout_BreechSteel",new Color(.08f,.09f,.10f),null,.5f);steel.SetFloat("_Cull",0);foreach(var sk in model.GetComponentsInChildren<SkinnedMeshRenderer>())if(sk.name.Contains("_Weapon_")&&sk.sharedMesh.subMeshCount>1)sk.sharedMaterials=new[]{gun,steel};}
   var muzzle=model.GetComponentsInChildren<Transform>().First(t=>t.name=="Muzzle");player.views[i]=new ClassView{id=(ClassId)i,animation=animation,muzzle=muzzle,health=player.maxHealth[i]};
   animation.Play("Idle");animation.Sample();model.SetActive(i==0);
  }
  var platform=GameObject.CreatePrimitive(PrimitiveType.Cube);platform.name="TestPlatform";platform.transform.position=new Vector3(0,-.2f,0);platform.transform.localScale=new Vector3(20,.4f,20);platform.GetComponent<Renderer>().sharedMaterial=Mat("Platform",new Color(.25f,.31f,.36f));
  // Thin markings make movement visible without introducing a map or enemies.
  var lineMat=Mat("FloorGrid",new Color(.40f,.46f,.50f));
  var grid=new GameObject("FloorGrid");
  for(int i=-8;i<=8;i+=2)for(int axis=0;axis<2;axis++){var line=GameObject.CreatePrimitive(PrimitiveType.Cube);line.name="GridMark";line.transform.SetParent(grid.transform);UnityEngine.Object.DestroyImmediate(line.GetComponent<Collider>());line.transform.position=new Vector3(axis==0?i:0,.002f,axis==1?i:0);line.transform.localScale=axis==0?new Vector3(.018f,.003f,20):new Vector3(20,.003f,.018f);line.GetComponent<Renderer>().sharedMaterial=lineMat;}
  var lo=new GameObject("KeyLight");var light=lo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;lo.transform.rotation=Quaternion.Euler(40,-30,0);
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.51f,.54f);
  PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/FourClassPlayer.prefab");EditorSceneManager.SaveScene(scene,Root+"/Scenes/FourClass_Test.unity");AssetDatabase.SaveAssets();Selection.activeGameObject=root;
 }
 public static string TestClocks() {
  int checks=0;void Check(bool test,string message){checks++;if(!test)throw new Exception(message);}
  var s=new WeaponClock(ClassId.Scout);Check(s.Step(0,true,false,false)==1&&s.Ammo==1,"Scout first");Check(s.Step(.31249,true,false,false)==0,"Scout early gate");Check(s.Step(.3125,true,false,false)==1&&s.Ammo==0,"Scout second");s.Step(.625,false,false,false);Check(s.Reloading&&Math.Abs(s.Deadline-2.0583)<.00001,"Auto reload after second recovery");s.Step(2.05829,true,false,false);Check(s.Ammo==0,"Scout no early reload");s.Step(2.0583,false,false,false);Check(s.Ammo==2&&s.Reserve==30&&!s.Reloading,"Scout reload 1.4333");
  var soldier=new WeaponClock(ClassId.Soldier);soldier.Ammo=1;Check(soldier.Reload(0),"Soldier reload start");soldier.Step(.9199,false,false,false);Check(soldier.Ammo==1,"Soldier first gate");soldier.Step(.92,false,false,false);Check(soldier.Ammo==2&&Math.Abs(soldier.Deadline-1.72)<.00001,"Soldier first .92");soldier.Step(1.72,false,false,false);Check(soldier.Ammo==3,"Soldier next .8");soldier.Step(2.52,false,false,false);Check(soldier.Ammo==4&&!soldier.Reloading,"Soldier full");
  var auto=new WeaponClock(ClassId.Soldier);auto.Step(0,true,false,false);auto.Step(.8,false,false,false);Check(auto.Reloading&&auto.Ammo==3,"Soldier auto reload after one shot");auto.Step(1.72,false,false,false);Check(auto.Ammo==4&&auto.Reserve==19&&!auto.Reloading,"Soldier fills single missing rocket");
  var interrupt=new WeaponClock(ClassId.Soldier);interrupt.Step(0,true,false,false);interrupt.Step(.8,false,false,false);Check(interrupt.Step(1,true,false,false)==1&&!interrupt.Reloading&&interrupt.Ammo==2,"Soldier interrupts before insertion");interrupt.Step(interrupt.NextShot,false,false,false);Check(interrupt.Reloading,"Soldier resumes after interrupted shot");double done=interrupt.Deadline;interrupt.Step(done,false,false,false);Check(interrupt.Ammo==3&&interrupt.Reloading,"Soldier inserts first pending rocket");interrupt.Step(done+.8,false,false,false);Check(interrupt.Ammo==4&&interrupt.Reserve==18&&interrupt.Reloads==2,"Soldier inserts exactly two pending rockets");
  var midway=new WeaponClock(ClassId.Soldier);midway.Ammo=1;midway.Reload(0);midway.Step(.92,false,false,false);midway.Step(1,true,false,false);Check(midway.Ammo==1&&midway.Reserve==19,"Completed insertion survives interruption");midway.Step(midway.NextShot,false,false,false);midway.Step(midway.Deadline+1.6,false,false,false);Check(midway.Ammo==4&&midway.Reserve==16,"All remaining rockets after second interruption");
  var limited=new WeaponClock(ClassId.Soldier);limited.Ammo=0;limited.Reserve=2;limited.Step(0,false,false,false);limited.Step(1.72,false,false,false);Check(limited.Ammo==2&&limited.Reserve==0&&!limited.Reloading,"No invented reserve rockets");
  var h=new WeaponClock(ClassId.Heavy);Check(h.Step(0,true,false,false)==0&&h.State=="Bajando","Heavy left alone lowers");Check(h.Step(1.09999,true,false,false)==0,"Heavy cold fire 1.10 gate");Check(h.Step(1.1,true,false,false)==1,"Heavy fires without right at 1.10");Check(h.Step(1.20499,true,false,false)==0,"Heavy interval gate");Check(h.Step(1.205,true,false,false)==1,"Heavy .105 interval");h.Step(1.3,false,false,false);Check(h.State=="Frenando"&&!h.Spinning,"Heavy release lowers and stops shots");h.Step(2.4,false,false,false);Check(h.State=="Idle","Heavy lower animation finishes");
  var held=new WeaponClock(ClassId.Heavy);held.Step(0,false,true,false);Check(held.State=="Acelerando","Heavy right spin starts");Check(held.Step(.86999,true,true,false)==0,"Heavy right spin .87 gate");Check(held.Step(.87,true,true,false)==1,"Heavy ready spin fires immediately");Check(held.Step(.975,true,false,false)==1&&held.Spinning,"Right release while left held keeps firing");Check(held.Step(1.08,true,false,false)==1&&held.Spinning,"Left alone sustains spin");held.Step(1.12,false,true,false);Check(held.Spinning&&held.State=="Girando","Right alone maintains motor");held.Step(1.2,false,false,false);Check(!held.Spinning&&held.State=="Frenando","Both released stop motor");
  var cancel=new WeaponClock(ClassId.Heavy);cancel.Step(0,true,false,false);cancel.Step(.4,false,false,false);cancel.Step(1.1,false,false,false);Check(cancel.Shots==0,"Early left release never fires");cancel.Step(2,true,true,false);Check(Math.Abs(cancel.Deadline-3.1)<.00001,"Both buttons from cold prepare 1.10");cancel.Cancel();Check(!cancel.Spinning&&cancel.State=="Idle","Switch cancels motor");
  var p=new WeaponClock(ClassId.Pyro);Check(p.Step(0,true,false,false)==1&&p.Ammo==199,"Pyro starts");Check(p.Step(.07499,true,false,false)==0,"Pyro attack gate");Check(p.Step(.075,true,false,false)==1&&p.Ammo==199,"Pyro .075 separate consumption");p.Step(.08,true,false,false);Check(p.Ammo==198,"Pyro .08 ammo");p.Cancel();Check(p.Ammo==198,"Switch preserves ammo");
  var fast=new WeaponClock(ClassId.Soldier);Check(fast.Step(0,true,false,false)==1,"Soldier first shot");Check(fast.Step(.79999,true,false,false)==0,"Soldier cannot shoot before .8");Check(fast.Step(.8,true,false,false)==1,"Soldier shoots at .8");Check(fast.Step(1.59999,true,false,false)==0,"Soldier second gate");Check(fast.Step(1.6,true,false,false)==1,"Soldier repeat .8");
  var late=new WeaponClock(ClassId.Soldier);late.Step(0,true,false,false);late.Step(.93,true,false,false);Check(late.Step(1.72999,true,false,false)==0,"Soldier cooldown uses actual last shot");Check(late.Step(1.73,true,false,false)==1,"Soldier delayed shot plus .8");
  var interruptedFast=new WeaponClock(ClassId.Soldier);interruptedFast.Step(0,true,false,false);interruptedFast.Step(.8,false,false,false);Check(interruptedFast.Reloading,"Soldier auto reload at cooldown");Check(interruptedFast.Step(.8,true,false,false)==1&&!interruptedFast.Reloading,"Soldier interrupts reload at .8");interruptedFast.Step(1.6,false,false,false);Check(interruptedFast.Reloading,"Soldier resumes pending reload");interruptedFast.Step(2.52,false,false,false);interruptedFast.Step(3.32,false,false,false);Check(interruptedFast.Ammo==4&&interruptedFast.Reserve==18,"Soldier reloads both pending rockets");
  return "PASS: "+checks+" deterministic timing and ammunition checks";
 }
}
}


