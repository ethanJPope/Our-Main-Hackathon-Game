# SM_Conifer_01 — Design V2

Status: **awaiting Ethan's visual approval**. This is a Blender-only design and has not been exported to Unity.

## Approval views

- [Front view](SM_Conifer_01-design-v2-front.png)
- [Three-quarter view](SM_Conifer_01-design-v2-three-quarter.png)
- [Measured design statistics](SM_Conifer_01-design-v2-stats.json)

## Correction from rejected V1

V1 used sparse panels that traveled farther outward than downward. They read as horizontal branch spokes, exposed the trunk between tiers, and did not match the supplied tree reference.

V2 changes the construction to:

- ten dense overlapping foliage skirts;
- long shallow panels whose vertical drop is roughly three to four times their added outward travel;
- roots placed near the conical surface so each panel can hang rather than radiate from the trunk;
- twelve long apex strips forming one continuous layered crown;
- a dark irregular foliage core behind the panels, hiding the trunk without defining the outer silhouette;
- trunk visibility restricted to the base below the foliage;
- one uniform solid base color per separate panel and no textures.

The lower core narrows toward the trunk so the hanging panels create the bottom edge instead of a flat cone.

## Files

- Editable Blender source: `SourceArt/Blender/Environment/Trees/DesignV2/SM_Conifer_01-design-v2.blend`
- Reproducible build script: `Tools/Blender/build_conifer_01.py`
- Front preview: `Docs/ArtDirection/Assets/DesignV2/SM_Conifer_01-design-v2-front.png`
- Three-quarter preview: `Docs/ArtDirection/Assets/DesignV2/SM_Conifer_01-design-v2-three-quarter.png`

The script explicitly skips FBX export. No `SM_Conifer_01.fbx` or `P_Conifer_01.prefab` exists under `Assets` while this design is unapproved.

## Approval gate

Judge only these three shape questions before export:

1. Are the foliage panels vertical enough?
2. Is the layer density close enough to the reference?
3. Is the trunk hidden enough above its exposed base?

If approved, the next step is a clean FBX export and Unity prefab. If rejected, change only the named shape issue and render new Blender approval views.
