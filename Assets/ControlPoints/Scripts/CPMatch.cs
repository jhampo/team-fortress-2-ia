using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace FpsStage2 {
[System.Serializable] public class CPZone {
 public string label;public Vector3 position;public float radius=3.5f,height=3;public Renderer[] glowRenderers;public Transform visual;
 public bool Contains(Vector3 p)=>Mathf.Abs(p.y-position.y)<height&&new Vector2(p.x-position.x,p.z-position.z).sqrMagnitude<=radius*radius;
}
[DefaultExecutionOrder(-200)] public sealed class CPMatch:MonoBehaviour {
 public static CPMatch Instance {get;private set;}
 public FpsStage2Player player;public CPZone[] zones;public NavMeshData navigation;
 public Vector3[] strategicNodes,healthStations;public Texture2D[] portraits;
 public CPAlternativeRoute[] alternativeRoutes=System.Array.Empty<CPAlternativeRoute>();
 public int RouteUsers(string route){if(actors==null)return 0;int n=0;foreach(var a in actors)if(!a.isPlayer&&a.Alive&&a.GetComponent<CPBrain>().ActiveRoute==route)n++;return n;}
 public float respawnBase=5;public float supplyCooldown=10;float[] supplyReady;public ControlPointRules rules;public CPActor[] actors;public CPDifficulty difficulty=CPDifficulty.Normal;
 [System.NonSerialized] CPPointAtmosphere[] pointAtmospheres;
 public CPSpawnGate[] spawnGates=System.Array.Empty<CPSpawnGate>();public Bounds[] teamBases=System.Array.Empty<Bounds>();
 public CPGameFlow flow;public CPAudio audioSystem;public CPScorekeeper scores;public bool RoundLive=>flow==null||flow.Live;
 public bool WeaponsAvailable=>!MenuOpen&&!rules.Finished&&(flow==null||flow.Phase!=CPRoundPhase.MainMenu);
 public bool MenuOpen {get;private set;} public bool InputBlocked=>MenuOpen||rules.Finished||(flow!=null&&(flow.Phase==CPRoundPhase.MainMenu||flow.ClassMenuOpen))||(PlayerActor!=null&&!PlayerActor.Alive);
 public CPActor PlayerActor {get;private set;} public CPCombat combat;public CPCombatFeedback feedback;float nextRoles;int[] red=new int[3],blue=new int[3];InputAction pauseAction;MaterialPropertyBlock block;NavMeshDataInstance navInstance;
 void Awake(){Instance=this;Application.runInBackground=true;Time.timeScale=1;difficulty=(CPDifficulty)Mathf.Clamp(PlayerPrefs.GetInt("PowerhouseDifficulty",1),0,3);rules=new ControlPointRules{BaseRespawn=respawnBase};block=new MaterialPropertyBlock();if(navigation!=null)navInstance=NavMesh.AddNavMeshData(navigation);pauseAction=new InputAction("PowerhousePause",InputActionType.Button,"<Keyboard>/escape");pauseAction.performed+=_=>ToggleMenu();pauseAction.Enable();}
 void Start(){supplyReady=new float[healthStations.Length];actors=FindObjectsByType<CPActor>(FindObjectsSortMode.None).OrderBy(a=>a.name).ToArray();PlayerActor=actors.First(a=>a.isPlayer);foreach(var a in actors)a.Initialize();scores=new CPScorekeeper(this);audioSystem=gameObject.AddComponent<CPAudio>();audioSystem.Initialize(this);combat=GetComponent<CPCombat>();combat.Initialize(this);foreach(var a in actors)if(!a.isPlayer)a.GetComponent<CPBrain>().Initialize(a,this);var hud=GetComponent<CPHud>();hud.Initialize(this);feedback=GetComponent<CPCombatFeedback>();if(feedback==null)feedback=gameObject.AddComponent<CPCombatFeedback>();feedback.Initialize(this,hud.RootCanvas);AssignRoles();flow=gameObject.AddComponent<CPGameFlow>();flow.Initialize(this);gameObject.AddComponent<CPMenus>().Initialize(this);UpdatePointVisuals();}
 void Update(){if(actors==null||MenuOpen)return;
  if(!RoundLive){if(flow!=null&&flow.Phase==CPRoundPhase.Preparation)foreach(var a in actors)a.Tick();UpdatePointVisuals();return;}foreach(var a in actors)a.Tick();System.Array.Clear(red,0,3);System.Array.Clear(blue,0,3);
  foreach(var a in actors)if(a.Alive)for(int i=0;i<3;i++)if(zones[i].Contains(a.transform.position)){if(a.team==CPTeam.RED)red[i]++;else blue[i]++;}
  rules.Tick(Time.deltaTime,red,blue);scores?.Captures();UpdatePointVisuals();
  foreach(var a in actors)if(a.Alive&&Vector3.Distance(a.transform.position,a.spawn)<3)a.HealAndSupply();
  if(rules.Finished){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;player.MatchStopWeapon();}
 }
 void UpdatePointVisuals(){if(pointAtmospheres==null||pointAtmospheres.Length!=zones.Length){pointAtmospheres=new CPPointAtmosphere[zones.Length];for(int j=0;j<zones.Length;j++){var go=new GameObject("Point_Atmosphere_"+j);go.transform.SetParent(transform,false);pointAtmospheres[j]=go.AddComponent<CPPointAtmosphere>();pointAtmospheres[j].Initialize(zones[j]);}}for(int i=0;i<3;i++){var s=rules.Points[i];pointAtmospheres[i].Present(s);Color c=TeamColor(s.Owner);if(s.Capturing!=CPTeam.Neutral)c=Color.Lerp(c,TeamColor(s.Capturing),s.Progress*(.7f+.3f*Mathf.Sin(Time.time*7)));foreach(var r in zones[i].glowRenderers){if(r==null)continue;block.Clear();block.SetColor("_BaseColor",c);block.SetColor("_EmissionColor",c*(s.Locked?1.8f:3.5f));r.SetPropertyBlock(block);}}}
 public static Color TeamColor(CPTeam team)=>team==CPTeam.RED?new Color(.72f,.12f,.075f):team==CPTeam.BLU?new Color(.16f,.46f,.65f):new Color(.70f,.65f,.45f);
 void AssignRoles(){foreach(var team in new[]{CPTeam.RED,CPTeam.BLU}){var bots=actors.Where(a=>!a.isPlayer&&a.team==team).OrderBy(a=>a.name).ToArray();int attack=Mathf.Max(1,Mathf.RoundToInt(bots.Length*.4f)),flank=Mathf.Max(1,Mathf.RoundToInt(bots.Length*.2f));for(int i=0;i<bots.Length;i++)bots[i].GetComponent<CPBrain>().role=i>=bots.Length-flank?CPRole.Flank:i<attack?CPRole.Attack:CPRole.Defend;}}
 public int CountNear(CPTeam team,Vector3 point,float radius){int count=0;foreach(var a in actors)if(a.Alive&&a.team==team&&(a.transform.position-point).sqrMagnitude<radius*radius)count++;return count;}
 public int Objective(CPActor actor,CPRole role){int threatened=-1;float closest=float.PositiveInfinity;for(int i=0;i<3;i++){var s=rules.Points[i];int enemies=actor.team==CPTeam.RED?s.BlueCount:s.RedCount;if(s.Owner==actor.team&&enemies>0){float d=(zones[i].position-actor.transform.position).sqrMagnitude;if(d<closest){closest=d;threatened=i;}}}if(threatened>=0)return threatened;
  if(rules.Points[1].Owner!=actor.team)return 1;if(role==CPRole.Defend)return 1;return actor.team==CPTeam.RED?2:0;}
 public Vector3 SafeStation(CPActor actor){Vector3 best=actor.spawn;float score=Vector3.Distance(actor.transform.position,best);foreach(var pickup in CPCollectible.All){if(pickup==null||!pickup.UsefulFor(actor)||actor.HealthFraction<.28f&&!pickup.IsHealth)continue;var p=pickup.transform.position;float d=Vector3.Distance(actor.transform.position,p);if(d<score&&CountNear(ControlPointRules.Enemy(actor.team),p,12)==0){NavMeshPath path=new NavMeshPath();if(NavMesh.CalculatePath(actor.transform.position,p,-1,path)&&path.status==NavMeshPathStatus.PathComplete){best=p;score=d;}}}return best;}
 public void ToggleMenu(){if(flow!=null){if(flow.Phase==CPRoundPhase.MainMenu)return;if(flow.ClassMenuOpen){flow.CloseClasses();return;}}MenuOpen=!MenuOpen;Time.timeScale=MenuOpen?0:1;player.MatchStopWeapon();Cursor.lockState=MenuOpen?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=MenuOpen;audioSystem?.Play(CPSound.Click,Vector3.zero,true);}
 public void Resume(){MenuOpen=false;Time.timeScale=1;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
 public void SelectDifficulty(CPDifficulty value){if(flow==null||flow.Phase!=CPRoundPhase.MainMenu)return;difficulty=(CPDifficulty)Mathf.Clamp((int)value,0,3);PlayerPrefs.SetInt("PowerhouseDifficulty",(int)difficulty);PlayerPrefs.Save();audioSystem?.Play(CPSound.Click,Vector3.zero,true);}
 public void ReturnToMainMenu(){CPGameFlow.SkipMenuOnce=false;Time.timeScale=1;AudioListener.pause=false;SceneManager.LoadScene(SceneManager.GetActiveScene().path);}
 public void Restart(CPDifficulty value){PlayerPrefs.SetInt("PowerhouseDifficulty",(int)value);PlayerPrefs.Save();CPGameFlow.SkipMenuOnce=true;Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().path);}
 void OnDestroy(){CPDeathPresentation.ClearCaches();pauseAction?.Dispose();if(navInstance.valid)navInstance.Remove();if(Instance==this)Instance=null;Time.timeScale=1;}
}
}
