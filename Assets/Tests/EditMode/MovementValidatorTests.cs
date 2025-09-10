using NUnit.Framework;
using UnityEngine;
using PiggyRace.Networking;

namespace PiggyRace.Tests.EditMode
{
    public class MovementValidatorTests
    {
        [Test]
        public void Clamps_Position_By_Max_Speed()
        {
            Vector3 prev = Vector3.zero;
            float prevYaw = 0f;
            float t0 = 1f;
            Vector3 desired = new Vector3(10f, 0f, 0f);
            float desiredYaw = 0f;
            float now = 1.5f; // dt=0.5s
            float maxSpeed = 3f; // maxDist=1.5m
            float maxYaw = 360f;
            MovementValidator.ClampState(prev, prevYaw, t0, desired, desiredYaw, now, maxSpeed, maxYaw, out var pos, out var yaw);
            Assert.AreEqual(1.5f, pos.x, 0.001f);
            Assert.AreEqual(0f, pos.y, 0.001f);
            Assert.AreEqual(0f, pos.z, 0.001f);
            Assert.AreEqual(0f, yaw, 0.001f);
        }

        [Test]
        public void Clamps_Yaw_By_Max_Rate()
        {
            Vector3 prev = Vector3.zero;
            float prevYaw = 0f;
            float t0 = 0f;
            Vector3 desired = Vector3.zero;
            float desiredYaw = 180f;
            float now = 0.25f; // dt=0.25s
            float maxSpeed = 100f;
            float maxYaw = 90f; // 22.5 deg allowed
            MovementValidator.ClampState(prev, prevYaw, t0, desired, desiredYaw, now, maxSpeed, maxYaw, out var pos, out var yaw);
            Assert.AreEqual(22.5f, yaw, 0.001f);
        }
    }
}
