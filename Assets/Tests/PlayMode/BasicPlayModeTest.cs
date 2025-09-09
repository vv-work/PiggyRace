using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace PiggyRace.Tests.PlayMode
{
    public class BasicPlayModeTest
    {
        [UnityTest]
        public IEnumerator SceneBootsOneFrame()
        {
            // Placeholder smoke test to verify PlayMode test assembly is wired.
            yield return null;
            Assert.Pass();
        }
    }
}

