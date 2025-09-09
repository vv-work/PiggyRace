using NUnit.Framework;
using PiggyRace.Netcode.Interpolation;
using UnityEngine;

namespace PiggyRace.Tests.EditMode
{
    public class SnapshotBufferTests
    {
        [Test]
        public void InterpolatesBetweenSnapshots()
        {
            var buffer = new SnapshotBuffer();
            buffer.AddSnapshot(new NetPigState { Tick = 100, Position = new Vector3(0,0,0), Yaw = 0f });
            buffer.AddSnapshot(new NetPigState { Tick = 110, Position = new Vector3(10,0,0), Yaw = 90f });

            Assert.That(buffer.TryInterpolate(105, out var mid), Is.True);
            Assert.That(mid.Tick, Is.EqualTo(105));
            Assert.That(mid.Position.x, Is.InRange(4.9f, 5.1f));
            Assert.That(Mathf.DeltaAngle(mid.Yaw, 45f), Is.InRange(-0.6f, 0.6f));
        }

        [Test]
        public void ClampsOutsideRange()
        {
            var buffer = new SnapshotBuffer();
            buffer.AddSnapshot(new NetPigState { Tick = 100, Position = new Vector3(1,0,0), Yaw = 10f });
            buffer.AddSnapshot(new NetPigState { Tick = 110, Position = new Vector3(2,0,0), Yaw = 20f });

            buffer.TryInterpolate(90, out var early);
            Assert.That(early.Position.x, Is.EqualTo(1).Within(1e-4));
            buffer.TryInterpolate(999, out var late);
            Assert.That(late.Position.x, Is.EqualTo(2).Within(1e-4));
        }
    }
}

