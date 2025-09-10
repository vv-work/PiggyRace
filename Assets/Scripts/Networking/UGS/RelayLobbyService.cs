using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#if UGS_MULTIPLAYER
using Unity.Services.Relay.Models; // for AllocationUtils.ToRelayServerData
#endif

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

        // Create a Relay allocation and return its join code. Works with either Multiplayer package (which includes Relay) or standalone Relay.
        public async Task<string> CreateJoinCodeAsync(int maxConns)
        {
#if UGS_MULTIPLAYER || UGS_RELAY
            try
            {
                var alloc = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(maxConns);
                string joinCode = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
#if UGS_MULTIPLAYER
                var dt = alloc.ToRelayServerData("dtls");
#else
                var dt = new Unity.Networking.Transport.Relay.RelayServerData(
                    alloc.ServerEndpoints[0].Host,
                    (ushort)alloc.ServerEndpoints[0].Port,
                    alloc.AllocationIdBytes,
                    alloc.ConnectionData,
                    alloc.ConnectionData,
                    alloc.Key,
                    alloc.ServerEndpoints[0].Secure,
                    alloc.ServerEndpoints[0].ConnectionType == "wss");
#endif
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

        // Join a Relay allocation by code and configure UnityTransport. Works with either Multiplayer package or standalone Relay.
        public async Task<bool> JoinByCodeAsync(string joinCode)
        {
#if UGS_MULTIPLAYER || UGS_RELAY
            try
            {
                var join = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(joinCode);
#if UGS_MULTIPLAYER
                var dt = join.ToRelayServerData("dtls");
#else
                var dt = new Unity.Networking.Transport.Relay.RelayServerData(
                    join.ServerEndpoints[0].Host,
                    (ushort)join.ServerEndpoints[0].Port,
                    join.AllocationIdBytes,
                    join.ConnectionData,
                    join.HostConnectionData,
                    join.Key,
                    join.ServerEndpoints[0].Secure,
                    join.ServerEndpoints[0].ConnectionType == "wss");
#endif
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
