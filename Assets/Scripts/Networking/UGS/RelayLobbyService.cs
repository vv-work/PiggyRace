using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace PiggyRace.Networking.UGS
{
    // Lightweight wrapper around Unity Gaming Services Relay with safe fallbacks when packages are missing.
    [DisallowMultipleComponent]
    public class RelayLobbyService : MonoBehaviour
    {
        [Header("Defaults")]
        public int maxConnections = 8;

        private UnityTransport Transport
        {
            get
            {
                var nm = NetworkManager.Singleton;
                return nm != null ? nm.GetComponent<UnityTransport>() : null;
            }
        }

        public async Task<bool> InitializeUGSAsync()
        {
#if UGS_MULTIPLAYER
            try
            {
                // Use the connector to initialize Services + Auth.
                var connector = GetComponent<MultiplayerServicesConnector>() ?? gameObject.AddComponent<MultiplayerServicesConnector>();
                return await connector.InitializeAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UGS] Initialize failed: {e}");
                return false;
            }
#elif UGS_RELAY
            Debug.LogWarning("[UGS] Legacy Relay flow detected. Ensure Services/Core/Authentication are initialized before calling host/join.");
            await Task.CompletedTask;
            return false;
#else
            Debug.LogWarning("[UGS] Multiplayer/Relay packages not installed.");
            await Task.CompletedTask;
            return false;
#endif
        }

        // Preferred: Create a Multiplayer Services session and return a join code.
        // Falls back to legacy Relay when the unified package is not present.
        public async Task<string> CreateJoinCodeAsync(int maxConns)
        {
#if UGS_MULTIPLAYER
            try
            {
                var connector = GetComponent<MultiplayerServicesConnector>() ?? gameObject.AddComponent<MultiplayerServicesConnector>();
                if (!await connector.InitializeAsync()) return null;
                var code = await connector.CreateSessionAsync(Mathf.Max(1, maxConns));
                if (string.IsNullOrEmpty(code))
                {
                    Debug.LogWarning("[UGS] Session created, but no join code yet.");
                }
                return code;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Multiplayer] Create session failed: {e}");
                return null;
            }
#elif UGS_RELAY
            try
            {
                var alloc = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(maxConns);
                string joinCode = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
                var dt = new Unity.Networking.Transport.Relay.RelayServerData(alloc, "dtls");
                Transport?.SetRelayServerData(dt);
                return joinCode;
            }
            catch (Exception e)
            {
                Debug.LogError($"[UGS] Relay create failed: {e}");
                return null;
            }
#else
            Debug.LogWarning("[UGS] Multiplayer/Relay packages not installed.");
            await Task.CompletedTask; return null;
#endif
        }

        // Preferred: Join Multiplayer Services session by code. Falls back to Relay when unified package is not present.
        public async Task<bool> JoinByCodeAsync(string joinCode)
        {
#if UGS_MULTIPLAYER
            try
            {
                var connector = GetComponent<MultiplayerServicesConnector>() ?? gameObject.AddComponent<MultiplayerServicesConnector>();
                if (!await connector.InitializeAsync()) return false;
                return await connector.JoinSessionAsync(joinCode);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Multiplayer] Join session failed: {e}");
                return false;
            }
#elif UGS_RELAY
            try
            {
                var join = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(joinCode);
                var dt = new Unity.Networking.Transport.Relay.RelayServerData(join, "dtls");
                Transport?.SetRelayServerData(dt);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[UGS] Relay join failed: {e}");
                return false;
            }
#else
            Debug.LogWarning("[UGS] Multiplayer/Relay packages not installed.");
            await Task.CompletedTask; return false;
#endif
        }

        // Backward-compat method names — redirect to unified methods
        public Task<string> CreateRelayAllocationAsync(int maxConns) => CreateJoinCodeAsync(maxConns);
        public Task<bool> JoinRelayAsync(string joinCode) => JoinByCodeAsync(joinCode);

        // Lobby functionality intentionally omitted (Lobby API is deprecated/moved under Multiplayer).
    }
}
