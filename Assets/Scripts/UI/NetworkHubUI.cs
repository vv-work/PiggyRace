using TMPro;
using UnityEngine;

using PiggyRace.Networking;
using PiggyRace.Networking.UGS;

namespace PiggyRace.UI
{
    // Simple façade so one panel can control both UGS (RelayLobbyService) and plain NGO (NetworkBootstrap).
    // Wire your buttons to these methods instead of mixing scripts on the UI GameObject.
    [DisallowMultipleComponent]
    public class NetworkHubUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RelayLobbyService ugs;
        [SerializeField] private NetworkBootstrap ngo;

        [Header("UI Inputs")]
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private TMP_Text statusText;

        [Header("Settings")]
        [SerializeField] private int maxConnections = 8;

        // Tracks whether we are using the UGS path (Multiplayer/Relay buttons)
        private bool _usingUgsPath = false;

        void Start()
        {
            // If no UI is wired, spawn a minimal panel at runtime.
            if (joinCodeInput == null || statusText == null)
            {
                TrySpawnBasicPanel();
            }
        }

        void Reset()
        {
            ugs = FindObjectOfType<RelayLobbyService>();
            ngo = FindObjectOfType<NetworkBootstrap>();
        }

        // ---- UGS path ----
        public async void InitializeUGS()
        {
            EnsureRefs();
            _usingUgsPath = true; PiggyRace.Networking.NetPathRuntimeStatus.UsingUgs = true; PiggyRace.Networking.NetPathRuntimeStatus.IsHost = false; PiggyRace.Networking.NetPathRuntimeStatus.JoinCode = null;
            SetStatus("Initializing UGS...");
            bool ok = await ugs.InitializeUGSAsync();
            SetStatus(ok ? "UGS Ready" : "UGS Init Failed");
        }

        public async void HostUGS()
        {
            EnsureRefs();
            _usingUgsPath = true; PiggyRace.Networking.NetPathRuntimeStatus.UsingUgs = true; PiggyRace.Networking.NetPathRuntimeStatus.IsHost = true;
            SetStatus("Creating session...");
            var code = await ugs.CreateJoinCodeAsync(maxConnections);
            if (string.IsNullOrEmpty(code)) { SetStatus("Session/Relay allocation failed"); return; }
            PiggyRace.Networking.NetPathRuntimeStatus.JoinCode = code;
            PiggyRace.Networking.NetPathRuntimeStatus.Address = null;
            PiggyRace.Networking.NetPathRuntimeStatus.Port = 0;
            SetStatus($"Join Code: {code}");
            // Note: With Multiplayer package installed, NGO Host/Client auto-starts via the package handler.
        }

        public async void JoinUGS()
        {
            EnsureRefs();
            _usingUgsPath = true; PiggyRace.Networking.NetPathRuntimeStatus.UsingUgs = true; PiggyRace.Networking.NetPathRuntimeStatus.IsHost = false;
            var code = (joinCodeInput != null ? joinCodeInput.text : null)?.Trim();
            if (string.IsNullOrEmpty(code)) { SetStatus("Enter Join Code"); return; }
            SetStatus("Joining session...");
            bool ok = await ugs.JoinByCodeAsync(code);
            if (ok)
            {
                PiggyRace.Networking.NetPathRuntimeStatus.JoinCode = code;
                PiggyRace.Networking.NetPathRuntimeStatus.Address = null;
                PiggyRace.Networking.NetPathRuntimeStatus.Port = 0;
            }
            SetStatus(ok ? "Joined" : "Join failed");
        }

        // ---- Direct NGO path ----
        public void HostDirect()
        {
            _usingUgsPath = false; PiggyRace.Networking.NetPathRuntimeStatus.Reset();
            PiggyRace.Networking.NetPathRuntimeStatus.UsingUgs = false; PiggyRace.Networking.NetPathRuntimeStatus.IsHost = true;
            try { var utp = Unity.Netcode.NetworkManager.Singleton?.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>(); if (utp != null) { PiggyRace.Networking.NetPathRuntimeStatus.Address = utp.ConnectionData.Address; PiggyRace.Networking.NetPathRuntimeStatus.Port = utp.ConnectionData.Port; } } catch {}
            SetStatus("Starting Host (Direct)"); ngo?.StartHost();
        }
        public void ClientDirect()
        {
            _usingUgsPath = false; PiggyRace.Networking.NetPathRuntimeStatus.Reset();
            PiggyRace.Networking.NetPathRuntimeStatus.UsingUgs = false; PiggyRace.Networking.NetPathRuntimeStatus.IsHost = false;
            try { var utp = Unity.Netcode.NetworkManager.Singleton?.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>(); if (utp != null) { PiggyRace.Networking.NetPathRuntimeStatus.Address = utp.ConnectionData.Address; PiggyRace.Networking.NetPathRuntimeStatus.Port = utp.ConnectionData.Port; } } catch {}
            SetStatus("Starting Client (Direct)"); ngo?.StartClient();
        }
        public void ServerDirect()
        {
            _usingUgsPath = false; PiggyRace.Networking.NetPathRuntimeStatus.Reset();
            PiggyRace.Networking.NetPathRuntimeStatus.UsingUgs = false; PiggyRace.Networking.NetPathRuntimeStatus.IsHost = false;
            try { var utp = Unity.Netcode.NetworkManager.Singleton?.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>(); if (utp != null) { PiggyRace.Networking.NetPathRuntimeStatus.Address = utp.ConnectionData.ServerListenAddress; PiggyRace.Networking.NetPathRuntimeStatus.Port = utp.ConnectionData.Port; } } catch {}
            SetStatus("Starting Server (Direct)"); ngo?.StartServer();
        }
        public void Shutdown() { SetStatus("Shutdown"); ngo?.Shutdown(); }

