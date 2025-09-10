# PiggyRace

![PiggyRace gif](./img/PigRacer.webp)

Arcade online racing where players ride boosty pigs around fun tracks. This repository is Netcode for GameObjects (NGO)–based: Unity 6.2 (URP) + NGO 2.5.0 with a server‑authoritative model, client prediction, and interpolation. UI uses TextMeshPro (TMP).

Current status (snapshot)
- Physics‑correct pig control with server authority and client prediction/reconciliation.
- TDD race loop: checkpoints, lap tracking, countdown, spawns, finish state.
- Editor track tools: quick loop generation and spawn grid.
- Minimal TMP HUD: phase, countdown, lap/total, checkpoint progress + arrow to next checkpoint.
- Online services scaffold: unified Multiplayer Services flow with Relay fallback; simple host/join UI.

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

Key scripts (implemented)
- Networking: `Assets/Scripts/Networking/NetworkPig.cs`, `PlayerSpawner.cs`, `MovementValidator.cs`.
- Race: `Assets/Scripts/Gameplay/Race/{TrackManager.cs,Checkpoint.cs,LapTracker.cs,LapTrackerLogic.cs}`.
- UI: `Assets/Scripts/UI/RaceHUD.cs`.
- UGS glue: `Assets/Scripts/Networking/UGS/{RelayLobbyService.cs,RelayLobbyUI.cs,MultiplayerServicesConnector.cs}`.

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
- Optional client-authoritative mode (per-pig) for extra-smooth local feel: owner simulates locally and sends state to host, which echoes to remotes. Use only for prototypes — not secure against cheating.
- Optional: Unity Relay + Lobby for discovery and NAT traversal.

## Development Setup
- Unity 6.2 with NGO 2.5.0 and Unity Transport. Input System enabled (already present). UI uses TextMeshPro (TMP).
- Editor: Fixed Timestep 0.0167s (60 Hz). Avoid frame-dependent logic in simulation.
- Tests: Window → General → Test Runner → Run All (or CLI flags in AGENTS.md).

Packages to import (minimum)
- `com.unity.inputsystem` (Input System)
- `com.unity.netcode.gameobjects` (NGO 2.5.0)
- `com.unity.transport` (UTP)
- `com.unity.textmeshpro`
- Optional online: `com.unity.services.multiplayer` (unified), or `com.unity.services.relay` (+ `com.unity.services.authentication`)

Scenes/Prefabs
- `Assets/Prefabs/Systems/NetworkManager` with `UnityTransport`
- Scenes: `MainMenu`, `Lobby`, `Race` (you can start with a single test scene)

## Step-by-Step Milestones
1) Plumbing: add packages, NetworkManager prefab, scenes (MainMenu, Lobby, Race).
2) Movement: `PigMotor` + `PigController` + follow camera.
3) Netcode core (NGO): ticks/time sync, input via `ServerRpc`, server sim on host, state sync via `NetworkVariable`/RPC, prediction + reconciliation on client, smoothing for remotes.
4) Race loop (TDD): checkpoints, lap tracker, spawns, countdown, HUD, results — with EditMode tests for lap/sector logic and minimal PlayMode smoke checks.
5) Resilience/polish: anti-cheat checks, rejoin/spectate, net-sim tuning.
6) Online services: unified Multiplayer Services (preferred) with join code flow and Relay fallback. Simple host/join UI included.

See TODO.md for an actionable, checkpointed plan with “Agent does / You do” for each step.

## Contribution
- Small PRs, each with passing EditMode + PlayMode tests.
- Describe behavioral changes; attach screenshots/gifs for gameplay/HUD changes.

License: TBD

## Tools
- Input: Unity Input System
- UI: TextMeshPro (TMP)
- Camera: Cinemachine (optional)
 - Editor: Track Tools (Tools → PiggyRace → Track Tools) to quickly generate checkpoint loops and spawn grids.

