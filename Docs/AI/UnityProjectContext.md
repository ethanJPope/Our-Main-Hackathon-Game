# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `D:\UnityProjects\MainHackathonGame\Main Hackathon Game`
- Last analyzed: 2026-08-18
- Last analyzed commit: `2e3a9d5` (`Initial check-in`)

## Confirmed Environment

- Unity version: `6000.5.8f1` (Unity 6)
- Render pipeline: High Definition Render Pipeline (HDRP)
- Input system: Unity Input System package `1.20.0`
- Current Editor target: Windows/Standalone so HDRP renders correctly while Unity Remote streams to Android; mobile remains the intended shipping target.

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| AI/MCP | Unity AI Assistant `2.18.0-pre.2` is installed; the official Unity relay exists on this machine. | Confirmed | `Packages/manifest.json`, `Packages/packages-lock.json`, local Unity relay |
| Input | Unity Input System `1.20.0` is installed. | Confirmed | `Packages/manifest.json` |
| UI | Unity uGUI `2.5.0` is installed. | Confirmed | `Packages/manifest.json` |
| Rendering | HDRP `17.5.0` is installed. | Confirmed | `Packages/manifest.json`, `ProjectSettings/HDRPProjectSettings.asset` |
| Camera | Cinemachine `3.1.7` is installed and reserved for the isometric camera rig and orbit behavior. | Confirmed | `Packages/manifest.json`, `Packages/packages-lock.json` |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/OutdoorsScene.unity` | Current prototype scene and only enabled build scene; now contains the first mobile-controls hierarchy. | Confirmed | `ProjectSettings/EditorBuildSettings.asset`, MCP hierarchy verification |
| `Assets/InputSystem_Actions.inputactions` | Existing Player and UI action maps, including Move and Look. | Confirmed | Input Actions asset |
| `Assets/Settings/` | HDRP and sky/fog settings assets. | Confirmed | Asset inventory |
| `Docs/AI/` | AI-facing project context and future architecture notes. | Confirmed | This document |
| `Assets/Scripts/` | Runtime control scripts for camera swipe, virtual joystick, and player movement. | Confirmed | Asset inventory |

## Assembly Boundaries

- No first-party `.asmdef` or `.asmref` files were found.
- Runtime scripts currently compile through Unity's default `Assembly-CSharp` assembly.

## Scenes And Startup Flow

- Build scenes: `Assets/OutdoorsScene.unity` is enabled.
- Likely startup scene: `Assets/OutdoorsScene.unity`.
- Scene loading flow: no custom loader or bootstrap scene has been identified.
- Existing template objects include `Sun`, `Sky and Fog Volume`, `Main Camera`, and `StaticLightingSky`.
- Prototype hierarchy added through the official Unity MCP: `GameRoot/PlayerRoot/PlayerVisual`, `GameRoot/CameraRig/Main Camera`, `GameRoot/World/PrototypeGround`, and `GameRoot/MobileHUD`.
- Cinemachine camera foundation added through the official Unity MCP: `PlayerRoot/CameraTarget` follows the player at local position `(0, 1, 0)`; `CameraRig/Main Camera` has `CinemachineBrain`; and `CameraRig/Cinemachine Camera` uses `CinemachineOrbitalFollow` plus `CinemachineRotationComposer` with both `Follow` and `Look At` set to `CameraTarget`. The initial isometric framing is radius `12`, elevation `35` degrees, and heading `45` degrees.
- `Assets/Scripts/CameraOrbitSwipe.cs` temporarily uses legacy `Input.GetTouch` and mouse input to drive `CinemachineOrbitalFollow.HorizontalAxis`. A swipe must begin outside the left-side control area and not over UI, allowing simultaneous joystick movement and camera rotation.
- Active Input Handling is set to `Both`, but the prototype mobile controls currently read the legacy input stream because that is the stream verified through Unity Remote on this Editor setup. Migration back to the new Input System is intentionally deferred. The active Editor target remains Windows because the current HDRP project cannot render as an Android target.
- Android phone testing is configured through Unity Remote using `Any Android Device`, `JPEG`, `Normal`, and `Remote`. The currently verified device is a Moto G 5G (2024), and the Unity Remote ADB connection was confirmed working.
- `GameRoot/MobileHUD/HotbarRoot` now contains eight non-interactive `Image` slots (`Slot_01` through `Slot_08`) in a bottom-center eight-column grid. No hotbar logic or `Button` components are present.
- `GameRoot/MobileHUD/JoystickRoot` contains layered circular artwork and a movable handle. `MobileJoystick` polls legacy touch input directly, keeps one finger assigned to the joystick, and also supports mouse testing in the Editor.
- `GameRoot/MobileHUD/DodgeButtonRoot` is a lower-right circular dodge control. `MobileDodgeButton` polls legacy touch input directly so it works through the current Unity Remote setup without relying on the Input System UI module.
- `PlayerRoot` has a `CharacterController` and `PlayerJoystickMovement`. Movement is camera-relative, applies gravity, and smoothly turns the player toward the joystick direction. A dodge moves up to `4` world units along the player's current facing direction over `0.18` seconds with a `0.65` second cooldown; these values are serialized for tuning. The primitive collider on `PlayerVisual` is disabled to avoid duplicate collision geometry.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Unity component composition | New project has no first-party gameplay architecture yet. | Likely | Asset and script inventory |
| Input handling | Mobile movement and camera swipe temporarily poll legacy touches directly. Each control tracks its own finger ID, so joystick movement and camera swipe can occur together. | Confirmed | Scene and control scripts |
| Networking | No first-party networking implementation identified. | Unknown | Package and asset inspection |

## Coding Conventions

- Namespace style: not established; use no namespace only for the first prototype scripts unless a project assembly convention is introduced.
- Serialized fields: use private fields with `[SerializeField]`.
- Async: not established.
- Comments/docs: comment intent and setup constraints, not obvious lines.

## Testing And Validation

- EditMode tests: no project tests found.
- PlayMode tests: no project tests found.
- CI/build validation: not documented.
- Current validation route: Unity Editor compilation, Console inspection, and a small mobile-oriented Play Mode smoke test.

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Official Unity MCP relay | directly initialized and used successfully; native Codex tool exposure still requires reload | `C:\Users\ethan\.codex\config.toml`, Unity relay executable, MCP probe |
| Unity Editor | available/open | Unity process for this project detected |
| Live MCP tool calls in this task | verified through direct official relay probe | `Unity_GetConsoleLogs`, `Unity_RunCommand` |
| Unity console/scene inspection | pending MCP reload | to verify after restart |

## Important Constraints

- The project working tree already contains user/template changes unrelated to this task; preserve them.
- Saving the HDRP scene through the current Unity 6 editor may serialize automatic HDRP/project-setting upgrades; review those diffs before committing.
- Mobile is the first target. PC support is deferred.
- The control prototype should use the existing Input System and uGUI package.
- Camera control must use Cinemachine; do not implement the orbit camera as a standalone transform-only controller.
- Avoid editing HDRP settings unless the control prototype proves a direct need.

## Unknowns And Confidence

- The exact active Unity Editor scene state is now confirmed through MCP for the camera rig; Play Mode behavior still needs a visual smoke test.
- The current control prototype does not require edits to the generated action asset because uGUI pointer events are supplied by `InputSystemUIInputModule` and camera swipe uses Enhanced Touch directly.
- Camera perspective/orthographic choice is a design decision; the first prototype will use a perspective isometric rig unless the visible result is clearly wrong.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `ProjectSettings/EditorBuildSettings.asset`
- `Assets/InputSystem_Actions.inputactions`
- `Assets/OutdoorsScene.unity`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/HDRPProjectSettings.asset`

<!-- unity-onboarding:generated:end -->
