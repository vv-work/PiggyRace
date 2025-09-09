using UnityEngine;
using Unity.Netcode;

namespace PiggyRace.Networking
{
    // Attach to any active scene object to get connection logs in Console.
    public class NetworkEventsLogger : MonoBehaviour
    {
        void OnEnable()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            nm.OnServerStarted += OnServerStarted;
            nm.OnClientConnectedCallback += OnClientConnected;
            nm.OnClientDisconnectCallback += OnClientDisconnected;
        }

        void OnDisable()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            nm.OnServerStarted -= OnServerStarted;
            nm.OnClientConnectedCallback -= OnClientConnected;
            nm.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        private void OnServerStarted()
        {
            Debug.Log($"[NGO] Server started. IsHost={NetworkManager.Singleton.IsHost}");
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[NGO] Client connected: {clientId}. LocalClientId={NetworkManager.Singleton.LocalClientId}");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"[NGO] Client disconnected: {clientId}");
        }
    }
}

