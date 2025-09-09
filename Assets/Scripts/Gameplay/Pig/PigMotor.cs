using System;
using UnityEngine;

namespace PiggyRace.Gameplay.Pig
{
    // Pure-logic pig motor for offline driving and future NGO server-side sim.
    [Serializable]
    public class PigMotor
    {
        [Header("Speed & Accel")]
        public float MaxSpeed = 16f;
        public float ReverseSpeed = 8f;
        public float Accel = 24f;
        public float BrakeDecel = 35f;
        public float LinearDrag = 1.2f;

        [Header("Steering")]
        public float TurnRateDeg = 140f;
        public float DriftTurnMultiplier = 1.35f; // higher turn rate while drifting

        [Header("Boost")]
        public float BoostSpeedAdd = 6f;     // additive speed when boost active
        public float BoostDuration = 0.6f;   // seconds
        public float BoostCooldown = 1.4f;   // seconds after boost ends

        public float YawDeg { get; private set; }
        public Vector2 VelocityXZ { get; private set; }

        private float _boostTimer;     // remaining active boost
        private float _cooldownTimer;  // time until next boost allowed

        public PigMotor() { }
        public PigMotor(float initialYawDeg) { YawDeg = initialYawDeg; VelocityXZ = Vector2.zero; }

        public (Vector2 deltaXZ, float yawDeg) Step(
            float dt,
            float throttle, float steer,
            bool brake, bool drift, bool boostPressed)
        {
            if (dt <= 0f) return (Vector2.zero, YawDeg);
            throttle = Mathf.Clamp(throttle, -1f, 1f);
            steer = Mathf.Clamp(steer, -1f, 1f);

            // Timers
            if (_boostTimer > 0f)
                _boostTimer = Mathf.Max(0f, _boostTimer - dt);
            else if (_cooldownTimer > 0f)
                _cooldownTimer = Mathf.Max(0f, _cooldownTimer - dt);

            // Consume boost input if available
            if (boostPressed && _boostTimer <= 0f && _cooldownTimer <= 0f)
            {
                _boostTimer = BoostDuration;
                _cooldownTimer = BoostCooldown + BoostDuration; // cooldown starts after boost ends
            }

            // Steering
            float turnRate = TurnRateDeg * (drift ? DriftTurnMultiplier : 1f);
            YawDeg += steer * turnRate * dt;

            // Speed control (scalar along forward)
            float speed = VelocityXZ.magnitude;
            if (brake)
            {
                speed = Mathf.Max(0f, speed - BrakeDecel * dt);
            }
            else
            {
                float maxForward = MaxSpeed * Mathf.Max(0f, throttle);
                float maxReverse = ReverseSpeed * Mathf.Max(0f, -throttle);
                float target = (throttle >= 0f) ? maxForward : -maxReverse;
                float signed = speed * Mathf.Sign(target);
                signed = Mathf.MoveTowards(signed, target, Accel * dt);
                speed = Mathf.Abs(signed);
            }

            // Boost if active
            if (_boostTimer > 0f)
                speed += BoostSpeedAdd;

            // Drag
            speed = Mathf.Max(0f, speed - speed * LinearDrag * dt);

            // World forward
            float yawRad = YawDeg * Mathf.Deg2Rad;
            var forward = new Vector2(Mathf.Sin(yawRad), Mathf.Cos(yawRad));
            VelocityXZ = forward * speed;
            var delta = VelocityXZ * dt;
            return (delta, YawDeg);
        }
    }
}