        // ---- Race controls ----
        public void StartCountdown() => ngo?.StartCountdown();
        public void AbortToLobby() => ngo?.AbortToLobby();
        public void ShowResults() => ngo?.ShowResults();

        private void EnsureRefs()
        {
            if (ugs == null) ugs = FindObjectOfType<RelayLobbyService>();
            if (ngo == null) ngo = FindObjectOfType<NetworkBootstrap>();
        }

        private void SetStatus(string s)
        {
            var badge = _usingUgsPath ? "✅ UGS" : "❌ UGS";
            if (statusText != null) statusText.text = $"{badge}  {s}";
            else Debug.Log($"[NetworkHubUI] {s}");
        }

        private void TrySpawnBasicPanel()
        {
            if (GameObject.Find("UGS Panel") != null) return;

            var canvasGo = new GameObject("UGS Panel", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            var prt = panelGo.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0);
            prt.anchorMax = new Vector2(0, 0);
            prt.sizeDelta = new Vector2(420, 180);
            prt.anchoredPosition = new Vector2(20, 20);
            var img = panelGo.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0, 0, 0, 0.5f);

            // Status text
            var statusGo = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusGo.transform.SetParent(panelGo.transform, false);
            var srt = statusGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 1);
            srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0.5f, 1);
            srt.anchoredPosition = new Vector2(0, -10);
            srt.sizeDelta = new Vector2(-20, 32);
            statusText = statusGo.GetComponent<TextMeshProUGUI>();
            statusText.text = "UGS: not initialized";
            statusText.fontSize = 20;

            // Join code input
            var inputGo = new GameObject("JoinCode", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            inputGo.transform.SetParent(panelGo.transform, false);
            var irt = inputGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 1);
            irt.anchorMax = new Vector2(0, 1);
            irt.pivot = new Vector2(0, 1);
            irt.anchoredPosition = new Vector2(10, -52);
            irt.sizeDelta = new Vector2(400, 36);
            var inputBg = inputGo.GetComponent<UnityEngine.UI.Image>();
            inputBg.color = new Color(1, 1, 1, 0.1f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(inputGo.transform, false);
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = new Vector2(10, 6);
            trt.offsetMax = new Vector2(-10, -6);
            var textComp = textGo.GetComponent<TextMeshProUGUI>();
            textComp.text = "Join Code";
            textComp.fontSize = 20;
            joinCodeInput = inputGo.AddComponent<TMP_InputField>();
            joinCodeInput.textViewport = trt;
            joinCodeInput.textComponent = textComp;
            joinCodeInput.placeholder = textComp;

            // Buttons helper
            UnityEngine.UI.Button AddButton(string name, Vector2 pos, string label, UnityEngine.Events.UnityAction onClick)
            {
                var btnGo = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                btnGo.transform.SetParent(panelGo.transform, false);
                var brt = btnGo.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0, 0);
                brt.anchorMax = new Vector2(0, 0);
                brt.pivot = new Vector2(0, 0);
                brt.anchoredPosition = pos;
                brt.sizeDelta = new Vector2(125, 44);
                var bimg = btnGo.GetComponent<UnityEngine.UI.Image>();
                bimg.color = new Color(1, 1, 1, 0.15f);
                var btn = btnGo.GetComponent<UnityEngine.UI.Button>();
                btn.onClick.AddListener(onClick);

                var lblGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                lblGo.transform.SetParent(btnGo.transform, false);
                var lrt = lblGo.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0, 0);
                lrt.anchorMax = new Vector2(1, 1);
                lrt.offsetMin = new Vector2(6, 6);
                lrt.offsetMax = new Vector2(-6, -6);
                var lbl = lblGo.GetComponent<TextMeshProUGUI>();
                lbl.text = label;
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.fontSize = 20;
                return btn;
            }

            AddButton("Initialize", new Vector2(10, 10), "Initialize", () => InitializeUGS());
            AddButton("Host", new Vector2(145, 10), "Host", () => HostUGS());
            AddButton("Join", new Vector2(280, 10), "Join", () => JoinUGS());
        }
    }
}
