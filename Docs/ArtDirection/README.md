# Art Direction Package

This folder is the source of truth for the clean-rebooted world-art direction.

## Start here

1. Read [ArtCompass.md](ArtCompass.md) before creating or sourcing an asset.
2. Use [CleanRebootPrompt.md](CleanRebootPrompt.md) when revising or extending the concept images so the style does not drift.
3. Check [VerificationReport.md](VerificationReport.md) for the current visual QA result and deferred 3D/performance gates.

## Current visual compass

### Clean-reboot V4 — simple-shape starting soldier, awaiting character approval

The new reference set replaces the image theme of V1–V9. Gameplay lore, camera behavior, controls, HUD, two-week scope, beacon story, and guardian story remain unchanged.

- [Open the current clean-reboot forest shrine](VisualTests/CleanReboot/01-Forest-Beacon-Shrine-v4-simple-shape-soldier.png)
- [Compare V3 and V4 player construction](VisualTests/CleanReboot/Verification/V3-V4-Player-Construction-Comparison.png)
- [Open the phone preview](VisualTests/CleanReboot/Verification/01-Forest-Beacon-Shrine-v4-simple-shape-soldier-phone.png)
- [Open the accessibility contact sheet](VisualTests/CleanReboot/Verification/01-Forest-Beacon-Shrine-v4-simple-shape-soldier-accessibility-contact.png)
- [Read the clean-reboot prompt](CleanRebootPrompt.md)
- [Read the V4 generation record](CleanReboot-v4-Simple-Shape-Soldier-GenerationRecord.md)
- [Open the rejected V3 character pass](VisualTests/CleanReboot/01-Forest-Beacon-Shrine-v3-starting-soldier.png)
- [Open the environment-only V2 rollback](VisualTests/CleanReboot/01-Forest-Beacon-Shrine-v2-simple-log.png)
- [Review the reference roles](References/CleanReboot-2026-08-19/README.md)

Ethan rejected V3 because its character was too smooth and detailed for the environment. V4 preserves the short, broad unarmored soldier but reconstructs him from a small visible budget of faceted blocks, prisms, and wedges. Review this construction before creating a turnaround sheet or Unity model. V2 remains the environment-only rollback source.

### Blender tree Design V2 — awaiting approval

The first tree export was rejected because its sparse foliage panels read as horizontal spokes and exposed the trunk. Design V2 remains Blender-only and uses ten overlapping skirts of long near-vertical panels plus a layered apex. The trunk is visible only at the base. Nothing will be exported to Unity until Ethan approves this design.

- [Open the front approval view](Assets/DesignV2/SM_Conifer_01-design-v2-front.png)
- [Open the three-quarter approval view](Assets/DesignV2/SM_Conifer_01-design-v2-three-quarter.png)
- [Read the design record](Assets/DesignV2/SM_Conifer_01-design-v2.md)

## Legacy V1–V9 visual history

Everything below is preserved for decision history only. Do not use it as active production guidance.

### V9 straight ribbon grass — superseded

Ethan approved V8's broad blade dimensions, then requested that the blades stand straight up without changing those dimensions. Two generated attempts were rejected internally for stretching the blades too tall. V9 is the closest conservative straightening pass and remains a concept target rather than a dimensional mesh measurement.

- [Open the current V9 orientation candidate](VisualTests/02-Lush-Forest-Traversal-v9-straight-ribbon-grass.png)
- [Compare V8 and V9](VisualTests/Verification/V9-Grass/V8-V9-Straightening-Comparison.png)
- [Open the V9 phone preview](VisualTests/Verification/V9-Grass/v9-straight-ribbon-grass-phone.png)
- Read the [V9 generation record](V9-Grass-GenerationRecord.md).

