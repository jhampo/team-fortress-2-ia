using UnityEngine;
namespace FpsStage2 {
// Camera-centred compression preserves the projected pose without moving the body.
[DefaultExecutionOrder(100)]
public sealed class Stage2WeaponCollision : MonoBehaviour {
 public LayerMask environmentMask=1;
 public float clearance=.035f;
 FpsStage2Player player;Transform[] pivots;Renderer[][] renderers;float[] scales;
 readonly RaycastHit[] hits=new RaycastHit[32];
 public float CurrentScale=>scales==null?1:scales[(int)player.currentClass];
 public void Initialize(FpsStage2Player owner){
  player=owner;pivots=new Transform[owner.views.Length];renderers=new Renderer[pivots.Length][];scales=new float[pivots.Length];
  for(int i=0;i<pivots.Length;i++){
   var view=owner.views[i].animation.transform;
   var pivot=new GameObject("ViewmodelWallProtection_"+i).transform;
   pivot.SetParent(owner.playerCamera.transform,false);view.SetParent(pivot,true);
   pivots[i]=pivot;scales[i]=1;
   renderers[i]=System.Array.FindAll(view.GetComponentsInChildren<Renderer>(true),r=>r.name.Contains("_Weapon_"));
  }
 }
 public void ResolveWalls(){
  if(player==null||pivots==null)return;
  int index=(int)player.currentClass;Vector3 origin=player.playerCamera.transform.position;
  float target=1,previous=Mathf.Max(.001f,scales[index]);
  if(CPWebPerformance.Active){
   // Conservative broad phase encloses every uncompressed weapon corner.
   // Away from walls this replaces dozens of triangle-mesh sphere casts with one test.
   float reach=clearance;
   foreach(var r in renderers[index])if(r!=null&&r.enabled&&r.gameObject.activeInHierarchy){var b=r.bounds;reach=Mathf.Max(reach,((b.center-origin).magnitude+b.extents.magnitude)/previous+clearance);}
   if(!Physics.CheckSphere(origin,reach,environmentMask,QueryTriggerInteraction.Ignore)){
    scales[index]=Mathf.MoveTowards(previous,1,Time.deltaTime*3);pivots[index].localScale=Vector3.one*scales[index];return;
   }
  }
  foreach(var renderer in renderers[index]){
   if(renderer==null||!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;
   Bounds b=renderer.bounds;
   for(int corner=-1;corner<8;corner++){
    Vector3 point=corner<0?b.center:b.center+Vector3.Scale(b.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
    Vector3 delta=(point-origin)/previous;float distance=delta.magnitude;if(distance<.001f)continue;
    int count=Physics.SphereCastNonAlloc(origin,clearance,delta/distance,hits,distance,environmentMask,QueryTriggerInteraction.Ignore);
    for(int h=0;h<count;h++){
     if(hits[h].collider==null||hits[h].collider.transform.IsChildOf(transform))continue;
     target=Mathf.Min(target,Mathf.Max(.01f,(hits[h].distance-clearance)/distance));
    }
   }
  }
  // Immediate protection on approach, smooth return, never displace the player.
  scales[index]=target<previous?target:Mathf.MoveTowards(previous,target,Time.deltaTime*3);
  pivots[index].localScale=Vector3.one*scales[index];
 }
 void LateUpdate(){ResolveWalls();}
}
}
