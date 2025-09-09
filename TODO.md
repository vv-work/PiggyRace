# PiggyRace — Step-by-Step Plan (Agent + You)

Shared checklist for building a Netcode for GameObjects (NGO) game together. Each step lists what the Agent patches and what You validate in Unity.

## 0) Project Plumbing
- Agent: ensure packages (NGO 2.5.0, Unity Transport, Input System) in `Packages/manifest.json`; add `NetworkManager` prefab and config; create scenes `MainMenu`, `Lobby`, `Race`; simple `GameBootstrap`.
- You: open with Unity 6.2, import packages, assign bootstrap scene, press Play to smoke test.
- Done when: editor opens cleanly and playmode runs without errors.

## 1) Core Movement & Camera
- Agent: implement `PigMotor` + `PigController` (pure logic emphasized); add tests; add follow camera or Cinemachine rig.
- You: wire `PigPlayer` prefab with `NetworkObject`; tweak curves and camera; validate feel on a graybox.
- Done when: local pig drives stably at 60 Hz.

## 2) Netcode Foundations
- Agent: `TickManager` + `TimeSync`; input send via `ServerRpc`; server-authoritative motor; snapshot broadcast (15–20 Hz); client prediction + reconciliation; interpolation buffer for remotes; tests for serialization/interp.
- You: run Host + Client; test at 80–120 ms RTT using network simulator; note jitter/rubber-banding.
- Done when: remote pigs are smooth; owned pig is responsive.

## 3) Race Loop
- Agent: `TrackManager` (ordered checkpoints) + `LapTracker`; grid spawn; `NetworkGameManager` (Lobby → Countdown → Race → Results); HUD counters.
- You: place checkpoints on a test track; verify laps and results across clients.
- Done when: a full race works end-to-end for 2+ players.

## 4) Resilience & Polish
- Agent: anti-cheat validations, rejoin/spectate flows, parameter tuning under net-sim; more tests.
- You: test with packet loss/jitter profiles; approve tuning.
- Done when: stable under common network conditions.

## 5) Online Services (Optional)
- Agent: add Relay + Lobby, minimal join UI.
- You: configure UGS project IDs/keys; validate cross-network join.
- Done when: clients can discover/join via Lobby/Relay.

## Stretch Content
- Items (pads, oil, bash), ghosts/replays, photo mode.
