using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#if UGS_MULTIPLAYER
using Unity.Services.Core;
#if UGS_AUTH
using Unity.Services.Authentication;
#endif
using Unity.Services.Multiplayer;
#endif

namespace PiggyRace.Networking.UGS
{
    // Unified Multiplayer Services connector (join-code flow).
    // Safe-by-default: contains no direct references to Unity.Services.* APIs.
    // Actual calls should be added inside the UGS_MULTIPLAYER blocks once the package is in your project.
    [DisallowMultipleComponent]
    public class MultiplayerServicesConnector : MonoBehaviour
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

        public bool IsMultiplayerAvailable()
        {
#if UGS_MULTIPLAYER
            return true;
#else
            return false;
#endif
        }

        public async Task<bool> InitializeAsync()
        {
#if UGS_MULTIPLAYER
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }
#if UGS_AUTH
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
#endif
                Debug.Log("[Multiplayer] UGS initialized");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Multiplayer] Initialize failed: {e}");
                return false;
            }
#else
            Debug.LogWarning("[Multiplayer] Package not installed. Install com.unity.services.multiplayer to enable.");
            await Task.CompletedTask;
            return false;
#endif
        }

        // Host side: create a session and return a human-readable join code (or null on failure).
        public async Task<string> CreateSessionAsync(int maxConns)
        {
#if UGS_MULTIPLAYER
            try
            {
                const string sessionType = "PiggyRace";

                // If a session of this type already exists in this process, reuse it and return its code.
                if (MultiplayerService.Instance != null && MultiplayerService.Instance.Sessions != null &&
                    MultiplayerService.Instance.Sessions.TryGetValue(sessionType, out var existing) && existing != null)
                {
                    string existingCode = existing.Code;
                    if (string.IsNullOrWhiteSpace(existingCode))
                    {
                        var startWait = Time.realtimeSinceStartup;
                        while (string.IsNullOrWhiteSpace(existingCode) && Time.realtimeSinceStartup - startWait < 5f)
                        {
                            await Task.Delay(50);
                            existingCode = existing.Code;
                        }
                    }
                    return existingCode; // may be null if still populating; caller can retry
                }

                var options = new SessionOptions
                {
                    Type = sessionType,
                    Name = Application.productName,
                    MaxPlayers = Mathf.Max(1, maxConns),
                    IsPrivate = false,
                    IsLocked = false
                };

                var hostSession = await MultiplayerService.Instance.CreateSessionAsync(options);

                // Session.Code is the join code (from Lobby). It may be immediately available.
                string code = hostSession?.Code;
                if (string.IsNullOrWhiteSpace(code))
                {
                    // Give a brief window for the code to populate.
                    var start = Time.realtimeSinceStartup;
                    while (string.IsNullOrWhiteSpace(code) && Time.realtimeSinceStartup - start < 5f)
                    {
                        await Task.Delay(50);
                        code = hostSession?.Code;
                    }
                }

                if (string.IsNullOrWhiteSpace(code))
                {
                    Debug.LogWarning("[Multiplayer] Session created but join code not available.");
                }

                return code;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Multiplayer] CreateSessionAsync failed: {e}");
                return null;
            }
#else
            Debug.LogWarning("[Multiplayer] Package not installed.");
            await Task.CompletedTask;
            return null;
#endif
        }

        // Client side: join session by code. Returns true on success and configures transport.
        public async Task<bool> JoinSessionAsync(string joinCode)
        {
#if UGS_MULTIPLAYER
            try
            {
                if (string.IsNullOrWhiteSpace(joinCode))
                {
                    Debug.LogWarning("[Multiplayer] JoinSessionAsync: join code is empty.");
                    return false;
                }

                var options = new JoinSessionOptions
                {
                    Type = "PiggyRace"
                };

                var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode.Trim(), options);

                // If we reached here without exception, NGO handler should have started the client.
                return session != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Multiplayer] JoinSessionAsync failed: {e}");
                return false;
            }
#else
            Debug.LogWarning("[Multiplayer] Package not installed.");
            await Task.CompletedTask;
            return false;
#endif
        }

        // Helper to configure UnityTransport (fill with data from the session/join response when wiring the API):
        private void ConfigureTransport(/* session endpoint data */)
        {
            var utp = Transport;
            if (utp == null)
            {
                Debug.LogWarning("[Multiplayer] UnityTransport not found on NetworkManager.");
                return;
            }
            // TODO: Set endpoints/keys on UnityTransport based on Multiplayer session allocation.
        }
    }
}
