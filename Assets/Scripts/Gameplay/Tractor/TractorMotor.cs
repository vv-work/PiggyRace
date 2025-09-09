using UnityEngine;
using UnityEngine.InputSystem;

namespace PiggyRace.Gameplay.Tractor
{
    // MonoBehaviour wrapper to drive a GameObject using TractorModel.
    [DisallowMultipleComponent]
    public class TractorMotor : MonoBehaviour
    {
        [Header("Control")]
        public bool UseInput = true;
        [Range(-1, 1)] public float Throttle = 0f;
        [Range(-1, 1)] public float Steer = 0f;
        [Range(0, 1)] public float Brake = 0f;

        [Header("Tuning")]
        public float MaxSpeed = 12f;
        public float ReverseSpeed = 6f;
        public float Accel = 20f;
        public float BrakeDecel = 30f;
        public float LinearDrag = 1.0f;
        public float TurnRateDeg = 120f;

        private TractorModel _model;
        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _model = new TractorModel(transform.eulerAngles.y)
            {
                MaxSpeed = MaxSpeed,
                ReverseSpeed = ReverseSpeed,
                Accel = Accel,
                BrakeDecel = BrakeDecel,
                LinearDrag = LinearDrag,
                TurnRateDeg = TurnRateDeg
            };
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            var (delta, yaw) = _model.Step(dt, GetThrottle(), GetSteer(), GetBrake());

            if (_rb != null)
            {
                _rb.MovePosition(_rb.position + new Vector3(delta.x, 0f, delta.y));
                _rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
            }
            else
            {
                transform.position += new Vector3(delta.x, 0f, delta.y);
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        private float GetThrottle()
        {
            if (!UseInput) return Throttle;
            var k = Keyboard.current;
            if (k == null) return 0f;
            float fwd = k.wKey.isPressed ? 1f : 0f;
            float back = k.sKey.isPressed ? 1f : 0f;
            return Mathf.Clamp(fwd - back, -1f, 1f);
        }

        private float GetSteer()
        {
            if (!UseInput) return Steer;
            var k = Keyboard.current;
            if (k == null) return 0f;
            float right = k.dKey.isPressed ? 1f : 0f;
            float left = k.aKey.isPressed ? 1f : 0f;
            return Mathf.Clamp(right - left, -1f, 1f);
        }

        private float GetBrake()
        {
            if (!UseInput) return Brake;
            var k = Keyboard.current;
            if (k == null) return 0f;
            return k.spaceKey.isPressed ? 1f : 0f;
        }
    }
}
