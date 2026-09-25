using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class CPGraphicsProfiles {
 public static string Configure(){
  const string root="Assets/ControlPoints/Resources/";
  const string rendererPath=root+"CPEconomyRenderer.asset",pipelinePath=root+"CPEconomyPipeline.asset";
  if(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath)==null)AssetDatabase.CopyAsset("Assets/Settings/Mobile_Renderer.asset",rendererPath);
  var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);var r=new SerializedObject(renderer);
  r.FindProperty("m_RenderingMode").intValue=0;r.FindProperty("m_DepthPrimingMode").intValue=0;
  r.FindProperty("m_IntermediateTextureMode").intValue=1;r.FindProperty("m_RendererFeatures").ClearArray();r.ApplyModifiedPropertiesWithoutUndo();
  if(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath)==null)AssetDatabase.CopyAsset("Assets/Settings/Mobile_RPAsset.asset",pipelinePath);
  var asset=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);var p=new SerializedObject(asset);
  p.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;
  foreach(var key in new[]{"m_RequireDepthTexture","m_RequireOpaqueTexture","m_SupportsHDR","m_MainLightShadowsSupported","m_AdditionalLightShadowsSupported","m_SoftShadowsSupported","m_ReflectionProbeBlending","m_ReflectionProbeBoxProjection","m_ReflectionProbeAtlas"}){var field=p.FindProperty(key);if(field!=null)field.boolValue=false;}
  p.FindProperty("m_GPUResidentDrawerMode").intValue=0;p.FindProperty("m_UpscalingFilter").intValue=1;
  p.FindProperty("m_RenderScale").floatValue=.85f;p.FindProperty("m_MSAA").intValue=1;
  p.FindProperty("m_ShadowDistance").floatValue=0;p.FindProperty("m_AdditionalLightsPerObjectLimit").intValue=2;
  p.FindProperty("m_UseSRPBatcher").boolValue=true;p.ApplyModifiedPropertiesWithoutUndo();
  EditorUtility.SetDirty(renderer);EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();return "Economy Forward renderer configured, no SSAO/deferred/GPU drawer or shadow maps.";
 }
}
