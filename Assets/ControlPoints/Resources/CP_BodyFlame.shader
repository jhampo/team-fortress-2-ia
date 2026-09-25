Shader "Powerhouse/BodyFlame" {
 Properties {_MainTex("Flame wisps",2D)="white"{} _Opacity("Opacity",Range(0,1))=.5 _Intensity("Intensity",Range(0,1))=.65}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 Pass {Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
 struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;float distanceToCamera:TEXCOORD1;};
 float _Opacity,_Intensity;
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 V vert(A i){V o;float3 world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(world);o.distanceToCamera=distance(world,_WorldSpaceCameraPos);o.uv=i.uv;o.color=i.color;return o;}
 half4 frag(V i):SV_Target{float2 uv=i.uv;uv.x+=sin(uv.y*10-_Time.y*8)*.055*(uv.y);half shape=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).a;half core=pow(shape,2);half nearFade=smoothstep(.12,1.3,i.distanceToCamera);return half4(saturate((i.color.rgb+core*half3(.3,.16,.02))*_Intensity),i.color.a*shape*_Opacity*nearFade);}
 ENDHLSL
 }
 }
}
