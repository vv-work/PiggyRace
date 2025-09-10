using UnityEngine;
using Unity.Netcode;

namespace PiggyRace.Networking
{
    // Ensures each connected client has a spawned player object.
    // Helpful when auto-spawn settings are disabled or when using external session starters.
    [DisallowMultipleComponent]
    public class AutoPlayerSpawner : MonoBehaviour
    {
        void OnEnable()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            nm.OnServerStarted += OnServerStarted;
            nm.OnClientConnectedCallback += OnClientConnected;
        }

        void OnDisable()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            nm.OnServerStarted -= OnServerStarted;
            nm.OnClientConnectedCallback -= OnClientConnected;
        }

        private void OnServerStarted()
        {
            if (!NetworkManager.Singleton.IsServer) return;
            // Host/Server may already be connected; ensure we have a player for the host.
            TryEnsurePlayerObject(NetworkManager.Singleton.LocalClientId);
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            TryEnsurePlayerObject(clientId);
        }

        private void TryEnsurePlayerObject(ulong clientId)
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || nm.NetworkConfig == null) return;

            if (nm.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
            {
                return; // already has a player object
            }

            var playerPrefab = nm.NetworkConfig.PlayerPrefab;
            if (playerPrefab == null)
            {
                Debug.LogWarning("[AutoPlayerSpawner] NetworkConfig.PlayerPrefab is not assigned.");
                return;
            }

            var instance = Instantiate(playerPrefab);
            var no = instance.GetComponent<NetworkObject>();
            if (no == null)
            {
                Debug.LogError("[AutoPlayerSpawner] Player prefab has no NetworkObject.");
                Destroy(instance);
                return;
            }

            no.SpawnAsPlayerObject(clientId, true);
        }
    }
}

