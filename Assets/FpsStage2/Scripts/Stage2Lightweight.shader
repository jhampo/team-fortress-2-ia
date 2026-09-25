Shader "Stage2/LightweightFire" {
 Properties { _MainTex("Shape",2D)="white"{} _Tint("Tint",Color)=(1,0.5,0.1,1) }
 SubShader { Tags {"RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Pass { Blend SrcAlpha One ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _Tint;
   CBUFFER_END
   Varyings vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
   half4 frag(Varyings i):SV_Target{half a=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;half3 hot=lerp(_Tint.rgb,half3(1.8,1.65,1.2),smoothstep(.50,.90,a));return half4(hot,a*_Tint.a);}
   ENDHLSL
  }
 }
}
