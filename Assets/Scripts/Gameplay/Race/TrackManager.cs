using System.Collections.Generic;
using UnityEngine;

namespace PiggyRace.Gameplay.Race
{
    [DisallowMultipleComponent]
    public class TrackManager : MonoBehaviour
    {
        [Tooltip("Number of laps to complete this track.")]
        public int TotalLaps = 3;

        [Tooltip("Ordered checkpoints. Auto-filled from children with Checkpoint components if empty.")]
        public List<Checkpoint> Checkpoints = new List<Checkpoint>();

        [Tooltip("Ordered spawn points for players.")]
        public List<Transform> SpawnPoints = new List<Transform>();

        private void OnValidate()
        {
            if (Checkpoints == null || Checkpoints.Count == 0)
            {
                Checkpoints = new List<Checkpoint>(GetComponentsInChildren<Checkpoint>(true));
                for (int i = 0; i < Checkpoints.Count; i++)
                {
                    if (Checkpoints[i] != null) { Checkpoints[i].Index = i; Checkpoints[i].Track = this; }
                }
            }
        }

        public int CheckpointCount => Checkpoints?.Count ?? 0;

        public Transform GetSpawnPoint(int index)
        {
            if (SpawnPoints == null || SpawnPoints.Count == 0) return null;
            index = Mathf.Clamp(index, 0, SpawnPoints.Count - 1);
            return SpawnPoints[index];
        }
    }
}

