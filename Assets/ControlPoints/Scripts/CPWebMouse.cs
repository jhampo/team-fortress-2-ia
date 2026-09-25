using System.Runtime.InteropServices;
using UnityEngine;
namespace FpsStage2 {
// Pointer-lock displacement, consumed once per frame. Never scale by canvas size or FPS.
public static class CPWebMouse {
#if UNITY_WEBGL && !UNITY_EDITOR
 [DllImport("__Internal")] static extern void CPWebMouseRead(out float x,out float y,int accept);
#endif
 public static Vector2 Read(Vector2 nativeDelta,bool accept){
#if UNITY_WEBGL && !UNITY_EDITOR
  CPWebMouseRead(out float x,out float y,accept?1:0);return new Vector2(x,-y);
#else
  return accept?nativeDelta:Vector2.zero;
#endif
 }
}
}
