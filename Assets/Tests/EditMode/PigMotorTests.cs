using NUnit.Framework;
using PiggyRace.Gameplay.Pig;
using UnityEngine;

namespace PiggyRace.Tests.EditMode
{
    public class PigMotorTests
    {
        [Test]
        public void AcceleratesAndTurns()
        {
            var m = new PigMotor(0f)
            {
                MaxSpeed = 12f,
                Accel = 30f,
                LinearDrag = 0.5f,
                TurnRateDeg = 120f
            };

            // accelerate forward for 1s
            for (int i = 0; i < 60; i++) m.Step(1f/60f, 1f, 0f, false, false, false);
            float v = m.VelocityXZ.magnitude;
            Assert.That(v, Is.GreaterThan(5f));

            // turn right for 0.5s
            float startYaw = m.YawDeg;
            for (int i = 0; i < 30; i++) m.Step(1f/60f, 1f, 1f, false, false, false);
            Assert.That(m.YawDeg, Is.GreaterThan(startYaw + 10f));
        }

        [Test]
        public void DriftIncreasesTurnRate()
        {
            var m1 = new PigMotor(0f) { TurnRateDeg = 100f, DriftTurnMultiplier = 1.5f };
            var m2 = new PigMotor(0f) { TurnRateDeg = 100f, DriftTurnMultiplier = 1.5f };
            for (int i = 0; i < 30; i++) m1.Step(1f/60f, 0f, 1f, false, false, false);
            for (int i = 0; i < 30; i++) m2.Step(1f/60f, 0f, 1f, false, true, false);
            Assert.That(m2.YawDeg, Is.GreaterThan(m1.YawDeg + 5f));
        }

        [Test]
        public void BoostTemporarilyIncreasesSpeed()
        {
            var m = new PigMotor(0f)
            {
                MaxSpeed = 8f,
                Accel = 100f,
                LinearDrag = 0f,
                BoostSpeedAdd = 4f,
                BoostDuration = 0.3f,
                BoostCooldown = 0.1f
            };
            // reach max speed
            for (int i = 0; i < 60; i++) m.Step(1f/60f, 1f, 0f, false, false, false);
            float baseSpeed = m.VelocityXZ.magnitude;
            // trigger boost
            m.Step(1f/60f, 1f, 0f, false, false, true);
            float boosted = m.VelocityXZ.magnitude;
            Assert.That(boosted, Is.GreaterThan(baseSpeed + 1f));
            // after duration, speed should return near base
            for (int i = 0; i < 40; i++) m.Step(1f/60f, 1f, 0f, false, false, false);
            Assert.That(m.VelocityXZ.magnitude, Is.InRange(baseSpeed - 0.5f, baseSpeed + 0.5f));
        }
    }
}

