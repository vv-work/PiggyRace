using NUnit.Framework;
using PiggyRace.Gameplay.Tractor;
using UnityEngine;

namespace PiggyRace.Tests.EditMode
{
    public class TractorModelTests
    {
        [Test]
        public void AcceleratesWithThrottle()
        {
            var m = new TractorModel(0f)
            {
                MaxSpeed = 10f,
                Accel = 20f,
                LinearDrag = 0f
            };

            float time = 0f;
            float dt = 0.02f;
            for (int i = 0; i < 50; i++) // 1s
            {
                m.Step(dt, 1f, 0f, 0f);
                time += dt;
            }

            Assert.That(m.VelocityXZ.magnitude, Is.GreaterThan(5f));
            Assert.That(m.VelocityXZ.magnitude, Is.LessThanOrEqualTo(m.MaxSpeed + 0.01f));
        }

        [Test]
        public void TurnsWithSteer()
        {
            var m = new TractorModel(0f)
            {
                TurnRateDeg = 90f
            };
            m.Step(1f, 0f, 1f, 0f);
            Assert.That(m.YawDeg, Is.InRange(89f, 91f));
        }

        [Test]
        public void DragSlowsDown()
        {
            var m = new TractorModel(0f)
            {
                Accel = 50f,
                LinearDrag = 2f
            };
            // accelerate
            for (int i = 0; i < 20; i++) m.Step(0.02f, 1f, 0f, 0f);
            float v = m.VelocityXZ.magnitude;
            // release throttle
            for (int i = 0; i < 20; i++) m.Step(0.02f, 0f, 0f, 0f);
            Assert.That(m.VelocityXZ.magnitude, Is.LessThan(v));
        }
    }
}

