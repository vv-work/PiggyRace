# PiggyRace — Roadmap / TODO

This file tracks the implementation phases and tasks. See README for architecture and design.

## Phase 0 — Project Plumbing
- [ ] Add packages: NGO, Unity Transport, Input System (verify), Cinemachine (optional).
- [ ] Create scenes: `MainMenu`, `Lobby`, `Race` with minimal UI placeholders.
- [ ] Add `NetworkManager` prefab with UTP configuration.
- [ ] Implement `GameBootstrap` and basic scene loading with CLI args for host/client.

## Phase 1 — Core Movement & Camera
- [ ] Implement `PigMotor` with throttle/steer/drift/boost curves.
- [ ] Implement `PigController` to translate inputs → motor commands.
- [ ] Add `ChaseCamera` or Cinemachine follow rig; tune FOV, camera lag.
- [ ] Create `PigPlayer` prefab (mesh placeholder + `NetworkObject`).

## Phase 2 — Netcode Foundations
- [ ] `TickManager` + `TimeSync` (client/server RTT, offset smoothing).
- [ ] `PlayerInputBuffer` (client) and input send path (RPC or custom message).
- [ ] Authoritative server simulation of `PigMotor`.
- [ ] `NetPigState` serializer + server snapshot broadcast at 15–20 Hz.
- [ ] Client prediction + reconciliation for owned pig.
- [ ] Snapshot interpolation for other players using `SnapshotBuffer`.

## Phase 3 — Race Systems
- [ ] `TrackManager` with ordered checkpoints, `LapTracker` with sector timing.
- [ ] Spawn grid, `PlayerSpawner`, and start positions.
- [ ] `NetworkGameManager` for phases: Lobby → Countdown → Race → Results.
- [ ] HUD: countdown, lap, position, deltas; basic minimap.

## Phase 4 — Polish & Resilience
- [ ] Anti-cheat validations (speed, boost economy, checkpoint order).
- [ ] Spectator mode when eliminated or on join-late.
- [ ] Pause/Resume handling for host; rejoin with state sync.
- [ ] NetSim testing: latency/jitter/loss; tune buffers and rates.

## Phase 5 — Online Services (Optional)
- [ ] Unity Relay integration for NAT traversal.
- [ ] Unity Lobby for room discovery and ready flow.
- [ ] Matchmaking queue (stretch goal).

## Content & Nice-to-haves
- [ ] Items and interactions (speed pads, oil slicks, bash stun).
- [ ] Ghost racing (best lap or world record).
- [ ] Photo mode / kill cam replays.

