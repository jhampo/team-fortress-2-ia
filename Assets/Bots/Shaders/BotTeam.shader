Shader "TF2/BotTeam" {
 Properties {
  _BaseMap("Original character texture",2D)="white"{}
  _TeamBlue("BLU clothing",Range(0,1))=0
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;float _TeamBlue;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;};
   V vert(A i){V o;VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);return o;}
   half4 frag(V i):SV_Target {
    half3 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
    // Work in perceptual RGB: saturated red fabric, not skin, gold or dark equipment.
    float3 srgb=LinearToSRGB(c);
    float mask=smoothstep(.28,.48,(srgb.r-max(srgb.g,srgb.b))/max(.001,srgb.r));
    float3 blue=float3(srgb.b*.80,srgb.r*.72,srgb.r*.95);
    c=SRGBToLinear(lerp(srgb,blue,mask*_TeamBlue));
    half3 n=normalize(i.normalWS);Light main=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    half3 light=max(SampleSH(n),half3(.17,.17,.17));
    light+=main.color*saturate(dot(n,main.direction))*main.shadowAttenuation*main.distanceAttenuation;
    #ifdef _ADDITIONAL_LIGHTS
    uint count=GetAdditionalLightsCount();for(uint j=0;j<count;j++){Light l=GetAdditionalLight(j,i.positionWS);light+=l.color*saturate(dot(n,l.direction))*l.distanceAttenuation*l.shadowAttenuation;}
    #endif
    return half4(c*light,1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
