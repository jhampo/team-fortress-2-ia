using UnityEngine;
using UnityEngine.Rendering;
namespace FpsStage2 {
// Cheap contact silhouettes replace realtime shadow maps on Low/Medium.
public sealed class CPBlobShadows:MonoBehaviour {
 CPActor[] actors;Transform[] discs;Material material;Texture2D texture;Mesh mesh;float next;
 void Start(){actors=CPMatch.Instance.actors;discs=new Transform[actors.Length];texture=new Texture2D(32,32,TextureFormat.RGBA32,false);var pixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){float dx=(x-15.5f)/15.5f,dy=(y-15.5f)/15.5f;pixels[y*32+x]=new Color(0,0,0,Mathf.Clamp01(1-dx*dx-dy*dy)*.28f);}texture.SetPixels(pixels);texture.Apply();
  material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
  mesh=new Mesh();mesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
  for(int i=0;i<actors.Length;i++){var g=new GameObject("Simple_ContactShadow");g.transform.SetParent(transform,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;discs[i]=g.transform;g.SetActive(false);}
 }
 void LateUpdate(){if(actors==null||Time.unscaledTime<next)return;next=Time.unscaledTime+.05f;for(int i=0;i<actors.Length;i++){var a=actors[i];bool show=CPQualityEffects.Current!=CPGraphicsQuality.Ultra&&a!=null&&a.Alive&&!a.isPlayer;RaycastHit hit=default;if(show)show=Physics.Raycast(a.transform.position+Vector3.up*.4f,Vector3.down,out hit,3,1,QueryTriggerInteraction.Ignore);discs[i].gameObject.SetActive(show);if(!show)continue;discs[i].position=hit.point+hit.normal*.015f;discs[i].rotation=Quaternion.FromToRotation(Vector3.up,hit.normal);discs[i].localScale=new Vector3(a.characterClass==ClassId.Heavy?1.4f:1,1,.8f);}}
 void OnDestroy(){if(material!=null)Destroy(material);if(texture!=null)Destroy(texture);if(mesh!=null)Destroy(mesh);}
}
}
