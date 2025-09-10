UI scripts live here (HUD, menus, lobby UI).

Combined control
- Use `NetworkHubUI` to drive both UGS Multiplayer/Relay (via `RelayLobbyService`) and plain NGO (via `NetworkBootstrap`) from a single panel.
- Assign `Join Code Input` and `Status Text` (TMP) for convenience.
- Wire buttons:
  - UGS: `InitializeUGS`, `HostUGS`, `JoinUGS`
  - Direct NGO: `HostDirect`, `ClientDirect`, `ServerDirect`, `Shutdown`
  - Race: `StartCountdown`, `AbortToLobby`, `ShowResults`