V8 remains the exact **width and height reference**. V9 is the **straight-upright orientation reference**. During Unity implementation, use V8's mesh dimensions and rotate/straighten the blades without scaling them. Each blade remains one uniform solid green, while neighboring blades and physical lighting create variation. Do not propagate this grass to other frames until Ethan approves V9's orientation.

### V7/V8 width calibration history

The [V6/V7/V8 comparison](VisualTests/Verification/V7-V8-Grass/Grass-Shape-V6-V7-V8-Comparison.png), [V8 phone preview](VisualTests/Verification/V7-V8-Grass/v8-wide-ribbon-grass-phone.png), saved [shape-only reference](References/User-Grass-Shape-Reference-2026-08-19.png), and [generation record](V7-V8-Grass-GenerationRecord.md) preserve the path to the approved width.

### V5/V6 calibration history

V5 corrected edge-on hairline grass but was too rigid. V6 demonstrates the one-solid-color-per-piece surface rule, but its thick upright grass reads like bamboo and its edit changed some non-grass geometry. Keep the [V5/V6 comparison](VisualTests/Verification/V5-V6/Grass-Width-And-Flat-Color-Comparison.png) and [generation record](V5-Grass-V6-Material-GenerationRecord.md) as decision history, not as the current grass source.

### Superseded V4 grass-width calibration

V4 replaced round-looking grass with rectangles but presented the blades too close to edge-on, making them read as hairlines. Keep its [comparison](VisualTests/Verification/V4-Grass/Grass-Only-Comparison.png) as decision history; use V8 for dimensions and V9 for the current orientation review.

### Thin-card organic V3 candidates — awaiting approval

These two images correct V2's blockiness. They keep vegetation extremely thin while allowing rocks, logs, architecture, and the guardian a few controlled irregular low-poly faces.

| Test | What changed | Candidate |
| --- | --- | --- |
| Village spawn V3 | Replaces thick grass, trees, crops, walls, and roofs with near-planar cards or shallow skewed planes; guardian and field rocks use broad irregular faces | [Open V3](VisualTests/01-Village-Spawn-v3.png) |
| Lush forest V3 | Uses paper-thin foliage tiers and ribbon grass, irregular faceted stones at mixed burial depths, and a gently bent/tapered squared log | [Open V3](VisualTests/02-Lush-Forest-Traversal-v3.png) |

Use the [V1–V2–V3 construction comparison](VisualTests/Verification/V3/V1-V2-V3-Construction-Comparison.png) to see the move from over-detailed, through too blocky, to thin and organic. The [V3 phone and accessibility sheet](VisualTests/Verification/V3/Contact-V3-Phone-Accessibility.png) contains normal, grayscale, protanopia, and deuteranopia previews.

Quarry and restoration remain first-pass images. Regenerate them with the V3 grammar only after the forest and village direction is approved.

### Superseded V2 calibration

V2 correctly reduced object detail but made grass, tree tiers, rocks, logs, walls, and roofs too thick and block-like. The files and [V2 comparison](VisualTests/Verification/V2/Primitive-Grammar-Comparison.png) remain as decision history; do not use them as production construction references.

### First-pass archive

| Test | Purpose | File |
| --- | --- | --- |
| Village spawn | Establishes the first view, old raid damage, guardian warning, regional beacon, and mountain landmark | [Open image](VisualTests/01-Village-Spawn.png) |
| Lush forest | Establishes normal exploration density, path composition, vegetation, and daylight | [Open image](VisualTests/02-Lush-Forest-Traversal.png) |
| Quarry reveal | Establishes the hidden asymmetric quarry, boss framing, and separate guarded shard | [Open image](VisualTests/03-Quarry-Guardian-Reveal.png) |
| Restoration | Establishes what beacon restoration changes and what human history it preserves | [Open image](VisualTests/04-Beacon-Restoration-Comparison.png) |

The first-pass [phone-size contact sheet](VisualTests/Verification/Contact-Phone.png) and its grayscale, protanopia, and deuteranopia sheets remain available for historical comparison.
