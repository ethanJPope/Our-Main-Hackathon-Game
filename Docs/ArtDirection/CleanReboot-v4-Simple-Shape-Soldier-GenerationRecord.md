# Clean-Reboot V4 Simple-Shape Soldier Generation Record

**Generated:** 2026-08-19  
**Mode:** Built-in reference-guided `precise-object-edit`  
**Status:** Selected simple-shape character correction; awaiting Ethan's approval

## Intent

Correct V3's player construction without changing his role, proportions, pose, placement, palette, or environment. The new player must visibly look assembled from simple low-poly shapes rather than a smooth sculpt with stylized clothing.

## Input

- Edit target and character rollback: `VisualTests/CleanReboot/01-Forest-Beacon-Shrine-v3-starting-soldier.png`.
- Exact construction rules: the starting-soldier section in `ArtCompass.md` and the starting-player lock in `CleanRebootPrompt.md`.

## Primitive lock

- Roughly 12–16 visible pieces total.
- One faceted block head and one angular hair cap.
- One broad tapered torso wedge.
- Two prism pieces per arm plus one block hand.
- One hip block and one flat belt.
- One tapered prism per leg and one wedge per boot.
- Sword/scabbard reduced to a long slab, short guard bar, and simple grip block.
- Broad flat faces, clear angular breaks, one base color per piece, slight taper, and restrained asymmetry.
- No smooth anatomy, round limbs, garment folds, seams, fingers, hair strands, material grain, or small decoration.

## Selected output

`VisualTests/CleanReboot/01-Forest-Beacon-Shrine-v4-simple-shape-soldier.png`

- Dimensions: 1672 × 941.
- SHA-256: `C0B8AE7B8A1BE76E2718216780405C54BECA5A1535582812D986CBA777B7ED98`.
- Phone, comparison, and accessibility checks: `VisualTests/CleanReboot/Verification`.

## Verification boundary

V4 visibly reduces the player to broader geometric pieces and passes the current phone and accessibility checks. This raster concept cannot prove an exact triangle count or 360-degree construction; those must be enforced in the Unity model. V3 remains the character rollback and V2 remains the environment-only rollback.
