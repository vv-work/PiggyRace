using UnityEngine;

namespace PiggyRace.Core.Tick
{
    // Pure-logic tick clock to support EditMode testing and play-mode integration.
    public class TickClock
    {
        public int CurrentTick { get; private set; }
        public float FixedDelta { get; private set; }
        public float Accumulator { get; private set; }

        public TickClock(float fixedDeltaSeconds = 1f / 60f)
        {
            FixedDelta = Mathf.Max(0.0001f, fixedDeltaSeconds);
            Reset();
        }

        public void Reset()
        {
            CurrentTick = 0;
            Accumulator = 0f;
        }

        // Steps the clock by deltaTime and returns number of ticks advanced.
        public int Step(float deltaTime)
        {
            if (deltaTime < 0f) deltaTime = 0f;
            Accumulator += deltaTime;
            int advanced = 0;
            while (Accumulator + 1e-6f >= FixedDelta)
            {
                Accumulator -= FixedDelta;
                CurrentTick++;
                advanced++;
            }
            return advanced;
        }
    }
}

