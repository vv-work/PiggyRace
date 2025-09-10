using UnityEngine;

namespace PiggyRace.Gameplay.Pig
{
    // Handles visual feedback (animator + particles) based on normalized speed [0..1].
    [DisallowMultipleComponent]
    public class PigVisualController : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParam = "Speed";

        [Header("Particles (low/high)")]
        [SerializeField] private ParticleSystem lowSpeedParticles;
        [SerializeField] private ParticleSystem highSpeedParticles;

        [Header("Thresholds (normalized)")]
        [SerializeField] private float lowThreshold = 0.33f;
        [SerializeField] private float highThreshold = 0.66f;

        private ParticleSystem.EmissionModule _lowEm;
        private ParticleSystem.EmissionModule _highEm;
        private bool _initialized;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (lowSpeedParticles != null) _lowEm = lowSpeedParticles.emission;
            if (highSpeedParticles != null) _highEm = highSpeedParticles.emission;
            _initialized = true;
        }

        // speedNormalized expected in [0,1]
        public void SpeedUpdate(float speedNormalized)
        {
            if (!_initialized) Awake();

            var s = Mathf.Clamp01(speedNormalized);

            if (animator != null && !string.IsNullOrEmpty(speedParam))
            {
                animator.SetFloat(speedParam, s);
            }

            // Determine which particle tier should be active
            bool none = s < lowThreshold;
            bool low = s >= lowThreshold && s < highThreshold;
            bool high = s >= highThreshold;

            SetParticlesActive(lowSpeedParticles, ref _lowEm, low);
            SetParticlesActive(highSpeedParticles, ref _highEm, high);
        }

        private void SetParticlesActive(ParticleSystem ps, ref ParticleSystem.EmissionModule em, bool active)
        {
            if (ps == null) return;
            var playing = ps.isPlaying;
            em.enabled = active;
            if (active)
            {
                if (!playing) ps.Play(true);
            }
            else
            {
                if (playing) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}

