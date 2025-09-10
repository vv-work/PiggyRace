using UnityEngine;
using Unity.Netcode;

namespace PiggyRace.Networking
{
    // Simple helper to start/stop NGO from UI buttons or for quick testing.
    public class NetworkBootstrap : MonoBehaviour
    {
        public void StartHost() => NetworkManager.Singleton?.StartHost();
        public void StartClient() => NetworkManager.Singleton?.StartClient();
        public void StartServer() => NetworkManager.Singleton?.StartServer();
        public void Shutdown() => NetworkManager.Singleton?.Shutdown();

        // Single-scene helpers
        public void StartCountdown()
        {
            var gm = FindObjectOfType<NetworkGameManager>();
            if (gm != null)
            {
                if (gm.IsServer) gm.RequestStartCountdownServerRpc();
                else Debug.LogWarning("StartCountdown called on client without authority.");
            }
        }

        public void AbortToLobby()
        {
            var gm = FindObjectOfType<NetworkGameManager>();
            if (gm != null && gm.IsServer) gm.RequestAbortToLobbyServerRpc();
        }

        public void ShowResults()
        {
            var gm = FindObjectOfType<NetworkGameManager>();
            if (gm != null && gm.IsServer) gm.RequestShowResultsServerRpc();
        }
    }
}
