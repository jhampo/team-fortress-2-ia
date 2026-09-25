Shader "Stage2/FireParticles" {
 Properties { _MainTex("Particle wisps",2D)="white"{} _Opacity("Opacity",Range(0,1))=.5 _Intensity("Intensity",Range(0,1))=.8 }
 SubShader { Tags {"RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;float distanceToCamera:TEXCOORD1;};
   float _Opacity,_Intensity;
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   Varyings vert(Attributes v){Varyings o;float3 world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(world);o.distanceToCamera=distance(world,_WorldSpaceCameraPos);o.uv=v.uv;o.color=v.color;return o;}
   half4 frag(Varyings i):SV_Target {half a=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;half nearFade=smoothstep(.1,1.0,i.distanceToCamera);return half4(saturate(i.color.rgb*_Intensity),i.color.a*a*_Opacity*nearFade);}
   ENDHLSL
  }
 }
}
