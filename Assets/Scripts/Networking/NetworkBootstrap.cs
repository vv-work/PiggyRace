using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using PiggyRace.Networking.UGS;
using TMPro;
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
        [Header("UGS UI (optional)")]
        [SerializeField] private TMP_InputField joinCodeInput; // optional: read join code from here

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
#if UGS_MULTIPLAYER || UGS_RELAY
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
                        nm.StartHost();
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
#if UGS_MULTIPLAYER || UGS_RELAY
            try
            {
                var svc = EnsureUgsService();
                bool ok = await svc.InitializeUGSAsync();
                if (ok)
                {
                    // Prefer explicit input field if assigned or findable
                    string codeFromInput = null;
                    try
                    {
                        if (joinCodeInput != null) codeFromInput = joinCodeInput.text;
                        else
                        {
                            // best-effort find if not wired in Inspector
                            var anyInput = GameObject.FindObjectOfType<TMPro.TMP_InputField>();
                            if (anyInput != null) codeFromInput = anyInput.text;
                        }
                    }
                    catch { }
                    var code = !string.IsNullOrWhiteSpace(codeFromInput)
                        ? codeFromInput.Trim().ToUpperInvariant()
                        : (string.IsNullOrWhiteSpace(clientJoinCodeOverride)
                            ? NetPathRuntimeStatus.JoinCode
                            : clientJoinCodeOverride.Trim().ToUpperInvariant());
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
                            nm.StartClient();
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

        // --- UGS explicit controls for UI buttons ---
        public async void InitializeUGS()
        {
#if UGS_MULTIPLAYER || UGS_RELAY
            try
            {
                var svc = EnsureUgsService();
                await svc.InitializeUGSAsync();
            }
            catch { }
#else
            Debug.LogWarning("UGS packages not present. InitializeUGS is a no-op.");
#endif
        }

        public void HostUGS()
        {
            // Uses the Relay-first flow in StartHost
            StartHost();
        }

        public void JoinUGS()
        {
            // Pull code from input if available
            if (joinCodeInput == null)
            {
                // Best-effort: try find one in the scene
                try { joinCodeInput = GameObject.FindObjectOfType<TMP_InputField>(); } catch { }
            }
            if (joinCodeInput != null)
            {
                clientJoinCodeOverride = (joinCodeInput.text ?? string.Empty).Trim().ToUpperInvariant();
            }
            StartClient();
        }

        public async void StartServer()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            var utp = nm.GetComponent<UnityTransport>();
#if UGS_MULTIPLAYER || UGS_RELAY
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
