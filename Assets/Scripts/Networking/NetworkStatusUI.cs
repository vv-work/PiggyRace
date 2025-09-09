using UnityEngine;
using TMPro;
using Unity.Netcode;

namespace PiggyRace.Networking
{
    // Bind a Text field to show basic NGO status and connected clients count.
    public class NetworkStatusUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusText;

        void Update()
        {
            if (statusText == null) return;
            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                statusText.text = "NGO: (no NetworkManager)";
                return;
            }

            string role = nm.IsServer ? (nm.IsHost ? "Host" : "Server") : (nm.IsClient ? "Client" : "Offline");
            int count = nm.ConnectedClientsList?.Count ?? 0;
            statusText.text = $"NGO: {role} | Clients: {count} | LocalId: {nm.LocalClientId}";
        }
    }
}
