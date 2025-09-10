using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using PiggyRace.Networking.UGS;
using System.Threading.Tasks;

namespace PiggyRace.Networking
{
    // Simple helper to start/stop NGO from UI buttons or for quick testing.
    public class NetworkBootstrap : MonoBehaviour
    {
        [Header("UGS (optional)")]
        [Tooltip("Max player count when hosting via UGS (including host)")]
        [SerializeField] private int ugsMaxConnections = 8;
        [Tooltip("If set, StartClient will use this join code when connecting via UGS.")]
        [SerializeField] private string clientJoinCodeOverride = string.Empty;

        private RelayLobbyService EnsureUgsService()
        {
            var svc = FindObjectOfType<RelayLobbyService>();
            if (svc == null)
            {
                var go = new GameObject("UGS Service");
                svc = go.AddComponent<RelayLobbyService>();
            }
            return svc;
        }

        public async void StartHost()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            var utp = nm.GetComponent<UnityTransport>();
#if UGS_MULTIPLAYER
            try
            {
                var svc = EnsureUgsService();
                bool ok = await svc.InitializeUGSAsync();
                if (ok)
                {
                    var code = await svc.CreateJoinCodeAsync(Mathf.Max(1, ugsMaxConnections));
                    if (!string.IsNullOrEmpty(code))
                    {
                        NetPathRuntimeStatus.UsingUgs = true;
                        NetPathRuntimeStatus.IsHost = true;
                        NetPathRuntimeStatus.JoinCode = code;
                        NetPathRuntimeStatus.Address = null;
                        NetPathRuntimeStatus.Port = 0;
                        // UGS handler auto-starts NGO Host.
                        return;
                    }
                }
            }
            catch { }
#endif
            if (utp != null)
            {
                // Force direct connection data to avoid Relay requirement when starting via direct buttons.
                utp.SetConnectionData("127.0.0.1", utp.ConnectionData.Port, utp.ConnectionData.ServerListenAddress);
            }
            nm.StartHost();
        }

        public async void StartClient()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            var utp = nm.GetComponent<UnityTransport>();
#if UGS_MULTIPLAYER
            try
            {
                var svc = EnsureUgsService();
                bool ok = await svc.InitializeUGSAsync();
                if (ok)
                {
                    var code = string.IsNullOrWhiteSpace(clientJoinCodeOverride) ? NetPathRuntimeStatus.JoinCode : clientJoinCodeOverride.Trim();
                    if (!string.IsNullOrEmpty(code))
                    {
                        bool joined = await svc.JoinByCodeAsync(code);
                        if (joined)
                        {
                            NetPathRuntimeStatus.UsingUgs = true;
                            NetPathRuntimeStatus.IsHost = false;
                            NetPathRuntimeStatus.JoinCode = code;
                            NetPathRuntimeStatus.Address = null;
                            NetPathRuntimeStatus.Port = 0;
                            // UGS handler auto-starts NGO Client.
                            return;
                        }
                    }
                }
            }
            catch { }
#endif
            if (utp != null)
            {
                utp.SetConnectionData(utp.ConnectionData.Address, utp.ConnectionData.Port, utp.ConnectionData.ServerListenAddress);
            }
            nm.StartClient();
        }

        public async void StartServer()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            var utp = nm.GetComponent<UnityTransport>();
#if UGS_MULTIPLAYER
            // No server-only UGS flow; treat as Host
            StartHost();
            return;
#else
            if (utp != null)
            {
                utp.SetConnectionData("127.0.0.1", utp.ConnectionData.Port, utp.ConnectionData.ServerListenAddress);
            }
            nm.StartServer();
#endif
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
