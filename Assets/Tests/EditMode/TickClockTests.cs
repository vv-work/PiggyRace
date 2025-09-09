using NUnit.Framework;
using PiggyRace.Core.Tick;

namespace PiggyRace.Tests.EditMode
{
    public class TickClockTests
    {
        [Test]
        public void StepsTicksAtFixedDelta()
        {
            var clock = new TickClock(0.02f); // 50 Hz
            Assert.That(clock.CurrentTick, Is.EqualTo(0));

            // One full tick
            int adv = clock.Step(0.02f);
            Assert.That(adv, Is.EqualTo(1));
            Assert.That(clock.CurrentTick, Is.EqualTo(1));

            // Two ticks at once
            adv = clock.Step(0.04f);
            Assert.That(adv, Is.EqualTo(2));
            Assert.That(clock.CurrentTick, Is.EqualTo(3));
        }

        [Test]
        public void AccumulatesFractionalDelta()
        {
            var clock = new TickClock(0.02f);
            // 1.5 ticks in two steps
            int a = clock.Step(0.01f);
            int b = clock.Step(0.02f);
            Assert.That(a, Is.EqualTo(0));
            Assert.That(b, Is.EqualTo(1));
            Assert.That(clock.CurrentTick, Is.EqualTo(1));
            // Remaining accumulator should be ~0.01
            Assert.That(clock.Accumulator, Is.InRange(0.0099f, 0.0101f));
        }
    }
}

