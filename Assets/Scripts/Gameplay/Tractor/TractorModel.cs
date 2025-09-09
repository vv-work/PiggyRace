using System;
using UnityEngine;

namespace PiggyRace.Gameplay.Tractor
{
    // Pure logic model for a simple arcade tractor.
    [Serializable]
    public class TractorModel
    {
        public float MaxSpeed = 12f;       // m/s
        public float ReverseSpeed = 6f;    // m/s
        public float Accel = 20f;          // m/s^2
        public float BrakeDecel = 30f;     // m/s^2
        public float LinearDrag = 1.0f;    // s^-1
        public float TurnRateDeg = 120f;   // deg/s at full steer

        public float YawDeg { get; private set; }
        public Vector2 VelocityXZ { get; private set; } // x,z in world space relative to yaw

        public TractorModel() { }

        public TractorModel(float initialYawDeg)
        {
            YawDeg = initialYawDeg;
            VelocityXZ = Vector2.zero;
        }

        // Steps the model forward, returning world-space displacement (x,z) and yaw in degrees.
        public (Vector2 deltaXZ, float yawDeg) Step(float dt, float throttle, float steer, float brake)
        {
            if (dt <= 0f) return (Vector2.zero, YawDeg);
            throttle = Mathf.Clamp(throttle, -1f, 1f);
            steer = Mathf.Clamp(steer, -1f, 1f);
            brake = Mathf.Clamp01(brake);

            // Heading
            YawDeg += steer * TurnRateDeg * dt;

            // Speed scalar along forward
            float speed = VelocityXZ.magnitude;

            if (brake > 0f)
            {
                speed = Mathf.Max(0f, speed - BrakeDecel * brake * dt);
            }
            else
            {
                float maxForward = MaxSpeed * Mathf.Max(0f, throttle);
                float maxReverse = ReverseSpeed * Mathf.Max(0f, -throttle);
                float target = (throttle >= 0f) ? maxForward : -maxReverse;

                // Accelerate towards target speed
                speed = Mathf.MoveTowards(speed * Mathf.Sign(target), target, Accel * dt);
                speed = Mathf.Abs(speed);
            }

            // Apply simple linear drag
            speed = Mathf.Max(0f, speed - speed * LinearDrag * dt);

            // World forward from yaw
            float yawRad = YawDeg * Mathf.Deg2Rad;
            var forward = new Vector2(Mathf.Sin(yawRad), Mathf.Cos(yawRad)); // x,z
            VelocityXZ = forward * speed;

            var delta = VelocityXZ * dt;
            return (delta, YawDeg);
        }
    }
}

