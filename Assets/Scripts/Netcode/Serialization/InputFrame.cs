using System;

namespace PiggyRace.Netcode.Serialization
{
    // Compact input frame suitable for network transport. Quantizes analogs to sbyte.
    [Serializable]
    public struct InputFrame
    {
        public int Tick;              // simulation tick
        public float Throttle;        // -1..1
        public float Steer;           // -1..1
        public bool Brake;
        public bool Drift;
        public bool Boost;
        public bool ItemUse;

        private static sbyte Quantize(float v) => (sbyte)Math.Clamp((int)Math.Round(v * 127f), -127, 127);
        private static float Dequantize(sbyte q) => Math.Clamp(q / 127f, -1f, 1f);

        // Packs to 7 bytes: 4 (tick) + 1 (throttle) + 1 (steer) + 1 (flags)
        public byte[] Pack()
        {
            var data = new byte[7];
            unchecked
            {
                data[0] = (byte)(Tick);
                data[1] = (byte)(Tick >> 8);
                data[2] = (byte)(Tick >> 16);
                data[3] = (byte)(Tick >> 24);
            }
            data[4] = (byte)Quantize(Throttle);
            data[5] = (byte)Quantize(Steer);
            byte flags = 0;
            if (Brake) flags |= 1 << 0;
            if (Drift) flags |= 1 << 1;
            if (Boost) flags |= 1 << 2;
            if (ItemUse) flags |= 1 << 3;
            data[6] = flags;
            return data;
        }

        public static InputFrame Unpack(ReadOnlySpan<byte> data)
        {
            if (data.Length < 7) throw new ArgumentException("InputFrame needs 7 bytes", nameof(data));
            int tick = data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24);
            sbyte qThrottle = unchecked((sbyte)data[4]);
            sbyte qSteer = unchecked((sbyte)data[5]);
            byte flags = data[6];
            return new InputFrame
            {
                Tick = tick,
                Throttle = Dequantize(qThrottle),
                Steer = Dequantize(qSteer),
                Brake = (flags & (1 << 0)) != 0,
                Drift = (flags & (1 << 1)) != 0,
                Boost = (flags & (1 << 2)) != 0,
                ItemUse = (flags & (1 << 3)) != 0,
            };
        }
    }
}

