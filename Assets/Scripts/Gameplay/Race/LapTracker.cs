using UnityEngine;
using Unity.Netcode;
using PiggyRace.Networking;

namespace PiggyRace.Gameplay.Race
{
    // Server-authoritative lap tracking attached to the player vehicle root.
    [DisallowMultipleComponent]
    public class LapTracker : NetworkBehaviour
    {
        [SerializeField] private TrackManager track;

        private readonly LapTrackerLogic logic = new LapTrackerLogic();

        public NetworkVariable<int> CurrentLap = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> NextCheckpoint = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> Finished = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkGameManager _gm;

        public override void OnNetworkSpawn()
        {
            if (track == null) track = FindObjectOfType<TrackManager>();
            _gm = FindObjectOfType<NetworkGameManager>();
            if (IsServer)
            {
                int laps = (_gm != null) ? _gm.TotalLaps : (track != null ? track.TotalLaps : 3);
                int cpCount = (track != null) ? track.CheckpointCount : 1;
                logic.Initialize(cpCount, laps, startTime: 0f, initialCheckpointIndex: 0);
                CurrentLap.Value = 0;
                NextCheckpoint.Value = 0;
                Finished.Value = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;
            if (_gm != null && _gm.Phase.Value != RacePhase.Race) return; // only track progress during race
            if (track == null) return;

            var cp = other.GetComponent<Checkpoint>();
            if (cp == null || cp.Track != track) return;

            if (logic.TryPass(cp.Index, _gm != null ? _gm.RaceTime.Value : Time.time,
                out bool lapCompleted, out bool raceCompleted))
            {
                CurrentLap.Value = logic.CurrentLap;
                NextCheckpoint.Value = logic.NextCheckpointIndex;
                if (raceCompleted)
                {
                    Finished.Value = true;
                }
            }
        }
    }
}

