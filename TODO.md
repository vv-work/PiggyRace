# PiggyRace — Step‑by‑Step TODO (NGO)

Shared checklist for building a Netcode for GameObjects (NGO) game. Each item is small, actionable, and marked for Agent or You.

## Phace 0 — Preflight & Packages
- [ ] Agent: Confirm Unity 6.2, NGO 2.5.0, Input System in `Packages/manifest.json`.
- [ ] Agent: Bump Unity Transport to a compatible version if Unity requests it.
- [ ] Agent: Create folders `Assets/Scripts/{Core,Gameplay,UI,Networking}`.
- [ ] Agent: Add `PiggyRace.Runtime.asmdef` refs (Unity.InputSystem; add NGO refs when used).
- [ ] Agent: Add `NetworkManager` prefab (UTP configured) in `Assets/Prefabs/Systems`.
- [ ] Agent: Create scenes `MainMenu`, `Lobby`, `Race` (placeholders) in `Assets/Scenes`.
- [ ] You: Open project, let packages import, press Play on an empty scene to smoke test.

## Phace 1 — Local Movement (Offline)
- [ ] Agent: Implement `TractorModel` (done) and `TractorMotor` (done) with tests.
- [ ] You: Drop `TractorMotor` in a scene; verify WASD + Space works; tweak tuning.
- [ ] Agent: Scaffold `PigMotor` + `PigController` (offline) and EditMode tests.
- [ ] You: Validate local pig feel on a flat test track.

## Phace 2 — NGO Foundations
- [ ] Agent: Create `Networking/NetworkGameManager : NetworkBehaviour` (lifecycle: Lobby → Countdown → Race → Results).
- [ ] Agent: Create `Networking/PlayerConnection : NetworkBehaviour` (join/leave, ownership).
- [ ] Agent: Create `Prefabs/PigPlayer` with `NetworkObject` (+ `NetworkTransform` or custom sync).
- [ ] Agent: Wire Host/Client buttons in a temporary UI (or keyboard shortcuts) to start/stop `NetworkManager`.
- [ ] You: Run Host + Client (second build or play-in-editor client) and connect.

## Phace 3 — Networked Movement
- [ ] Agent: Send input via `ServerRpc` from owner to server (quantized values).
- [ ] Agent: Simulate pig on server (authoritative); replicate to clients (NetworkVariable/RPC).
- [ ] Agent: Add basic client-side smoothing/prediction for owned pig.
- [ ] You: Test at 80–120 ms RTT in Net Simulator; report jitter or rubber‑banding.

## Phace 4 — Track & Race Loop
- [ ] Agent: `TrackManager` with ordered checkpoints and triggers.
- [ ] Agent: `LapTracker` (laps, sector times) and HUD bindings.
- [ ] Agent: Spawn grid + `PlayerSpawner`.
- [ ] Agent: Countdown, race timer, results UI; sync state via `NetworkVariable`s.
- [ ] You: Build a simple track, place checkpoints, run a 2‑player race end‑to‑end.

## Phace 5 — Resilience & Polish
- [ ] Agent: Anti‑cheat validations (max speed/boost, checkpoint order).
- [ ] Agent: Rejoin/spectate when a client reconnects mid‑race.
- [ ] Agent: Tune buffers/rates under latency/jitter/loss; add tests where possible.
- [ ] You: Validate stability under simulated network conditions.

## Phace 6 — Online Services (Optional)
- [ ] Agent: Add Unity Relay + Lobby and simple join UI.
- [ ] You: Configure UGS IDs/keys; verify cross‑network joining works.

## Phace 7 — Stretch Content
- [ ] Items (pads, oil, bash); ghosts/replays; photo mode.
