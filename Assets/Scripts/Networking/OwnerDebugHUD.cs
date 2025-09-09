using TMPro;
using UnityEngine;
using Unity.Netcode;

namespace PiggyRace.Networking
{
    // Attach to the PigPlayer (or a UI object and assign target) to see ownership/input info.
    public class OwnerDebugHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private NetworkPig targetPig; // if null, tries to find on same object

        private void Awake()
        {
            if (targetPig == null) targetPig = GetComponentInParent<NetworkPig>();
        }

        private void Update()
        {
            if (text == null || targetPig == null) return;
            var no = targetPig.NetworkObject;
            var nm = NetworkManager.Singleton;
            if (no == null || nm == null) return;

            text.text = $"Local:{nm.LocalClientId}\nOwner:{no.OwnerClientId}\nIsOwner:{no.IsOwner}\nIsServer:{nm.IsServer}";
        }
    }
}

