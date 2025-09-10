using UnityEngine;

namespace PiggyRace.Gameplay.Race
{
    // Pure layout helper to generate a loop of points/orientations.
    public static class LoopLayout
    {
        public static void GenerateEllipse(int count, float radiusX, float radiusZ, float startAngleDeg,
            out Vector3[] positions, out Quaternion[] rotations)
        {
            count = Mathf.Max(1, count);
            positions = new Vector3[count];
            rotations = new Quaternion[count];
            for (int i = 0; i < count; i++)
            {
                float t = (i / (float)count) * Mathf.PI * 2f + startAngleDeg * Mathf.Deg2Rad;
                float x = Mathf.Cos(t) * radiusX;
                float z = Mathf.Sin(t) * radiusZ;
                positions[i] = new Vector3(x, 0f, z);

                // Use orientation perpendicular to radial vector (circular tangent approximation)
                Vector2 radial = new Vector2(x, z);
                if (radial.sqrMagnitude < 1e-6f) radial = new Vector2(1f, 0f);
                radial.Normalize();
                Vector2 tangent = new Vector2(-radial.y, radial.x); // 90° CCW
                float yaw = Mathf.Atan2(tangent.x, tangent.y) * Mathf.Rad2Deg;
                rotations[i] = Quaternion.Euler(0f, yaw, 0f);
            }
        }
    }
}
