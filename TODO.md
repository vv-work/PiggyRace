# PiggyRace — Step‑by‑Step TODO (NGO)

Shared checklist for building a Netcode for GameObjects (NGO) game. Each item is small, actionable, and marked for Agent or You.

## Phase 0 — Preflight & Packages
- [x] You: Install via Package Manager: Netcode for GameObjects 2.5.0, Unity Transport (compatible), Input System, Cinemachine.
- [x] You: Project Settings → Player → Active Input Handling = Input System Package.
- [x] You: Project Settings → Time → Fixed Timestep = 0.0167.
- [x] You: Create scenes `MainMenu`, `Lobby`, `Race` (placeholders) in `Assets/Scenes`.
- [x] You: Create `Assets/Prefabs/Systems/NetworkManager` prefab and configure Unity Transport (address/port).
- [x] Agent: Create folders `Assets/Scripts/{Core,Gameplay,UI,Networking}`.
- [x] Agent: Maintain `PiggyRace.Runtime.asmdef` references (Unity.InputSystem; add NGO refs when code requires it).

## Phase 1 — Local Movement (Offline)
- [x] Agent: Removed legacy Tractor prototype and tests.
- [x] Agent: Added `PigMotor` + `PigController` (offline) and `PigMotorTests`.
- [x] You: Drop `PigController` in a test scene; verify controls; tweak tuning in Inspector.
- [x] You: Add Cinemachine Brain (Main Camera) and a Virtual Camera; set Follow/LookAt to the pig.
- [x] You: Validate local pig feel on a flat test track; adjust Inspector fields (incl. `RotationSpeedDeg`).

## Phase 2 — NGO Foundations
- [x] Agent: Create scripts `Networking/NetworkGameManager : NetworkBehaviour` (Lobby → Countdown → Race → Results) and `Networking/PlayerConnection : NetworkBehaviour` (join/leave/ownership).
- [x] Agent: Add `Networking/NetworkBootstrap` helper for UI buttons (Start Host/Client/Server, Shutdown).
 - [x] You: Create `Prefabs/PigPlayer` and add `NetworkObject` (+ `NetworkTransform` if used); assign visuals.
 - [x] You: Place `NetworkManager` prefab in a bootstrap scene; configure Transport (IP/Relay); set player prefab reference.
 - [x] You: Add temporary UI (or keyboard shortcuts) to Start Host/Client/Server, wired in Inspector to `NetworkBootstrap` methods.
 - [x] You: Run Host + Client (second build or play-in-editor client) and confirm connection.

## Phase 3 — Networked Movement
- [x] Agent: Implement `Networking/NetworkPig` (owner input via `ServerRpc`, server simulation with `PigMotor`, NetworkVariables for pos/yaw).
- [x] Agent: Add client-side smoothing for remotes and light reconciliation for owners.
- [x] Agent: Add client prediction buffers with ticked inputs and targeted owner reconciliation snapshots from server.
- [x] Agent: Add `PigMotor` Snapshot capture/restore and unit tests for roundtrip determinism.
- [x] You: Add `NetworkPig` to the PigPlayer prefab (remove/disable `PigController` to avoid double-move).
- [x] You: Configure Network Simulator (latency/jitter/loss) and test; report jitter or rubber‑banding.
- [x] You: Adjust Inspector tuning (smoothing, rotation speed, max speed) as advised; re-test.

## Phase 4 — Track & Race Loop (TDD First)
- [x] Agent: Add pure `LapTrackerLogic` with unit tests (EditMode).
- [x] Agent: `TrackManager` + `Checkpoint` components (ordered, auto-indexing).
- [x] Agent: `LapTracker` (server-side) using logic; expose `CurrentLap`, `NextCheckpoint`, `Finished` as `NetworkVariable`s.
- [x] Agent: Spawn grid + `PlayerSpawner` (positions players on Countdown).
- [x] Agent: Gate `NetworkPig` input by `RacePhase` (no driving before race).
- [x] Agent: Basic HUD bindings (laps/phase/time) — minimal TMP readout.
- [x] Agent: PlayMode smoke test for checkpoint trigger wiring (optional if flaky in CI).
- [x] You: Build a simple track, place checkpoints as children of a `TrackManager`, set lap count and spawn points.
- [x] You: Run Host + Client; start countdown; confirm spawn, lap counting, and race finish flag.

## Phase 5 — Resilience & Polish
- [x] Agent: Anti‑cheat validations (max speed/yaw rate clamping for client‑auth; checkpoint order already enforced by logic).
- [x] Agent: Rejoin/spectate when a client reconnects mid‑race (late joiners flagged as spectators; input disabled until next race).
- [x] Agent: Tune buffers/rates — throttle owner snapshots (configurable `SnapshotRateHz`).
- [x] Agent: Add unit tests for movement validation (`MovementValidatorTests`).
- [x] You: Validate stability under simulated network conditions; adjust Inspector parameters (e.g., `SnapshotRateHz`, reconcile thresholds) and confirm improvements.

## Optional — Client-Authoritative Mode
- [x] Agent: Add `Authority` toggle on `NetworkPig` (ServerAuthoritative | ClientAuthoritative).
- [x] Agent: Client-authoritative owner simulates locally, pushes state to server via RPC; server echoes to remotes.
- [x] You: Use this only for demos/prototyping; switch back to server-authoritative before shipping.

## Testing Guidelines (Expanded)
- Prefer EditMode tests for rules: checkpoint order, lap completion, sector time accumulation, race finish conditions.
- Keep MonoBehaviours thin; where needed, add public methods/events to allow PlayMode tests to drive behavior without physics.
- Name tests `ClassNameTests.cs` and keep them focused and deterministic (no real time dependence beyond passed timestamps).

## Phase 6 — Online Services (Unified + Relay Fallback)
- [x] Agent: Add unified join‑code flow stubs (`MultiplayerServicesConnector`) and Relay fallback (`RelayLobbyService` + `RelayLobbyUI`).
- [ ] You: Configure UGS project; Preferred: install `com.unity.services.multiplayer`. Fallback: install `com.unity.services.relay` + `com.unity.services.authentication`.
- [ ] Agent: Wire `MultiplayerServicesConnector.InitializeAsync/CreateSessionAsync/JoinSessionAsync` to the new Multiplayer Services APIs and configure `UnityTransport` endpoints.
- [ ] You: Validate Host/Join with Multiplayer (if available). If not, validate Relay flow end‑to‑end (join code connect across networks).
- [ ] Agent: Document package versions used and any environment config needed (Services environment, project ID) in README.

## Phase 7 — Stretch Content
- [ ] Items (pads, oil, bash); ghosts/replays; photo mode.

## Optional Improvements (Backlog)
- [ ] Remote interpolation buffer with time offset for even smoother visuals.
- [ ] Cache manager references to avoid `FindObjectOfType` in hot paths.
- [ ] PlayMode smoke tests for spectator/rejoin flows.
- [ ] Prefab a ready‑to‑use online UI (host/join panel).
