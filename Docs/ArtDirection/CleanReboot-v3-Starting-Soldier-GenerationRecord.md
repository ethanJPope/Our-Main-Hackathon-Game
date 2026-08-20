# Clean-Reboot V3 Starting-Soldier Generation Record

**Generated:** 2026-08-19  
**Mode:** Built-in reference-guided `precise-object-edit`  
**Status:** Selected first starting-soldier candidate; awaiting Ethan's character and overall style approval

## Intent

Add the first starting-player candidate to the clean-reboot forest shrine without redesigning the approved environment. Translate Ethan's description—an unarmored soldier who is somewhat short and wide—into a readable mobile gameplay silhouette.

## Input

- Immutable environment target and rollback source: `VisualTests/CleanReboot/01-Forest-Beacon-Shrine-v2-simple-log.png`.
- Character rules: the starting-soldier section in `ArtCompass.md` and the starting-player lock in `CleanRebootPrompt.md`.

## Character lock

- One adult male soldier in a back three-quarter standing pose, facing the shrine.
- Approximately four-and-a-half heads tall with broad shoulders and torso, sturdy limbs, and a low center of gravity.
- Short dark hair, faded rust-brown long-sleeve under-tunic, dark trousers, boots, and a wide belt.
- Weathered slightly oversized one-handed sword in a plain scabbard at the left hip.
- No helmet, shield, cloak, backpack, chainmail, metal plates, or other visible armor.
- Grounded heroic proportions; no dwarf, chibi, bodybuilder, or comedy caricature.
- Broad faceted low-poly construction, matte painterly materials, no outlines.

## Composition lock

- Full body slightly below center on the central path.
- Clear separation from the log, stones, grass, and shrine.
- Match the existing perspective, warm sunlight, cool ambient fill, contact shadow, haze, palette, and depth of field.
- Preserve the environment visually; add no other characters, creatures, props, text, or UI.

## Selected output

`VisualTests/CleanReboot/01-Forest-Beacon-Shrine-v3-starting-soldier.png`

- Dimensions: 1672 × 941.
- SHA-256: `646AB056E8A707B6D4018DEF7E8F360D3556F8D8F46686DB6F4DDCE033B9AF01`.
- Phone, comparison, and accessibility checks: `VisualTests/CleanReboot/Verification`.

## Verification boundary

The player, route, and shrine remain readable at phone size, in grayscale, and in approximate protanopia/deuteranopia previews. Generative raster editing does not guarantee exact pixel identity outside the inserted character, so V2 remains unchanged as the environment rollback source. A single view cannot prove final facial design or 360-degree silhouette readability.
