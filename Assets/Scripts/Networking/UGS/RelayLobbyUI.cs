using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Unity.Netcode;

namespace PiggyRace.Networking.UGS
{
    // Minimal UI bridge for UGS. Drop onto a UI GameObject and hook buttons.
    public class RelayLobbyUI : MonoBehaviour
    {
        [SerializeField] private RelayLobbyService service;
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private int maxConnections = 8;

        private void Reset()
        {
            service = FindObjectOfType<RelayLobbyService>();
        }

        public async void InitializeUGS()
        {
            EnsureService();
            SetStatus("Initializing UGS...");
            bool ok = await service.InitializeUGSAsync();
            SetStatus(ok ? "UGS Ready" : "UGS Init Failed");
        }

        public async void HostWithRelay()
        {
            EnsureService();
            SetStatus("Creating session...");
            string code = await service.CreateJoinCodeAsync(maxConnections);
            if (string.IsNullOrEmpty(code)) { SetStatus("Allocation/session failed"); return; }
            SetStatus($"Join Code: {code}");
            NetworkManager.Singleton?.StartHost();
        }

        public async void JoinWithRelay()
        {
            EnsureService();
            var code = joinCodeInput != null ? joinCodeInput.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(code)) { SetStatus("Enter Join Code"); return; }
            SetStatus("Joining session...");
            bool ok = await service.JoinByCodeAsync(code);
            if (!ok) { SetStatus("Join failed"); return; }
            NetworkManager.Singleton?.StartClient();
        }

        private void EnsureService()
        {
            if (service == null) service = FindObjectOfType<RelayLobbyService>();
            if (service == null)
            {
                var go = new GameObject("UGS Service");
                service = go.AddComponent<RelayLobbyService>();
            }
        }

        private void SetStatus(string s)
        {
            if (statusText != null) statusText.text = s;
            else Debug.Log($"[UGS UI] {s}");
        }
    }
}
