using UnityEngine;
using Unity.Netcode;

namespace PiggyRace.Networking
{
    // Simple helper to start/stop NGO from UI buttons or for quick testing.
    public class NetworkBootstrap : MonoBehaviour
    {
        public void StartHost() => NetworkManager.Singleton?.StartHost();
        public void StartClient() => NetworkManager.Singleton?.StartClient();
        public void StartServer() => NetworkManager.Singleton?.StartServer();
        public void Shutdown() => NetworkManager.Singleton?.Shutdown();
    }
}

