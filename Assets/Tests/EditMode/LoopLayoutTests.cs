using NUnit.Framework;
using UnityEngine;
using PiggyRace.Gameplay.Race;

namespace PiggyRace.Tests.EditMode
{
    public class LoopLayoutTests
    {
        [Test]
        public void Generates_Correct_Count_And_Rough_Radius()
        {
            LoopLayout.GenerateEllipse(8, 10f, 5f, 0f, out var pos, out var rot);
            Assert.AreEqual(8, pos.Length);
            Assert.AreEqual(8, rot.Length);
            // Check some points are near the intended radii
            Assert.That(Mathf.Abs(pos[0].x - 10f) < 0.01f && Mathf.Abs(pos[0].z) < 0.01f);
            Assert.That(Mathf.Abs(pos[2].z - 5f) < 0.01f);
        }

        [Test]
        public void Rotations_Are_Tangential()
        {
            LoopLayout.GenerateEllipse(16, 10f, 5f, 0f, out var pos, out var rot);
            for (int i = 0; i < pos.Length; i++)
            {
                Vector3 fwd = rot[i] * Vector3.forward;
                // Expect forward to be mostly tangent: perpendicular to radial vector
                Vector3 radial = new Vector3(pos[i].x, 0f, pos[i].z).normalized;
                float dot = Mathf.Abs(Vector3.Dot(fwd, radial));
                Assert.Less(dot, 0.5f, $"Rotation not tangential at {i}");
            }
        }
    }
}

