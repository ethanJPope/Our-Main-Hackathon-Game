# PlayerScene combat controls

Implemented September 4, 2026. Both mobile and keyboard/mouse are supported.

## Play

Open `Assets/PlayerScene.unity`, press Play, and click the Game view to focus it.
The wooden `PlayerPracticeTarget` has 1,000 health and no AI. Approach it to
test contact effects; reload Play Mode to reset it.

| Action | Desktop | Touch |
| --- | --- | --- |
| Move | WASD or arrows | Left joystick; partial deflection walks |
| Attack | Left mouse, outside UI; aims at the clicked ground position | ATK button; faces movement direction or locked target |
| Dodge | Space | Dodge button, on press |
| Lock / release | Tab | LOCK ON / RELEASE |
| Orbit camera | Hold right mouse and drag horizontally | Swipe clear space to the right of the joystick area |

Lock-on selects a visible enemy, turns the player toward it, and permits
camera-relative strafing. The camera smoothly frames both characters. Lock
releases on death, disable, range loss, or sustained obstruction; it never
silently switches to another enemy. Neutral dodge backsteps. Directional dodge
uses the current movement input, rather than waiting for the model to turn.

## Timing and tuning

| Parameter | PlayerScene value |
| --- | --- |
| Movement speed | 5 world units/second |
| Acceleration / braking / reversal | 35 / 45 / 55 units/second squared |
| Joystick radial dead zone | 12%, remaining range remapped to 0–1 |
| Action buffer | One action, 180 ms, newest wins; dodge wins simultaneous presses |
| Attack | 12 stamina; 720 ms commitment; one contact per strike |
| Attack contact | Existing animation event; 360 ms fallback if absent |
| Dodge | 18 stamina; 3.2 units over 320 ms |
| Dodge immunity | From 40 ms up to 220 ms after starting |
| Dodge recovery | Attacks and repeat dodges blocked until 520 ms |
| Stamina regeneration | 24/second after delay: 0.8 s attack, 1 s dodge |
| Lock acquisition / release distance | 14 / 18 units |
| Obstruction grace | 600 ms |

Attacks cannot cancel into dodge; a press in the final 180 ms of recovery can
queue the next action. Earlier presses expire instead of firing unexpectedly.
Damage cancels an unlanded strike. Death, pause, disable, and focus loss clear
pending input. Pausing freezes an action already in progress.

The separate `Assets/PlayerCombat/AC_PlayerCombat.controller` retimes the existing
punch to 720 ms and side-dodge clip to 480 ms. These are reused animations,
not new roll or eight-direction strafe animations. A cyan streak marks the
dodge immunity portion; short gold sparks occur only on confirmed damage.
Button colors and labels show readiness, recovery, and insufficient stamina.

## Implementation boundaries

- Existing movement, attack, vitals, and Cinemachine components remain the owners.
- `PlayerCombatInput` routes both devices and owns the single buffered command.
- `CombatTouchButton` retains the legacy touch path required by Unity Remote,
  plus mouse testing. Joystick and buttons sample once per frame on demand so
  execution order does not add a frame of latency.
- Touch controls retain individual finger ownership. UI clicks do not attack
  the world or begin camera rotation.
- Safe-area layout falls back to Game view dimensions when Unity Remote or
  Device Simulator reports dimensions outside the current render surface.
- The complete new setup is wired in PlayerScene only. Shared runtime script
  fixes also affect other scenes using those scripts; other scenes do not gain
  the new input bridge or lock-on until deliberately wired.
- No packages, project input settings, build scenes, boss AI, or source
  animations were changed by this task.
- This is a combat-controls foundation, not a full Dark Souls moveset:
  no new block/parry, heavy attack, sprint, equipment, or jump mechanics.

## Verification

`Tools > Player > Run Combat Control Tests` runs the PlayerScene regression
suite without changing Build Settings. Output is written to
`Temp/PlayerCombatTests/results.xml` and `status.txt`.

The suite covers directional and neutral dodge, collision, startup/immunity/
recovery damage windows, stamina/pause/disable/death gates, attack/dodge
exclusion including recovery, late buffering and expiration, lock release,
contact-only damage, duplicate contact protection, and hit interruption.

Desktop Game view checks supplement the tests with actual mouse/keyboard
input and visual inspection. Physical-phone multitouch and subjective combat
feel still need a device playtest; desktop simulation is not a hardware test.

Latest result: **12/12 Play Mode tests passed**. Actual Game view input checks
confirmed joystick drag and release, W movement, Space dodge, touch dodge,
touch lock-on, touch attack, and desktop left-click attack. The two attacks
reduced practice-target health from 1,000 to 990 to 980. The dodge streak was
visually inspected in slow motion; the combat VFX shader compiled without
errors. Unity's Console reported no errors or exceptions after validation.

The user's video could not be retrieved. The requested emphasis on animation,
timing, and VFX informed the work; the implementation does not claim to
reproduce the unseen video's recommendations.
