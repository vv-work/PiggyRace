using System.Collections;
using NUnit.Framework;
using PiggyRace.Gameplay.Tractor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PiggyRace.Tests.PlayMode
{
    public class TractorMotorPlayModeTest
    {
        [UnityTest]
        public IEnumerator MovesForwardAndTurns()
        {
            var go = new GameObject("Tractor");
            var motor = go.AddComponent<TractorMotor>();
            motor.UseInput = false;
            motor.Throttle = 1f;
            // advance ~0.5s
            for (int i = 0; i < 30; i++) yield return null;
            Assert.Greater(go.transform.position.z, 0.1f);

            motor.Steer = 1f;
            // advance ~0.5s
            float startYaw = go.transform.eulerAngles.y;
            for (int i = 0; i < 30; i++) yield return null;
            float endYaw = go.transform.eulerAngles.y;
            Assert.Greater(Mathf.DeltaAngle(endYaw, startYaw) * -1f, 1f); // yaw increased
        }
    }
}

