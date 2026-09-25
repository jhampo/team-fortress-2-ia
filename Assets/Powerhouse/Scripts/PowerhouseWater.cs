using UnityEngine;
public sealed class PowerhouseWater : MonoBehaviour {
 public Vector2 speed=new Vector2(.025f,.045f);
 Renderer target;MaterialPropertyBlock block;float next,smoothness;
 void Awake(){target=GetComponent<Renderer>();block=new MaterialPropertyBlock();if(target!=null&&target.sharedMaterial!=null&&target.sharedMaterial.HasProperty("_Smoothness"))smoothness=target.sharedMaterial.GetFloat("_Smoothness");}
 void Update(){if(target==null)return;var q=FpsStage2.CPQualityEffects.Current;if(Time.time<next)return;next=Time.time+(q==FpsStage2.CPGraphicsQuality.Low?.5f:q==FpsStage2.CPGraphicsQuality.Medium?.125f:0);target.GetPropertyBlock(block);var offset=speed*(q==FpsStage2.CPGraphicsQuality.Low?0:Time.time);block.SetVector("_BaseMap_ST",new Vector4(1,1,offset.x,offset.y));block.SetFloat("_Smoothness",q==FpsStage2.CPGraphicsQuality.Ultra?smoothness:.02f);target.SetPropertyBlock(block);}
}
