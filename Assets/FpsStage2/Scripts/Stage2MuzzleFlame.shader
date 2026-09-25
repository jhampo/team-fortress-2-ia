Shader "Stage2/MuzzleFlame" {
 Properties { _Tint("Tint",Color)=(1,1,1,1) _Heavy("Heavy plume",Float)=0 _FlashSeed("Shot variation",Float)=0 _FlashAge("Flash age",Float)=0 }
 SubShader { Tags {"RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline"}
 Pass { Blend SrcAlpha One ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct V {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 CBUFFER_START(UnityPerMaterial)
 float4 _Tint;float _Heavy;float _FlashSeed;float _FlashAge;
 CBUFFER_END
 V vert(A i){V o;o.pos=TransformObjectToHClip(i.vertex.xyz);o.uv=i.uv;return o;}
 float hash21(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise21(float2 p){float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash21(cell),hash21(cell+float2(1,0)),f.x),lerp(hash21(cell+float2(0,1)),hash21(cell+1),f.x),f.y);}
 half4 heavyFlash(float2 uv){
  float2 p=(uv-.5)*2;
  p.x*=1.27;p.y=(p.y+.12)/1.20;
  float2 drift=float2(_FlashSeed*1.37,_FlashSeed*.73-_FlashAge*.65);
  float n=.64*noise21(p*6+drift)+.26*noise21(p*13+drift*1.8)+.10*noise21(p*27+drift);
  float a=atan2(p.y,p.x),r=length(p);
  // Uneven gas tongues around a compact white-hot core, not a solid disk.
  float edge=.56+.075*sin(a*5+_FlashSeed)+.045*sin(a*9-_FlashSeed*.7);
  edge+=.09*max(0,p.y)+.16*(n-.5);
  float body=smoothstep(-.055,.16,edge-r);
  float breakup=smoothstep(.15,.63,n+.25*(1-r));
  float heat=exp(-dot(p-float2(0,-.025),p-float2(0,-.025))*19);
  float wisps=smoothstep(-.11,.03,edge-r)*breakup;
  float3 col=lerp(float3(2.1,.22,.009),float3(3.5,1.65,.18),saturate(body*.8+n*.25));
  col=lerp(col,float3(4.0,3.65,2.55),smoothstep(.10,.72,heat));
  float glow=exp(-r*r*7.5)*.12;
  float alpha=saturate(body*(.55+.45*breakup)+wisps*.25+heat*.25+glow);
  alpha*=1-smoothstep(.86,1,max(abs((uv.x-.5)*2),abs((uv.y-.5)*2)));
  return half4(col,alpha*_Tint.a);
 }
 half4 frag(V i):SV_Target {
  if(_Heavy>.5)return heavyFlash(i.uv);
  float2 p=(i.uv-.5)*2;
  p.y=(p.y+.12)/(1+.20*_Heavy);p.x*=1+.3*_Heavy;
  float a=atan2(p.y,p.x),r=length(p);
  float edge=.58+.075*sin(a*5+1.2)+.045*sin(a*9-2)+.025*sin(a*17);
  edge+=_Heavy*.16*max(0,p.y)*(1+.6*sin(a*11));
  float density=saturate((edge-r)*4);
  float core=smoothstep(.55,1,density);
  float3 col=lerp(float3(2,.08,.001),float3(2,.65,.008),smoothstep(0,.65,density));
  col=lerp(col,float3(2.2,2,1.3),core);
  float glow=exp(-r*r*6)*.18;
  float alpha=saturate(density+glow)*_Tint.a;
  return half4(col,alpha);
 }
 ENDHLSL
 } }
}
