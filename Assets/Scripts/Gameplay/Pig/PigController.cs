using UnityEngine;
using UnityEngine.InputSystem;

namespace PiggyRace.Gameplay.Pig
{
    // MonoBehaviour wrapper to drive a pig avatar using PigMotor offline.
    [DisallowMultipleComponent]
    public class PigController : MonoBehaviour
    {
        [Header("Control")]
        public bool UseInput = true;
        [Range(-1, 1)] public float Throttle = 0f;
        [Range(-1, 1)] public float Steer = 0f;
        public bool Brake = false;
        public bool Drift = false;
        public bool Boost = false;

        [Header("Tuning (mirrors PigMotor)")]
        public float MaxSpeed = 16f;
        public float ReverseSpeed = 8f;
        public float Accel = 24f;
        public float BrakeDecel = 35f;
        public float LinearDrag = 1.2f;
        public float TurnRateDeg = 140f;
        public float DriftTurnMultiplier = 1.35f;
        public float BoostSpeedAdd = 6f;
        public float BoostDuration = 0.6f;
        public float BoostCooldown = 1.4f;
        [Header("Presentation")]
        public float RotationSpeedDeg = 720f; // how fast we turn the avatar toward motor yaw

        private PigMotor _motor;
        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _motor = new PigMotor(transform.eulerAngles.y)
            {
                MaxSpeed = MaxSpeed,
                ReverseSpeed = ReverseSpeed,
                Accel = Accel,
                BrakeDecel = BrakeDecel,
                LinearDrag = LinearDrag,
                TurnRateDeg = TurnRateDeg,
                DriftTurnMultiplier = DriftTurnMultiplier,
                BoostSpeedAdd = BoostSpeedAdd,
                BoostDuration = BoostDuration,
                BoostCooldown = BoostCooldown
            };
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            var (delta, targetYaw) = _motor.Step(dt, GetThrottle(), GetSteer(), GetBrake(), GetDrift(), GetBoost());
            // Smoothly rotate the visible object to targetYaw using RotationSpeedDeg
            float currentYaw = transform.eulerAngles.y;
            float newYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, RotationSpeedDeg * dt);
            if (_rb != null)
            {
                _rb.MovePosition(_rb.position + new Vector3(delta.x, 0f, delta.y));
                _rb.MoveRotation(Quaternion.Euler(0f, newYaw, 0f));
            }
            else
            {
                transform.position += new Vector3(delta.x, 0f, delta.y);
                transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
            }
        }

        private float GetThrottle()
        {
            if (!UseInput) return Throttle;
            var k = Keyboard.current; if (k == null) return 0f;
            float fwd = k.wKey.isPressed ? 1f : 0f;
            float back = k.sKey.isPressed ? 1f : 0f;
            return Mathf.Clamp(fwd - back, -1f, 1f);
        }
        private float GetSteer()
        {
            if (!UseInput) return Steer;
            var k = Keyboard.current; if (k == null) return 0f;
            float right = k.dKey.isPressed ? 1f : 0f;
            float left = k.aKey.isPressed ? 1f : 0f;
            return Mathf.Clamp(right - left, -1f, 1f);
        }
        private bool GetBrake()
        {
            if (!UseInput) return Brake;
            var k = Keyboard.current; if (k == null) return false;
            return k.spaceKey.isPressed;
        }
        private bool GetDrift()
        {
            if (!UseInput) return Drift;
            var k = Keyboard.current; if (k == null) return false;
            return k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
        }
        private bool GetBoost()
        {
            if (!UseInput) return Boost;
            var k = Keyboard.current; if (k == null) return false;
            return k.leftCtrlKey.wasPressedThisFrame || k.rightCtrlKey.wasPressedThisFrame;
        }
    }
}
