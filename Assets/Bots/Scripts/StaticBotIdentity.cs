using UnityEngine;
namespace FpsStage2 {
// Data only. This stage intentionally has no AI, navigation, movement or attack loop.
public sealed class StaticBotIdentity : MonoBehaviour {
 public string team;
 public ClassId characterClass;
 public string weaponName;
 public int health,magazine,pellets;
 public float movementSpeed,shotInterval;
 public AnimationClip[] futureFirstPersonReferences;
}
}
