using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace PiggyRace.Networking
{
    // Simple helper to start/stop NGO from UI buttons or for quick testing.
    public class NetworkBootstrap : MonoBehaviour
    {
        public void StartHost()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            var utp = nm.GetComponent<UnityTransport>();
            if (utp != null)
            {
                // Force direct connection data to avoid Relay requirement when starting via direct buttons.
                utp.SetConnectionData("127.0.0.1", utp.ConnectionData.Port, utp.ConnectionData.ServerListenAddress);
            }
            nm.StartHost();
        }

        public void StartClient()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            var utp = nm.GetComponent<UnityTransport>();
            if (utp != null)
            {
                utp.SetConnectionData(utp.ConnectionData.Address, utp.ConnectionData.Port, utp.ConnectionData.ServerListenAddress);
            }
            nm.StartClient();
        }

        public void StartServer()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            var utp = nm.GetComponent<UnityTransport>();
            if (utp != null)
            {
                utp.SetConnectionData("127.0.0.1", utp.ConnectionData.Port, utp.ConnectionData.ServerListenAddress);
            }
            nm.StartServer();
        }
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
