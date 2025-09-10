using NUnit.Framework;
using PiggyRace.Gameplay.Race;

namespace PiggyRace.Tests.EditMode
{
    public class LapTrackerLogicTests
    {
        [Test]
        public void Requires_Checkpoints_In_Order()
        {
            var logic = new LapTrackerLogic();
            logic.Initialize(checkpointCount: 3, totalLaps: 2, startTime: 0f);

            // wrong checkpoint first
            Assert.False(logic.TryPass(1, 1f, out var lap1, out var race1));
            Assert.False(lap1); Assert.False(race1);
            Assert.AreEqual(0, logic.CurrentLap);
            Assert.AreEqual(0, logic.NextCheckpointIndex);

            // now correct order 0 -> 1 -> 2 completes a lap
            Assert.True(logic.TryPass(0, 1f, out lap1, out race1));
            Assert.False(lap1); Assert.False(race1);
            Assert.AreEqual(1, logic.NextCheckpointIndex);

            Assert.True(logic.TryPass(1, 2f, out var lap2, out var race2));
            Assert.False(lap2); Assert.False(race2);
            Assert.AreEqual(2, logic.NextCheckpointIndex);

            Assert.True(logic.TryPass(2, 3f, out var lap3, out var race3));
            Assert.True(lap3); Assert.False(race3);
            Assert.AreEqual(1, logic.CurrentLap);
            Assert.AreEqual(0, logic.NextCheckpointIndex);
        }

        [Test]
        public void Completes_Race_After_Total_Laps()
        {
            var logic = new LapTrackerLogic();
            logic.Initialize(checkpointCount: 2, totalLaps: 1, startTime: 0f);

            Assert.True(logic.TryPass(0, 0.5f, out var lapA, out var raceA));
            Assert.False(lapA); Assert.False(raceA);
            Assert.True(logic.TryPass(1, 1.0f, out var lapB, out var raceB));
            Assert.True(lapB); Assert.True(raceB);
            Assert.True(logic.RaceFinished);

            // further checkpoints ignored as finished? accepted but flagged finished, we choose to block further progression
            Assert.False(logic.TryPass(0, 1.1f, out var _, out var _));
        }

        [Test]
        public void Tracks_Sector_Times()
        {
            var logic = new LapTrackerLogic();
            logic.Initialize(checkpointCount: 3, totalLaps: 1, startTime: 10f);
            logic.TryPass(0, 12f, out _, out _); // sector 2s
            logic.TryPass(1, 15.5f, out _, out _); // sector 3.5s
            logic.TryPass(2, 20.0f, out var lap, out var race); // sector 4.5s completes
            Assert.True(lap); Assert.True(race);
            Assert.AreEqual(3, logic.SectorTimes.Count);
            Assert.AreEqual(2f, logic.SectorTimes[0], 0.001f);
            Assert.AreEqual(3.5f, logic.SectorTimes[1], 0.001f);
            Assert.AreEqual(4.5f, logic.SectorTimes[2], 0.001f);
        }
    }
}

