Shader "Hidden/BakeBotTeam" {
 Properties {_MainTex("Source",2D)="white"{}}
 SubShader {Cull Off ZWrite Off ZTest Always
 Pass {
 CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 fixed4 frag(v2f_img i):SV_Target {
  float4 c=tex2D(_MainTex,i.uv);float3 s=LinearToGammaSpace(c.rgb);
  float mask=smoothstep(.28,.48,(s.r-max(s.g,s.b))/max(.001,s.r));
  float3 blue=float3(s.b*.80,s.r*.72,s.r*.95);
  return float4(GammaToLinearSpace(lerp(s,blue,mask)),c.a);
 }
 ENDCG
 }}
}
