# PiggyRace — Step‑by‑Step TODO (NGO)

Shared checklist for building a Netcode for GameObjects (NGO) game. Each item is small, actionable, and marked for Agent or You.

## Phace 0 — Preflight & Packages
- [x] You: Install via Package Manager: Netcode for GameObjects 2.5.0, Unity Transport (compatible), Input System, Cinemachine.
- [x] You: Project Settings → Player → Active Input Handling = Input System Package.
- [x] You: Project Settings → Time → Fixed Timestep = 0.0167.
- [x] You: Create scenes `MainMenu`, `Lobby`, `Race` (placeholders) in `Assets/Scenes`.
- [x] You: Create `Assets/Prefabs/Systems/NetworkManager` prefab and configure Unity Transport (address/port).
- [x] Agent: Create folders `Assets/Scripts/{Core,Gameplay,UI,Networking}`.
- [x] Agent: Maintain `PiggyRace.Runtime.asmdef` references (Unity.InputSystem; add NGO refs when code requires it).

## Phace 1 — Local Movement (Offline)
- [x] Agent: Removed legacy Tractor prototype and tests.
- [x] Agent: Added `PigMotor` + `PigController` (offline) and `PigMotorTests`.
- [ ] You: Drop `PigController` in a test scene; verify controls; tweak tuning in Inspector.
- [ ] You: Add Cinemachine Brain (Main Camera) and a Virtual Camera; set Follow/LookAt to the pig.
- [ ] You: Validate local pig feel on a flat test track; adjust Inspector fields (incl. `RotationSpeedDeg`).

## Phace 2 — NGO Foundations
- [ ] Agent: Create scripts `Networking/NetworkGameManager : NetworkBehaviour` (Lobby → Countdown → Race → Results) and `Networking/PlayerConnection : NetworkBehaviour` (join/leave/ownership).
- [x] You: Create `Prefabs/PigPlayer` and add `NetworkObject` (+ `NetworkTransform` if used); assign visuals.
- [ ] You: Place `NetworkManager` prefab in a bootstrap scene; configure Transport (IP/Relay); set player prefab reference.
- [ ] You: Add temporary UI (or keyboard shortcuts) to Start Host/Client/Server, wired in Inspector to NetworkManager.
- [ ] You: Run Host + Client (second build or play-in-editor client) and confirm connection.

## Phace 3 — Networked Movement
- [ ] Agent: Send input via `ServerRpc` from owner to server (quantized values).
- [ ] Agent: Simulate pig on server (authoritative); replicate to clients (NetworkVariable/RPC).
- [ ] Agent: Add basic client-side smoothing/prediction for owned pig.
- [ ] You: Configure Network Simulator (latency/jitter/loss) and test; report jitter or rubber‑banding.
- [ ] You: Adjust Inspector tuning (smoothing buffers, max speed) as advised; re-test.

## Phace 4 — Track & Race Loop
- [ ] Agent: `TrackManager` with ordered checkpoints and triggers.
- [ ] Agent: `LapTracker` (laps, sector times) and HUD bindings.
- [ ] Agent: Spawn grid + `PlayerSpawner`.
- [ ] Agent: Countdown, race timer, results UI; sync state via `NetworkVariable`s.
- [ ] You: Build a simple track, place and index checkpoints, set lap count in Inspector, run a 2‑player race end‑to‑end.

## Phace 5 — Resilience & Polish
- [ ] Agent: Anti‑cheat validations (max speed/boost, checkpoint order).
- [ ] Agent: Rejoin/spectate when a client reconnects mid‑race.
- [ ] Agent: Tune buffers/rates under latency/jitter/loss; add tests where possible.
- [ ] You: Validate stability under simulated network conditions; adjust Inspector parameters (e.g., interpolation delay) and confirm improvements.

## Phace 6 — Online Services (Optional)
- [ ] Agent: Add Unity Relay + Lobby and simple join UI.
- [ ] You: Configure UGS IDs/keys; verify cross‑network joining works.

## Phace 7 — Stretch Content
- [ ] Items (pads, oil, bash); ghosts/replays; photo mode.
