using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

namespace PiggyRace.Networking
{
    public enum RacePhase : byte
    {
        Lobby = 0,
        Countdown = 1,
        Race = 2,
        Results = 3,
    }

    // Server-owned match state controller. Clients read NetworkVariables for UI.
    [DisallowMultipleComponent]
    public class NetworkGameManager : NetworkBehaviour
    {
        [Header("Race Settings")]
        [SerializeField] private int totalLaps = 3;
        [SerializeField] private float countdownSeconds = 3f;
        [SerializeField] private bool autoStartOnHost = false;

        public NetworkVariable<RacePhase> Phase = new NetworkVariable<RacePhase>(
            RacePhase.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<float> Countdown = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<float> RaceTime = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public int TotalLaps => totalLaps;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            if (autoStartOnHost && Phase.Value == RacePhase.Lobby)
            {
                Phase.Value = RacePhase.Countdown;
                Countdown.Value = Mathf.Max(1f, countdownSeconds);
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            switch (Phase.Value)
            {
                case RacePhase.Countdown:
                    Countdown.Value = Mathf.Max(0f, Countdown.Value - Time.deltaTime);
                    if (Countdown.Value <= 0f)
                    {
                        Phase.Value = RacePhase.Race;
                        RaceTime.Value = 0f;
                    }
                    break;

                case RacePhase.Race:
                    RaceTime.Value += Time.deltaTime;
                    break;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestStartCountdownServerRpc(ServerRpcParams rpcParams = default)
        {
            if (Phase.Value != RacePhase.Lobby) return;
            Phase.Value = RacePhase.Countdown;
            Countdown.Value = Mathf.Max(1f, countdownSeconds);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestAbortToLobbyServerRpc(ServerRpcParams rpcParams = default)
        {
            Phase.Value = RacePhase.Lobby;
            Countdown.Value = 0f;
            RaceTime.Value = 0f;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestShowResultsServerRpc(ServerRpcParams rpcParams = default)
        {
            if (Phase.Value == RacePhase.Race)
            {
                Phase.Value = RacePhase.Results;
            }
        }
    }
}
