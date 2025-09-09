using NUnit.Framework;
using PiggyRace.Netcode.Serialization;

namespace PiggyRace.Tests.EditMode
{
    public class InputFrameSerializationTests
    {
        [Test]
        public void RoundTripsWithQuantization()
        {
            var src = new InputFrame
            {
                Tick = 1234,
                Throttle = 0.73f,
                Steer = -0.42f,
                Brake = true,
                Drift = false,
                Boost = true,
                ItemUse = true,
            };
            var packed = src.Pack();
            var dst = InputFrame.Unpack(packed);

            Assert.That(dst.Tick, Is.EqualTo(src.Tick));
            Assert.That(dst.Brake, Is.EqualTo(src.Brake));
            Assert.That(dst.Drift, Is.EqualTo(src.Drift));
            Assert.That(dst.Boost, Is.EqualTo(src.Boost));
            Assert.That(dst.ItemUse, Is.EqualTo(src.ItemUse));

            // Allow 1/127 quantization error
            Assert.That(dst.Throttle, Is.InRange(src.Throttle - 0.01f, src.Throttle + 0.01f));
            Assert.That(dst.Steer, Is.InRange(src.Steer - 0.01f, src.Steer + 0.01f));
        }
    }
}

