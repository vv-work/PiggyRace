using UnityEngine;

namespace PiggyRace.Gameplay.Race
{
    [DisallowMultipleComponent]
    public class Checkpoint : MonoBehaviour
    {
        public int Index = 0; // order within TrackManager
        public TrackManager Track;

        private void OnValidate()
        {
            if (Track == null)
            {
                Track = GetComponentInParent<TrackManager>();
            }
        }
    }
}

