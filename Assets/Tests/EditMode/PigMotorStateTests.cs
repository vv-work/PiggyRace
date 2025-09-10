using NUnit.Framework;
using PiggyRace.Gameplay.Pig;

namespace PiggyRace.Tests.EditMode
{
    public class PigMotorStateTests
    {
        [Test]
        public void Snapshot_Restore_Roundtrip_Matches_Trajectory()
        {
            var m1 = new PigMotor(0f);
            var m2 = new PigMotor(0f);

            float dt = 0.016f;
            // Warmup few ticks
            for (int i = 0; i < 10; i++)
            {
                m1.Step(dt, 1f, 0.2f, false, false, false);
                m2.Step(dt, 1f, 0.2f, false, false, false);
            }

            // Diverge a bit, then snapshot state S from m1
            var s = m1.Capture();

            // Run several steps on both
            for (int i = 0; i < 20; i++)
            {
                m1.Step(dt, 1f, 0.1f, false, (i % 7)==0, (i==5));
                m2.Step(dt, 1f, 0.1f, false, (i % 7)==0, (i==5));
            }

            // Restore m2 to snapshot and replay the same inputs; results should match m1 when replayed equally
            m2.Restore(s);
            for (int i = 0; i < 20; i++)
            {
                m2.Step(dt, 1f, 0.1f, false, (i % 7)==0, (i==5));
            }

            Assert.AreEqual(m1.YawDeg, m2.YawDeg, 0.01f);
            Assert.AreEqual(m1.VelocityXZ.x, m2.VelocityXZ.x, 0.01f);
            Assert.AreEqual(m1.VelocityXZ.y, m2.VelocityXZ.y, 0.01f);
        }
    }
}

