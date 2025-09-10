using UnityEngine;

namespace PiggyRace.Networking
{
    public static class MovementValidator
    {
        // Clamps a desired state to max linear speed and yaw rate.
        public static void ClampState(
            Vector3 prevPos, float prevYawDeg, float prevTime,
            Vector3 desiredPos, float desiredYawDeg, float now,
            float maxSpeedMetersPerSec, float maxYawRateDegPerSec,
            out Vector3 clampedPos, out float clampedYawDeg)
        {
            float dt = Mathf.Max(0f, now - prevTime);
            // Position
            Vector3 delta = desiredPos - prevPos;
            float maxDist = maxSpeedMetersPerSec * dt;
            if (dt <= 0f || delta.sqrMagnitude <= (maxDist * maxDist))
                clampedPos = desiredPos;
            else
                clampedPos = prevPos + delta.normalized * maxDist;

            // Yaw
            float maxYawDelta = maxYawRateDegPerSec * dt;
            clampedYawDeg = Mathf.MoveTowardsAngle(prevYawDeg, desiredYawDeg, maxYawDelta);
        }
    }
}

