using TMPro;
using UnityEngine;
using Unity.Netcode;
using PiggyRace.Networking;
using PiggyRace.Gameplay.Race;

namespace PiggyRace.UI
{
    // Minimal HUD showing Phase, Countdown, and Lap/Total for the local player.
    [DisallowMultipleComponent]
    public class RaceHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text targetText; // assign a TextMeshProUGUI in the Canvas
        [SerializeField] private string format = "Phase: {0}  |  Countdown: {1:0.0}  |  Lap: {2}/{3}  |  CP: {4}/{5}";

        private NetworkGameManager _gm;
        private LapTracker _localLap;
        private TrackManager _track;
        private float _nextFindTime;

        private void Awake()
        {
            if (targetText == null)
            {
                targetText = GetComponent<TMP_Text>();
            }
        }

        private void Update()
        {
            if (targetText == null)
                return;

            if (_gm == null || ((_localLap == null || _track == null) && Time.time >= _nextFindTime))
            {
                _gm = FindObjectOfType<NetworkGameManager>();
                _localLap = FindLocalLapTracker();
                _track = FindObjectOfType<TrackManager>();
                _nextFindTime = Time.time + 0.5f; // avoid searching every frame
            }

            var phase = _gm != null ? _gm.Phase.Value : RacePhase.Lobby;
            float countdown = (_gm != null && phase == RacePhase.Countdown) ? _gm.Countdown.Value : 0f;
            int totalLaps = _gm != null ? _gm.TotalLaps : 3;
            int currentLap = _localLap != null ? _localLap.CurrentLap.Value : 0;
            int cpTotal = _track != null ? _track.CheckpointCount : 0;
            int cpPassed = 0;
            if (_localLap != null)
            {
                // NextCheckpoint is the next index to hit; passed in this lap equals that index
                cpPassed = Mathf.Clamp(_localLap.NextCheckpoint.Value, 0, Mathf.Max(0, cpTotal));
            }

            targetText.text = string.Format(
                format,
                phase,
                countdown,
                Mathf.Clamp(currentLap, 0, totalLaps),
                totalLaps,
                cpPassed,
                cpTotal);
        }

        private LapTracker FindLocalLapTracker()
        {
            var trackers = FindObjectsOfType<LapTracker>(true);
            foreach (var lt in trackers)
            {
                var no = lt.NetworkObject;
                if (no != null && no.IsOwner)
                    return lt;
            }
            return null;
        }
    }
}
