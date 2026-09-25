Shader "Stage2/ContinuousFlame" {
 Properties { _Intensity("Intensity",Float)=1.0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"} 
  Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
   CBUFFER_START(UnityPerMaterial)
    float _Intensity;
   CBUFFER_END
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
   float fbm(float2 p){return noise(p)*.57+noise(p*2.03+17)*.28+noise(p*4.07+29)*.15;}
   Varyings vert(Attributes v){Varyings o;float3 p=v.positionOS.xyz;float t=v.uv.y;p.x+=sin(t*12-_Time.y*16)*t*.055;p.y+=sin(t*17-_Time.y*13)*t*.035;o.positionCS=TransformObjectToHClip(p);o.uv=v.uv;return o;}
   half4 frag(Varyings i):SV_Target {
    float y=i.uv.y,x=i.uv.x*2-1;
    float n=fbm(float2(x*2.4,y*7-_Time.y*6));
    float warp=(noise(float2(y*9-_Time.y*4,2.7))-.5)*y*.7;
    float edge=1-abs(x+warp);
    float density=edge*.9+n*.62-y*.65-.24;
    float alpha=smoothstep(.02,.48,density)*(1-smoothstep(.68,1,y))*.86;
    float heat=saturate(density*1.1+(1-y)*.06);
    float3 color=lerp(float3(.75,.035,.002),float3(1.4,.28,.012),smoothstep(.08,.52,heat));
    color=lerp(color,float3(1.65,1.12,.23),smoothstep(.53,.9,heat));
    color=lerp(color,float3(.12,.42,1.4),saturate(1-y*17)*.8);
    return half4(color*_Intensity,alpha);
   }
   ENDHLSL
  }
 }
}
