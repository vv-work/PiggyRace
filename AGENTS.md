# Repository Guidelines

## Project Structure & Module Organization
- Source: `Assets/Scripts/` organized by domain (`Core/`, `Netcode/`, `Gameplay/`, `UI/`).
- Tests: `Assets/Tests/` with `EditMode/` and `PlayMode/` assemblies.
- Packages: `Packages/manifest.json` (Unity 6.2, NGO 2.5.0, UTP).
- Scenes/Assets: `Assets/Scenes/`, `Assets/Prefabs/`, `Assets/Settings/`.

## Build, Test, and Development Commands
- Open locally: Unity 6.2 (URP). Use the editor to play and run tests.
- Run EditMode tests (CLI):
  - `unity -batchmode -projectPath . -runTests -testPlatform editmode -testResults results-editmode.xml -quit`
- Run PlayMode tests (CLI):
  - `unity -batchmode -projectPath . -runTests -testPlatform playmode -testResults results-playmode.xml -quit`
- Optional coverage (if enabled): add `-enableCodeCoverage -coverageResultsPath Coverage/`.

## Coding Style & Naming Conventions
- Language: C# (Unity). Indent with 4 spaces, UTF-8, LF endings.
- Namespaces: `PiggyRace.*` (e.g., `PiggyRace.Core.Tick`).
- Types/Methods: PascalCase; fields: camelCase; constants: UPPER_CASE.
- Files: one public type per file, file name matches type.
- Avoid magic numbers; prefer serialized fields or `GameSettings`.

## Testing Guidelines
- Framework: Unity Test Framework (NUnit).
- Structure: `ClassNameTests.cs` in `EditMode/` for pure logic; `PlayMode/` for scene/runtime.
- Expectations: Add tests for new logic (tick/time sync, serialization, interpolation, lap logic).
- Run before PR: Both EditMode and PlayMode must pass.

## Commit & Pull Request Guidelines
- Commits: Conventional style preferred (e.g., `feat(netcode): add input packing`).
- PRs: Clear description, linked issues, test results summary, and relevant screenshots/gifs (race HUD, profiler graphs).
- Scope: Keep PRs focused (one feature/system). Include migration notes if packages or assets change.

## Security & Configuration Tips
- Netcode: Project targets NGO 2.5.0; ensure Unity Transport is compatible.
- Determinism: Keep physics params consistent across client/server; avoid frame-rate dependent code.
- Assets: Large binaries and generated folders (Library/, Temp/) are ignored via `.gitignore`.

