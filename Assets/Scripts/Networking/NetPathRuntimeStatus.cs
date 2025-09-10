namespace PiggyRace.Networking
{
    // Simple global bag of status about how networking was started.
    // Set by NetworkHubUI; read by UI (e.g., NetworkStatusUI) to display a badge and details.
    public static class NetPathRuntimeStatus
    {
        // True when started via UGS Multiplayer/Relay flow; false when started via direct NGO (IP/port)
        public static bool UsingUgs { get; set; } = false;

        // Optional: join code when using UGS (host or client who joined by code)
        public static string JoinCode { get; set; } = null;

        // Direct connection info (for non-UGS path)
        public static string Address { get; set; } = null;
        public static int Port { get; set; } = 0;

        // Role hint
        public static bool IsHost { get; set; } = false;

        public static void Reset()
        {
            UsingUgs = false;
            JoinCode = null;
            Address = null;
            Port = 0;
            IsHost = false;
        }
    }
}
