using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PiggyRace.UI;

namespace PiggyRace.UI
{
    // Creates a minimal UGS control panel at runtime if not already present.
    // Useful to avoid manual scene wiring and to ensure Host/Join via UGS are available.
    [DisallowMultipleComponent]
    public class UGSPanelSpawner : MonoBehaviour
    {
        [Header("Placement")]
        public Vector2 anchoredPosition = new Vector2(20, 20);
        public Vector2 size = new Vector2(420, 180);

        private NetworkHubUI _hub;

        void Awake()
        {
            _hub = FindObjectOfType<NetworkHubUI>();
        }

        void Start()
        {
            if (_hub == null)
            {
                _hub = FindObjectOfType<NetworkHubUI>();
                if (_hub == null)
                {
                    Debug.LogWarning("[UGSPanelSpawner] NetworkHubUI not found; panel will still be created but won’t function.");
                }
            }

            if (GameObject.Find("UGS Panel") != null) return; // already present

            // Root canvas (reuse existing EventSystem in scene)
            var canvasGo = new GameObject("UGS Panel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Panel background
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            var prt = panelGo.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0);
            prt.anchorMax = new Vector2(0, 0);
            prt.sizeDelta = size;
            prt.anchoredPosition = anchoredPosition;
            var img = panelGo.GetComponent<Image>();
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
            var statusText = statusGo.GetComponent<TextMeshProUGUI>();
            statusText.text = "UGS: not initialized";
            statusText.fontSize = 20;

            // Join code input
            var inputGo = new GameObject("JoinCode", typeof(RectTransform), typeof(Image));
            inputGo.transform.SetParent(panelGo.transform, false);
            var irt = inputGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 1);
            irt.anchorMax = new Vector2(0, 1);
            irt.pivot = new Vector2(0, 1);
            irt.anchoredPosition = new Vector2(10, -52);
            irt.sizeDelta = new Vector2(size.x - 20, 36);
            var inputBg = inputGo.GetComponent<Image>();
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

            var input = inputGo.AddComponent<TMP_InputField>();
            input.textViewport = trt;
            input.textComponent = textComp;
            input.placeholder = textComp;

            // Buttons
            Button AddButton(string name, Vector2 pos, string label, UnityEngine.Events.UnityAction onClick)
            {
                var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(panelGo.transform, false);
                var brt = btnGo.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0, 0);
                brt.anchorMax = new Vector2(0, 0);
                brt.pivot = new Vector2(0, 0);
                brt.anchoredPosition = pos;
                brt.sizeDelta = new Vector2((size.x - 30) / 3f, 44);
                var bimg = btnGo.GetComponent<Image>();
                bimg.color = new Color(1, 1, 1, 0.15f);
                var btn = btnGo.GetComponent<Button>();
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

            // Wire buttons
            var initBtn = AddButton("Initialize", new Vector2(10, 10), "Initialize", () => _hub?.InitializeUGS());
            var hostBtn = AddButton("Host", new Vector2(10 + (size.x - 30) / 3f + 5, 10), "Host", () => _hub?.HostUGS());
            var joinBtn = AddButton("Join", new Vector2(10 + 2 * ((size.x - 30) / 3f + 5), 10), "Join", () => _hub?.JoinUGS());

            // Hook fields into NetworkHubUI
            if (_hub != null)
            {
                NetworkHubUIRuntimeExtensions.JoinCodeInput = input;
                NetworkHubUIRuntimeExtensions.StatusText = statusText;
            }
        }
    }

    // Small extensions as properties if fields are private; reflected by Unity via SerializeField, but here we can set via code.
    public static class NetworkHubUIRuntimeExtensions
    {
        public static TMP_InputField JoinCodeInput
        {
            set
            {
                var hub = Object.FindObjectOfType<NetworkHubUI>();
                if (hub == null) return;
                var field = typeof(NetworkHubUI).GetField("joinCodeInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(hub, value);
            }
        }

        public static TextMeshProUGUI StatusText
        {
            set
            {
                var hub = Object.FindObjectOfType<NetworkHubUI>();
                if (hub == null) return;
                var field = typeof(NetworkHubUI).GetField("statusText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(hub, value);
            }
        }
    }
}
