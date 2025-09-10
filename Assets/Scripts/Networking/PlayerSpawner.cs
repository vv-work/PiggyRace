using System.Linq;
using UnityEngine;
using Unity.Netcode;
using PiggyRace.Gameplay.Race;

namespace PiggyRace.Networking
{
    // Positions player objects on the spawn grid when race starts.
    [DisallowMultipleComponent]
    public class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField] private TrackManager track;
        [SerializeField] private NetworkGameManager gameManager;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            if (track == null) track = FindObjectOfType<TrackManager>();
            if (gameManager == null) gameManager = FindObjectOfType<NetworkGameManager>();
            if (gameManager != null)
            {
                gameManager.Phase.OnValueChanged += OnPhaseChanged;
            }
        }

        private void OnDestroy()
        {
            if (IsServer && gameManager != null)
            {
                gameManager.Phase.OnValueChanged -= OnPhaseChanged;
            }
        }

        private void OnPhaseChanged(RacePhase prev, RacePhase next)
        {
            if (!IsServer) return;
            if (next == RacePhase.Countdown)
            {
                PlacePlayersOnGrid();
            }
        }

        private void PlacePlayersOnGrid()
        {
            if (track == null || track.SpawnPoints == null || track.SpawnPoints.Count == 0) return;

            var players = FindObjectsOfType<NetworkObject>()
                .Where(no => no.IsPlayerObject)
                .OrderBy(no => no.OwnerClientId)
                .ToList();

            for (int i = 0; i < players.Count; i++)
            {
                var spawn = track.GetSpawnPoint(i % track.SpawnPoints.Count);
                if (spawn == null) continue;
                var go = players[i].gameObject;
                var rb = go.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.position = spawn.position;
                    rb.rotation = spawn.rotation;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                else
                {
                    go.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
                }
            }
        }
    }
}

