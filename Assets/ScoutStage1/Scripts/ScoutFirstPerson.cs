using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ScoutStage1
{
    // One shared clock drives both input gating and the animation duration.
    [System.Serializable]
    public sealed class ScoutWeaponTiming
    {
        public const double ShotInterval = 0.3125;
        public const double ReloadDuration = 1.4333;
        public string State { get; private set; } = "Idle";
        public int Shots { get; private set; }
        public int Reloads { get; private set; }
        public double BusyUntil { get; private set; }
        private double nextShot;
        public void Tick(double now)
        {
            if (State == "Idle" || now < BusyUntil) return;
            if (State == "Recarga") Reloads++;
            State = "Idle";
        }
        public bool Fire(double now)
        {
            Tick(now);
            if (State == "Recarga" || now < nextShot) return false;
            State = "Disparo";
            nextShot = BusyUntil = now + ShotInterval;
            Shots++;
            return true;
        }
        public bool Reload(double now)
        {
            Tick(now);
            if (State != "Idle") return false;
            State = "Recarga";
            BusyUntil = now + ReloadDuration;
            return true;
        }
    }

    public sealed class ScoutFirstPerson : MonoBehaviour
    {
        public Animation viewmodel;
        public Camera playerCamera;
        public float mouseSensitivity = 0.08f;
        public ScoutWeaponTiming timing = new ScoutWeaponTiming();
        public string CurrentState => timing.State;
        float yaw, pitch;
        string playing;

        void Start()
        {
            if (playerCamera)
            {
                var angles = playerCamera.transform.localEulerAngles;
                yaw = angles.y;
                pitch = Mathf.DeltaAngle(0, angles.x);
            }
            Play("Idle");
        }
        void Update()
        {
            var now = Time.timeAsDouble;
            timing.Tick(now);
            bool fire = false, reload = false, escape = false;
            Vector2 delta = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            fire = mouse != null && mouse.leftButton.wasPressedThisFrame;
            reload = keyboard != null && keyboard.rKey.wasPressedThisFrame;
            escape = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            if (mouse != null) delta = mouse.delta.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
            fire = Input.GetMouseButtonDown(0);
            reload = Input.GetKeyDown(KeyCode.R);
            escape = Input.GetKeyDown(KeyCode.Escape);
            delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 15;
#endif
            if (escape) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (fire)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                TryFire();
            }
            if (reload) TryReload();
            if (Cursor.lockState == CursorLockMode.Locked && playerCamera)
            {
                yaw += delta.x * mouseSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, -70, 70);
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
            }
            if (playing != timing.State) Play(timing.State);
        }
        public bool TryFire()
        {
            if (!timing.Fire(Time.timeAsDouble)) return false;
            Play("Disparo");
            return true;
        }
        public bool TryReload()
        {
            if (!timing.Reload(Time.timeAsDouble)) return false;
            Play("Recarga");
            return true;
        }
        void Play(string name)
        {
            playing = name;
            if (!viewmodel || viewmodel[name] == null) return;
            var state = viewmodel[name];
            state.wrapMode = name == "Idle" ? WrapMode.Loop : WrapMode.ClampForever;
            state.speed = name == "Disparo" ? state.length / (float)ScoutWeaponTiming.ShotInterval
                : name == "Recarga" ? state.length / (float)ScoutWeaponTiming.ReloadDuration : 1f;
            state.time = 0;
            viewmodel.Play(name, PlayMode.StopAll);
        }
        void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            GUI.Label(new Rect(18, 16, 560, 28), "SCOUT / DISPENSADORA DE CAÑA — ETAPA 1", style);
            GUI.Label(new Rect(18, 45, 560, 28), "Clic: disparar  ·  R: recargar  ·  Ratón: mirar  ·  Esc: liberar cursor");
            GUI.Label(new Rect(18, Screen.height-48, 600, 30),
                $"{CurrentState}   |   Disparo: 0,3125 s   |   Recarga: 1,4333 s");
            GUI.Label(new Rect(Screen.width/2f-5, Screen.height/2f-12, 20, 24), "+");
        }
    }
}

