# V5 Grass and V6 Material Generation Record

**Generated:** 2026-08-19  
**Mode:** Built-in reference-guided image editing  
**Status:** Two separate candidates awaiting Ethan approval

## Purpose

These tests isolate two different decisions. V5 corrects the grass card's orientation and visible width. V6 tests one uniform base color per geometric piece. They are intentionally not treated as one approved image.

## V5 grass geometry lock

The V4 forest was used as the immutable visual base, with the supplied grass close-up used only as a construction reference. The edit requested:

- one upright rectangular billboard/card per blade;
- a broad front face aimed toward the gameplay camera;
- nearly zero depth along the camera-facing direction;
- flat parallel sides and a flat square-cut top;
- current screen-space proportion of roughly three-to-five units tall per one unit wide;
- V4 height range, clump count, density, spacing, colors, camera, lighting, and all non-grass objects preserved;
- no edge-on hairlines, rods, cylinders, crossed cards, curves, points, circles, or fuzzy grass texture.

Selected output: `VisualTests/02-Lush-Forest-Traversal-v5-grass-billboards.png`.

## V6 material lock

V5 was requested as the immutable geometry base. The material-only edit requested:

- exactly one uniform intrinsic base/albedo color for every separate primitive or geometric piece;
- nearby pieces may choose different colors from a small palette;
- real lighting, face normals, cast shadows, and ambient occlusion remain allowed;
- no color texture maps, painted gradients, procedural noise, mottling, grain, stains, or edge wear;
- moss, repairs, cloth patches, and accents must be separate solid-color geometry;
- no geometry, silhouette, camera, light, composition, object count, or object placement changes.

The first material attempt was rejected internally because it changed the scene too extensively. The stricter second attempt is saved as `VisualTests/02-Lush-Forest-Traversal-v6-flat-color.png`. It better demonstrates the solid-color rule, but it still changes several forms and placements. Use V6 only as the material-treatment reference; use V5 as the grass geometry reference.

## Final prompt intent

Precise object edit. Preserve V5 pixel structure and geometry. Replace only painted or textured surface variation with one uniform base color per existing geometric piece. Different existing pieces may use neighboring solid colors. Preserve physical light and shadow. Do not redraw or simplify any object.

## Review order

1. Approve or reject V5's grass face width and orientation.
2. Approve or reject V6's one-color-per-piece material principle, ignoring its geometry drift.
3. Only after both decisions are approved, create one combined geometry-locked target and propagate the rules to other scenes.
