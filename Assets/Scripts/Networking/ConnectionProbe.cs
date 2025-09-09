using UnityEngine;
using Unity.Netcode;

namespace PiggyRace.Networking
{
    // Simple round-trip test: owner sends timestamp to server, server echos back.
    public class ConnectionProbe : NetworkBehaviour
    {
        private double _lastRttMs;

        public override void OnNetworkSpawn()
        {
            if (IsOwner && IsClient)
            {
                var now = NetworkManager.LocalTime.TimeAsFloat;
                ProbeServerRpc(now);
            }
        }

        [ServerRpc]
        private void ProbeServerRpc(float clientSendTime, ServerRpcParams serverRpcParams = default)
        {
            ProbeClientRpc(clientSendTime, NetworkManager.ServerTime.TimeAsFloat);
        }

        [ClientRpc]
        private void ProbeClientRpc(float clientSendTime, float serverReceiveTime, ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner) return;
            float now = NetworkManager.LocalTime.TimeAsFloat;
            // naive RTT estimate in milliseconds
            _lastRttMs = (now - clientSendTime) * 1000.0;
            Debug.Log($"[NGO] Probe RTT≈{_lastRttMs:F1} ms (server t={serverReceiveTime:F2})");
        }
    }
}

