using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

namespace PiggyRace.Networking
{
    // Minimal per-player component for ownership and simple metadata.
    [DisallowMultipleComponent]
    public class PlayerConnection : NetworkBehaviour
    {
        public NetworkVariable<FixedString32Bytes> DisplayName = new NetworkVariable<FixedString32Bytes>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public override void OnNetworkSpawn()
        {
            if (IsOwner && DisplayName.Value.Length == 0)
            {
                DisplayName.Value = new FixedString32Bytes($"Player {OwnerClientId}");
            }
        }
    }
}

