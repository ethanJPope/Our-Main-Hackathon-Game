# Primitive-Grammar V2 Generation Record

**Date:** 2026-08-19  
**Mode:** Built-in image generation, multi-image reference-guided editing  
**Status:** Selected internal candidates; awaiting Ethan/team approval

## Inputs and roles

- The user-supplied forest image was the primary construction-language reference.
- `02-Lush-Forest-Traversal.png` anchored the existing traveler, camera, route, palette, and lighting for Forest V2.
- `02-Lush-Forest-Traversal-v2.png` became the project asset-grammar anchor for Village V2.
- `01-Village-Spawn.png` supplied narrative beats and landmark composition only; its detailed asset construction was explicitly rejected.

## Final prompt lock

The selected prompts enforce:

- complexity by aggregation, never by individual asset complexity;
- conifers assembled from offset rectangular foliage tiers;
- upright bar grass and square/dot groundcover;
- cross/dot flowers;
- cuboid stones and square-ended beam logs;
- village shells made from a few wall boxes, timber bars, and roof slabs;
- damage made by removing or tilting whole pieces;
- a guardian assembled from a small set of large asymmetric boulder-box masses;
- the existing gameplay camera, traveler identity, bright daylight, soft depth, and original world content.

The complete reusable scene wording is maintained in `VisualTestPrompts.md`.

## Selected outputs

| Candidate | Project file | Notes |
| --- | --- | --- |
| Village spawn V2 | `VisualTests/01-Village-Spawn-v2.png` | A first correction was rejected for detailed masonry and roofs. The selected correction merges them into broad slabs and removes clutter. |
| Forest traversal V2 | `VisualTests/02-Lush-Forest-Traversal-v2.png` | Replaces detailed vegetation, rocks, and stump with the strict primitive kit while preserving the scene's mood and route. |

The earlier PNG files were not overwritten. Quarry and restoration were not regenerated during this focused revision.
