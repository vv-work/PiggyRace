using TMPro;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using PiggyRace.Networking;

#if UGS_MULTIPLAYER
using Unity.Services.Multiplayer;
#endif

namespace PiggyRace.UI
{
    // Displays whether the session is running through UGS (✅) or direct NGO (❌),
    // along with a concise detail string (join code or IP:port and role).
    [DisallowMultipleComponent]
    public class NetworkStatusUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text targetText;
        [SerializeField] private string label = "UGS";
        [SerializeField] private bool showDetails = true;

        private NetworkManager _nm;
        private UnityTransport _utp;
        private float _nextProbe;

        private void Awake()
        {
            if (targetText == null)
            {
                targetText = GetComponent<TMP_Text>();
            }
        }

        private void LateUpdate()
        {
            if (targetText == null) return;

            // Refresh refs occasionally
            if (Time.unscaledTime >= _nextProbe || _nm == null)
            {
                _nm = NetworkManager.Singleton;
                _utp = _nm != null ? _nm.GetComponent<UnityTransport>() : null;
                _nextProbe = Time.unscaledTime + 0.5f;
            }

            bool usingUgs = InferUsingUgs();
            var badge = usingUgs ? "✅" : "❌";
            if (!showDetails)
            {
                targetText.text = $"{label}: {badge}";
                return;
            }

            string role = InferRole();
            if (usingUgs)
            {
                string code = InferJoinCode();
                string codePart = string.IsNullOrWhiteSpace(code) ? string.Empty : $" | Code: {code}";
                targetText.text = $"{label}: {badge} Internet (Relay){codePart} | Role: {role}";
            }
            else
            {
                string addr = _utp != null ? _utp.ConnectionData.Address : "127.0.0.1";
                int port = _utp != null ? _utp.ConnectionData.Port : 7777;
                if (_nm != null && (_nm.IsServer || _nm.IsHost))
                {
                    // Show listen address when hosting
                    addr = _utp != null ? _utp.ConnectionData.ServerListenAddress : addr;
                }
                targetText.text = $"{label}: {badge} Local (Direct) | {addr}:{port} | Role: {role}";
            }
        }

        private string InferRole()
        {
            if (_nm == null) return "Offline";
            if (_nm.IsHost) return "Host";
            if (_nm.IsServer) return "Server";
            if (_nm.IsClient) return "Client";
            return "Offline";
        }

        private bool InferUsingUgs()
        {
#if UGS_MULTIPLAYER
            try
            {
                var svc = MultiplayerService.Instance;
                if (svc != null && svc.Sessions != null && svc.Sessions.Count > 0)
                    return true;
            }
            catch { }
#endif
            // Fallback to runtime status set by NetworkHubUI
            return NetPathRuntimeStatus.UsingUgs;
        }

        private string InferJoinCode()
        {
#if UGS_MULTIPLAYER
            try
            {
                var svc = MultiplayerService.Instance;
                if (svc != null && svc.Sessions != null && svc.Sessions.Count > 0)
                {
                    foreach (var kv in svc.Sessions)
                    {
                        var session = kv.Value;
                        if (session != null && !string.IsNullOrWhiteSpace(session.Code))
                            return session.Code;
                    }
                }
            }
            catch { }
#endif
            return NetPathRuntimeStatus.JoinCode;
        }
    }
}
