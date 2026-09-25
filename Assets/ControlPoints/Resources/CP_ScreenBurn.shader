Shader "Powerhouse/ScreenBurn" {
 Properties { [PerRendererData] _MainTex("Sprite Texture",2D)="white"{} _Opacity("Opacity",Range(0,1))=0 _BurnTime("Time",Float)=0 _Aspect("Aspect",Float)=1.777 _Pulse("Damage pulse",Float)=0 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
 Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
 Blend SrcAlpha OneMinusSrcAlpha
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 float _Opacity,_BurnTime,_Aspect,_Pulse;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 float fbm(float2 p){return .55*noise(p)+.28*noise(p*2.13+3.4)+.17*noise(p*4.31+7.1);}
 fixed4 frag(v2f i):SV_Target {
  float2 uv=i.uv;float t=_BurnTime;
  float x=min(uv.x,1-uv.x)*_Aspect,y=min(uv.y,1-uv.y);
  float edge=min(x,y);float along=x<y?uv.y:uv.x*_Aspect;
  float2 flow=float2(along*17+sin(edge*15-t*2)*.35,edge*22-t*2.7);
  float n=fbm(flow),curl=fbm(flow*1.65+float2(t*.4,2));
  float height=.045+n*.12;float flame=saturate((height-edge)*13);
  flame*=smoothstep(.15,.72,n+flame*.45);
  float core=saturate((curl-.42)*3.5)*flame;
  float3 col=lerp(float3(.72,.035,.006),float3(1,.22,.008),saturate(flame*1.5));
  col=lerp(col,float3(1,.68,.1),core*core);
  float a=flame*(.50+_Pulse*.14)*(.58+curl*.65)+saturate(1-edge/.08)*.07;
  return fixed4(col,saturate(a)*_Opacity*i.color.a);
 }
 ENDCG
 }
 }
}
