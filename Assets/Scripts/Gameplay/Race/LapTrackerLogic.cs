using System.Collections.Generic;
using UnityEngine;

namespace PiggyRace.Gameplay.Race
{
    // Pure logic for lap/checkpoint progression; keeps times per sector.
    public class LapTrackerLogic
    {
        public int CheckpointCount { get; private set; }
        public int TotalLaps { get; private set; }
        public int CurrentLap { get; private set; } // 0-based completed laps
        public int NextCheckpointIndex { get; private set; } // expected next checkpoint (0..N-1)
        public bool RaceFinished { get; private set; }

        public readonly List<float> SectorTimes = new List<float>();
        public float CurrentLapStartTime { get; private set; }
        public float LastSectorStartTime { get; private set; }

        public void Initialize(int checkpointCount, int totalLaps, float startTime = 0f, int initialCheckpointIndex = 0)
        {
            CheckpointCount = Mathf.Max(1, checkpointCount);
            TotalLaps = Mathf.Max(1, totalLaps);
            CurrentLap = 0;
            NextCheckpointIndex = Mathf.Clamp(initialCheckpointIndex, 0, CheckpointCount - 1);
            RaceFinished = false;
            SectorTimes.Clear();
            CurrentLapStartTime = startTime;
            LastSectorStartTime = startTime;
        }

        // Attempts to pass a checkpoint at time. Returns true if accepted and out flags for lap/race completion.
        public bool TryPass(int checkpointIndex, float time,
            out bool lapCompleted, out bool raceCompleted)
        {
            lapCompleted = false;
            raceCompleted = false;
            if (RaceFinished || CheckpointCount <= 0) return false;

            if (checkpointIndex != NextCheckpointIndex)
            {
                // wrong order: ignore
                return false;
            }

            // Record sector time from last sector start
            float sectorTime = Mathf.Max(0f, time - LastSectorStartTime);
            SectorTimes.Add(sectorTime);
            LastSectorStartTime = time;

            // Advance expected checkpoint
            NextCheckpointIndex = (NextCheckpointIndex + 1) % CheckpointCount;

            // Lap completed when we wrapped to 0
            if (NextCheckpointIndex == 0)
            {
                CurrentLap += 1;
                lapCompleted = true;
                CurrentLapStartTime = time;
                if (CurrentLap >= TotalLaps)
                {
                    RaceFinished = true;
                    raceCompleted = true;
                }
            }
            return true;
        }
    }
}

