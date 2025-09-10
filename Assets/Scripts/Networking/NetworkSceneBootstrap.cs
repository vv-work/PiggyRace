using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace PiggyRace.Networking
{
    // Loads a target scene over NGO when the server/host starts.
    // Place this in your main menu/lobby scene and set the scene name to your race scene.
    [DisallowMultipleComponent]
    public class NetworkSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private string sceneName = "Race";
        [SerializeField] private bool loadOnServerStart = true;

        void OnEnable()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            nm.OnServerStarted += OnServerStarted;
        }

        void OnDisable()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            nm.OnServerStarted -= OnServerStarted;
        }

        private void OnServerStarted()
        {
            if (!loadOnServerStart) return;
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer) return;

            if (nm.SceneManager == null || string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[NetworkSceneBootstrap] Missing SceneManager or scene name.");
                return;
            }

            // If we're already in the target scene, do nothing.
            var active = SceneManager.GetActiveScene().name;
            if (active == sceneName) return;

            nm.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }
}

