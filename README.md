# PiggyRace

Arcade online racing where players ride boosty pigs around fun tracks. This repository is Netcode for GameObjects (NGO)–based: Unity 6.2 (URP) + NGO 2.5.0 with a server‑authoritative model, client prediction, and interpolation.

---

## How We Build This Together (You + Agent)
- Agent: scaffolds folders/scripts, writes tests first, applies small patches, explains changes.
- You: open Unity, run tests, press Play, give feedback, approve package/scene changes.
- Loop per step: propose → patch → you validate in Unity → iterate. The detailed step list lives in TODO.md.

## Project Structure
- Code: `Assets/Scripts/` by domain (`Core/`, `Gameplay/`, `UI/`).
- Tests: `Assets/Tests/EditMode` (pure logic) and `Assets/Tests/PlayMode` (runtime/scene).
- Scenes/Prefabs: `Assets/Scenes`, `Assets/Prefabs`, `Assets/Settings`.
- Packages: `Packages/manifest.json` (Unity 6.2, NGO 2.5.0, UTP).

Planned networking scripts will live under `Assets/Scripts/Networking/` (e.g., `NetworkGameManager`, `PlayerConnection`, `Spawning`).

## Architecture Snapshot
```mermaid
flowchart LR
  subgraph Clients
    C1[Client A<br/>Input + Prediction]
    C2[Client B<br/>Input + Prediction]
    Cn[Client N<br/>Input + Prediction]
  end

  subgraph Server[Host/Server]
    NM[NetworkManager]
    GSM[NetworkGameManager<br/>Match State]
    SIM[Authoritative Simulation<br/>Pig Physics]
    VAL[Validation<br/>Anti-Cheat Checks]
    SS[Snapshot Builder]
  end

  C1 -- Inputs (ticks) --> NM
  C2 -- Inputs (ticks) --> NM
  Cn -- Inputs (ticks) --> NM

  NM -- Snapshots (20 Hz) --> C1
  NM -- Snapshots (20 Hz) --> C2
  NM -- Snapshots (20 Hz) --> Cn

  C1 <-- Time Sync --> NM
  C2 <-- Time Sync --> NM
  Cn <-- Time Sync --> NM
```

Key principles: server authoritative simulation; client-side prediction for local pig; reconciliation on server snapshots; interpolation for remote pigs; fixed-timestep motor for stability.

## NGO Usage (What we rely on)
- `NetworkManager` prefab configured with Unity Transport (UTP).
- `NetworkBehaviour` components for player controllers and the race game manager.
- RPCs (`ServerRpc`/`ClientRpc`) for input submission and events.
- `NetworkVariable<T>` for shared race state (phase, countdown, lap totals).
- `NetworkTransform` or custom transform sync + smoothing for remote players.
- Optional: Unity Relay + Lobby for discovery and NAT traversal.

## Development Setup
- Unity 6.2 with NGO 2.5.0 and Unity Transport. Input System enabled (already present).
- Editor: Fixed Timestep 0.0167s (60 Hz). Avoid frame-dependent logic in simulation.
- Tests: Window → General → Test Runner → Run All (or CLI flags in AGENTS.md).

## Step-by-Step Milestones
1) Plumbing: add packages, NetworkManager prefab, scenes (MainMenu, Lobby, Race).
2) Movement: `PigMotor` + `PigController` + follow camera.
3) Netcode core (NGO): ticks/time sync, input via `ServerRpc`, server sim on host, state sync via `NetworkVariable`/RPC, prediction + reconciliation on client, smoothing for remotes.
4) Race loop: checkpoints, lap tracker, spawns, countdown, HUD, results.
5) Resilience/polish: anti-cheat checks, rejoin/spectate, net-sim tuning.

See TODO.md for an actionable, checkpointed plan with “Agent does / You do” for each step.

## Contribution
- Small PRs, each with passing EditMode + PlayMode tests.
- Describe behavioral changes; attach screenshots/gifs for gameplay/HUD changes.

License: TBD

---

## Local Pig Prototype (offline)
- Components: `PigController` (MonoBehaviour) + `PigMotor` (pure logic).
- Purpose: quick local driving to test feel before wiring NGO.

How to use in a scene
- Create an empty GameObject named `Pig` and add `PigController`.
- Optional: add a kinematic `Rigidbody` to use physics transforms.
- Controls: W/S (throttle), A/D (steer), Space (brake), Shift (drift), Ctrl (boost).
- Tune fields in Inspector: speed/accel/drag, turn/ drift multipliers, boost, and `RotationSpeedDeg` for visual turn smoothing.

Tests
- EditMode: `PigMotorTests` validate acceleration/turning, drift turn rate, boost effect.
