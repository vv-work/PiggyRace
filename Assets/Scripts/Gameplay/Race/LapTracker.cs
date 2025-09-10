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
        [Header("Guidance")]
        [SerializeField] private Transform nextCheckpointArrow; // Optional: arrow that points to the next checkpoint
        [SerializeField] private bool hideArrowWhenFinished = true;

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

        private void Update()
        {
            // Local visual guidance: point arrow towards the next checkpoint
            if (nextCheckpointArrow == null || track == null) return;
            if (hideArrowWhenFinished && Finished.Value)
            {
                if (nextCheckpointArrow.gameObject.activeSelf) nextCheckpointArrow.gameObject.SetActive(false);
                return;
            }
            else if (!nextCheckpointArrow.gameObject.activeSelf)
            {
                nextCheckpointArrow.gameObject.SetActive(true);
            }

            int idx = Mathf.Clamp(NextCheckpoint.Value, 0, Mathf.Max(0, track.CheckpointCount - 1));
            if (track.Checkpoints == null || track.Checkpoints.Count == 0) return;
            var cp = track.Checkpoints[idx]; if (cp == null) return;
            Vector3 target = cp.transform.position;
            Vector3 dir = target - nextCheckpointArrow.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                nextCheckpointArrow.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
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