## Testing Approach (TDD)
- Keep core rules in pure C# classes (no MonoBehaviour) and cover with EditMode tests. Example: `LapTrackerLogic` verifies ordered checkpoints, lap completion, final race state, and sector timings.
- Keep runtime glue thin (MonoBehaviours) and add light PlayMode tests where needed to validate wiring.
- Prefer server-authoritative code paths in tests: simulate server-side updates and assert NetworkVariables mirror expected state where feasible.
- Physics + prediction: `PigMotor` exposes a `Snapshot` with capture/restore for deterministic rewind/replay. EditMode tests cover snapshot/restore roundtrips to support reconciliation.

## Editor Track Tools
- Open: `Tools → PiggyRace → Track Tools`.
- Create Track Loop: specify checkpoint count, ellipse radii, start angle, and checkpoint trigger size; click "Create TrackManager + Loop" to generate a `TrackManager` with ordered `Checkpoint` children.
- Spawn Grid: set count, columns, row/column spacing; click "Add/Replace Spawn Points At Start" to populate spawn points behind checkpoint 0, aligned with its forward.
- Quick action: `Tools → PiggyRace → Create Track Loop (Quick)` uses defaults to create a loop fast.

## Minimal Race HUD
- Component: `PiggyRace.UI.RaceHUD`
- Shows: `Phase`, `Countdown`, and local `Lap/Total` using a TMP Text.
- How to use:
  - Create a Canvas → TextMeshPro - Text (UI) in your scene.
  - Add `RaceHUD` to the TMP object and assign its `TMP_Text` field (or let it auto-grab).
  - Ensure your player prefab has `LapTracker` and the scene has `NetworkGameManager`.


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


## Online Services (Multiplayer + Relay)
This project includes a unified join‑code flow for online play. It prefers the new Multiplayer Services package and cleanly falls back to Relay when Multiplayer is not installed.

Packages
- Preferred (unified): `com.unity.services.multiplayer`
- Fallback (legacy): `com.unity.services.relay` and `com.unity.services.authentication`

Compilation guards
- Code paths for Multiplayer/Relay are wrapped in defines (e.g., `UGS_MULTIPLAYER`, `UGS_RELAY`). When the packages are absent, the scripts compile but UI will show an info message.

Scene wiring
- Add `RelayLobbyService` (any GameObject).
- Add `RelayLobbyUI` to your existing network UI and wire fields:
  - `Join Code Input` (TMP_InputField), `Status Text` (TMP_Text), and optional `Max Connections`.
  - Buttons: bind to `InitializeUGS`, `HostWithRelay`, and `JoinWithRelay`.

Usage
1) Click Initialize UGS (once per run). It initializes Services and anonymous auth.
2) Host: with Multiplayer installed, clicking Host creates a session, outputs a join code, and automatically starts Host via NGO. Without Multiplayer, Relay allocation is used and Host starts as before.
3) Join: paste the join code and Start Client.

Security
- Client-authoritative mode is for prototypes. Use server-authoritative with prediction + reconciliation for shipping.

UGS Setup (one-time per project)
1) Open Unity → Window → Services. Create or link a UGS project for your game.
2) Preferred: install `com.unity.services.multiplayer` (join‑code sessions). If not available, install `com.unity.services.relay` + `com.unity.services.authentication` for fallback.
3) Enable the services you installed in the UGS dashboard (Authentication for Relay; Multiplayer for unified).
4) In Unity, ensure `NetworkManager` uses `UnityTransport` on your bootstrap object.
5) Defines are handled in code/asmdef; when packages are absent, UI prints clear messages and skips network allocation.

Inspector tuning (quick reference)
- `NetworkPig`: `Authority` (ServerAuthoritative | ClientAuthoritative), `SnapshotRateHz`, `RemoteLerpRate`, `OwnerReconcileThreshold`.
- `MovementValidator`: tune max speed/yaw limits in `NetworkPig` fields that call it.
- `LapTracker`: assign `TrackManager`, laps, and optional arrow transform for next‑checkpoint indicator.

Scene bootstrap (ensuring players spawn and race scene loads)
- Add `AutoPlayerSpawner` to any persistent object in your starting scene. It ensures a player object is spawned for each client if NGO doesn’t auto‑spawn.
- Add `NetworkSceneBootstrap` to your starting scene and set `Scene Name` to `Race`. When the server/host starts, it loads the Race scene via NGO SceneManager so that `NetworkGameManager`, `TrackManager`, and checkpoints exist for all peers.
