# Visual-Test Verification Report

**Verified:** 2026-08-19  
**Result:** Clean-reboot V4 passes internal prompt, phone-size, grayscale, and approximate color-vision QA; awaits Ethan's character approval  
**Not claimed:** 3D turntable readability, Unity rendering parity, or device performance

## 2026-08-19 clean-reboot V4 simple-shape soldier correction

Ethan rejected V3 because the player was not visibly low-poly enough and contained too many smooth, detailed forms. V4 changes only the intended player construction toward a small set of simple faceted pieces.

| File | Role | Dimensions | SHA-256 |
| --- | --- | --- | --- |
| `VisualTests/CleanReboot/01-Forest-Beacon-Shrine-v4-simple-shape-soldier.png` | Current character-and-environment candidate | 1672 × 941 | `C0B8AE7B8A1BE76E2718216780405C54BECA5A1535582812D986CBA777B7ED98` |
| `VisualTests/CleanReboot/Verification/01-Forest-Beacon-Shrine-v4-simple-shape-soldier-phone.png` | Phone-size preview | 960 × 540 | `97451CDE59FA9169E3B614D712CAF1D146E5ED0F27BE6DD7A79BB2AF3EA70B77` |
| `VisualTests/CleanReboot/Verification/V3-V4-Player-Construction-Comparison.png` | Close construction comparison | 1012 × 505 | `81D40349F8BC036FA166C8D0C56E9ACCE2AE4D74ED6A5D803881852FA5D84E68` |
| `VisualTests/CleanReboot/Verification/01-Forest-Beacon-Shrine-v4-simple-shape-soldier-accessibility-contact.png` | Normal, grayscale, and approximate color-vision contact sheet | 1920 × 1080 | `715C317EB97C9A7C657705E7CE3D8C484ACE0E40E0C4470FA7A262B703092B91` |

- The head changed from a rounded sculpt to one faceted block with a single angular hair cap.
- The torso, arms, hands, legs, boots, belt, and sword now read as a limited set of broad prism, block, wedge, and slab pieces.
- Clothing folds, realistic anatomy, hair detail, fingers, and small garment geometry were removed or substantially reduced.
- Slight taper and asymmetry keep the character handcrafted without turning him into a voxel-grid figure.
- The simple silhouette remains readable at 960 × 540 and retains separation from the path and shrine in grayscale and approximate color-vision checks.
- The environment remains visually consistent in full-frame review; V3 and V2 remain unchanged rollback sources because raster generation is not pixel-preserving.

## 2026-08-19 clean-reboot V3 starting-soldier addition

Ethan defined the starting player as a male soldier without armor who is somewhat short and wide. V3 adds one character to the V2 shrine clearing while retaining V2 as the environment-only rollback source.

| File | Role | Dimensions | SHA-256 |
| --- | --- | --- | --- |
| `VisualTests/CleanReboot/01-Forest-Beacon-Shrine-v3-starting-soldier.png` | Current character-and-environment candidate | 1672 × 941 | `646AB056E8A707B6D4018DEF7E8F360D3556F8D8F46686DB6F4DDCE033B9AF01` |
| `VisualTests/CleanReboot/Verification/01-Forest-Beacon-Shrine-v3-starting-soldier-phone.png` | Phone-size preview | 960 × 540 | `9CADA542D789D24235E8D3AF7C7EC0712397ABB5A8F92D9D6F826F3AE0A87B35` |
| `VisualTests/CleanReboot/Verification/V2-V3-Starting-Player-Comparison.png` | Environment-only and player-added comparison | 1452 × 455 | `E66D8F9DD31029329C33F003DE065EC9ABAC820AB6B68D57C28559477F98C65E` |
| `VisualTests/CleanReboot/Verification/01-Forest-Beacon-Shrine-v3-starting-soldier-accessibility-contact.png` | Normal, grayscale, and approximate color-vision contact sheet | 1920 × 1080 | `D45291551200D4093C907C7CC962A20D5DA6D5C428E210051BDE7C7747AA51AB` |

- The player reads as short and broad through his torso, shoulders, arms, and legs without becoming a dwarf or chibi caricature.
- His rust-brown under-tunic, dark trousers, boots, belt, short hair, and sheathed sword identify a grounded soldier while keeping the starting state visibly unarmored.
- The back three-quarter pose faces the shrine, and the feet plus contact shadow place him on the path.
- At 960 × 540, the player remains legible without obscuring the route, log, or beacon.
- Grayscale and approximate protanopia/deuteranopia checks preserve separation between the player, path, and shrine through value and silhouette.
- The environment remains visually consistent in the V2/V3 comparison. Reference-guided raster generation is not pixel-preserving, so V2 remains the exact environment rollback source.

## 2026-08-19 clean-reboot V2 simple-log correction

Ethan identified the V1 foreground log as too detailed and selected the supplementary forest reference's much simpler log construction. V2 is a non-destructive, reference-guided edit that reduces only the intended log design while retaining V1.

| File | Role | Dimensions | SHA-256 |
| --- | --- | --- | --- |
| `VisualTests/CleanReboot/01-Forest-Beacon-Shrine-v2-simple-log.png` | Current overall style candidate | 1672 × 941 | `19D81311F7FEE75735D8BD6A4FF928CD8D0D08252F9871642B68F8E6A05B3D9D` |
| `VisualTests/CleanReboot/Verification/01-Forest-Beacon-Shrine-v2-simple-log-phone.png` | Phone-size preview | 960 × 540 | `F27274B8D6EB5F8BB3527A24ACB9887AFB9A902BCDBB7DFFE20F5127DB0BE6D7` |
| `VisualTests/CleanReboot/Verification/V1-V2-Log-Comparison.png` | Cropped construction comparison | 952 × 350 | `C8329D9A6967F3C1C5B55CC273ACA6CC6B157A71A25A871F8F23DCE02BCE326B` |
| `VisualTests/CleanReboot/Verification/01-Forest-Beacon-Shrine-v2-simple-log-accessibility-contact.png` | Normal, grayscale, and approximate color-vision contact sheet | 1920 × 1080 | `C71F4AB24D4A82530C4D563D4DDD4EF36F91BBFDE7E5B50D377A3622106D0525` |

- The dark hollow, cut-end detail, many bark facets, and dense leafy moss were removed.
- The replacement reads as one simple warm orange-brown trunk with a low-sided roughly squared section, slight taper, broad planes, solid uneven ends, and sparse flat moss.
- At 960 × 540, the log remains readable without competing with the route or beacon.
- The route, beacon, trees, grass, rocks, flowers, lighting hierarchy, and composition remain visually consistent in full-frame review. Reference-guided raster generation is not pixel-preserving, so this is a visual lock rather than a claim that every non-log pixel is identical.
- Grayscale and approximate protanopia/deuteranopia checks retain the same landmark and traversal hierarchy. These simulations are design checks, not medical diagnostics.

## 2026-08-19 clean-reboot V1

Ethan supplied a new authoritative visual prompt plus separate mood, tree-shape, grass-shape, and density references. This clean reboot discards the image theme of V1–V9 while preserving the game's lore, camera, controls, and prototype scope.

| File | Role | Dimensions | SHA-256 |
| --- | --- | --- | --- |
| `VisualTests/CleanReboot/01-Forest-Beacon-Shrine-v1.png` | Preserved original; superseded for fallen-log construction | 1672 × 941 | `8837578E3C98F321F3EA18DDD5C6D6984A1B2349246DD943F2250589154673AC` |
| `VisualTests/CleanReboot/Verification/01-Forest-Beacon-Shrine-v1-phone.png` | Phone-size preview | 960 × 540 | `FB3FE4AE0026662FC90B7C3A27BC0FF2572545BBF89F155A4FFDF7BFD74545F2` |

- The conifers read as layered tapered polygonal branch masses rather than perfect cones or realistic needle trees.
- Grass reads as broad squared low-poly ribbons with varied height and clump spacing.
- Matte painterly tonal variation, broad facets, warm dappled sunlight, cool teal depth, contact occlusion, and restrained haze match the new prompt.
- The route, open clearing, beacon shrine, log, rocks, vegetation wall, and focal light remain readable at 960 × 540.
- Grayscale preserves the landmark and route hierarchy. Approximate protanopia and deuteranopia checks preserve the same hierarchy through value and silhouette.
- The beacon is original, clearly separate from the route, and not presented as a circular boss arena or copied landmark.
- No character, HUD, text, outlines, visible horizon, floating base, or watermark appears.

See `VisualTests/CleanReboot/Verification/01-Forest-Beacon-Shrine-v1-accessibility-contact.png`.

## Legacy verification history

All V1–V9 sections below document the discarded visual theme. They remain useful only as decision history and are not active production guidance.

## 2026-08-19 V9 straight-upright orientation

Ethan approved V8's blade width and asked to make the grass stand straight up without changing the blade dimensions. The first two straightening edits were rejected internally because they visibly stretched the grass into tall poles. The selected third attempt is closer to V8's broad, short-to-medium ribbon proportions.

| File | Role | Dimensions | SHA-256 |
| --- | --- | --- | --- |
| `02-Lush-Forest-Traversal-v9-straight-ribbon-grass.png` | Current orientation candidate | 1672 × 941 | `F232ACB1CAFB8ACBA93FF928EEF2D8BCA1FF08F493BB4255F17236106589B8E7` |

- Blades use straight vertical long edges with no intentional lean, arc, or curl.
- The selected pass retains broad flat faces, paper-thin intent, varied heights, clump gaps, and one uniform base color per blade.
- At 960 × 540, the grass orientation, route, traveler, rocks, log, flowers, and forest boundaries remain readable.
- Raster generation cannot prove blade-by-blade dimensional equality. V8 remains the width/height source; a Unity mesh should be straightened by rotation or vertex placement without scaling.

See `VisualTests/Verification/V9-Grass/V8-V9-Straightening-Comparison.png` and `v9-straight-ribbon-grass-phone.png`.

## 2026-08-19 V7/V8 ribbon-grass correction

Ethan rejected V6's grass because its thick, rigid upright forms and visible top faces read like bamboo. He supplied a new shape reference showing dense clumps of flat low-poly leaf ribbons. V7 changed the construction; V8 widened the visible ribbon faces by roughly 30–40 percent at his request.

| File | Role | Dimensions | SHA-256 |
| --- | --- | --- | --- |
| `02-Lush-Forest-Traversal-v7-ribbon-grass.png` | Narrower ribbon calibration | 1672 × 941 | `CD3073826F19E40C3BCDAF2833F8D6C1C843BB54A5042971AA2AB9DAC303EB33` |
| `02-Lush-Forest-Traversal-v8-wide-ribbon-grass.png` | Current grass candidate | 1672 × 941 | `5CC1C9213D53E66F2332C91BA3F9647034D4C572159AD1BA3D1DAC7433044463` |
| `User-Grass-Shape-Reference-2026-08-19.png` | User-supplied shape-only reference; not a shipping asset | 1542 × 1020 | `40271A457CFC8A78843EA5981F4EFF742FAD878122345CFED018168810A8E742` |

### V8 result: dimensions approved; superseded only for orientation

- Grass reads as overlapping broad leaf ribbons with paper-thin depth, varied height and lean, and occasional gentle bends.
- V8's visible blade faces are clearly wider than V7 while avoiding V6's square-post silhouette, horizontal top caps, and picket-row regularity.
- Each blade retains one uniform base green with no painted texture; nearby solid-green blades plus physical lighting create the color complexity.
- At 960 × 540, the ribbon construction remains visible and the traveler, route, rocks, log, flowers, and tree boundaries remain readable.
- The generated edit still cannot prove exact mesh thickness or camera-facing behavior from every orbit angle; those remain Unity implementation checks.

See `VisualTests/Verification/V7-V8-Grass/Grass-Shape-V6-V7-V8-Comparison.png` and `v8-wide-ribbon-grass-phone.png`.

## 2026-08-19 V5 grass geometry and V6 flat-color material study

Ethan clarified two independent construction rules. A grass blade must be wide across the face visible to the camera but nearly depthless along the camera-facing direction. Separately, each basic geometric piece must use one uniform base/albedo color rather than a texture.

| File | Role | Dimensions | SHA-256 |
| --- | --- | --- | --- |
| `02-Lush-Forest-Traversal-v5-grass-billboards.png` | Grass geometry candidate | 1672 × 941 | `09E61A819A59018C706280358CC47E6BCED7C86133AD90BE1CE04F66F4EA4E0C` |
| `02-Lush-Forest-Traversal-v6-flat-color.png` | Material-treatment reference | 1672 × 941 | `794842BCE1AC8425105AB9D7FFB2B49882EED7D9B0A68950608F926A3B78E793` |

### V5 result: superseded grass calibration

- Blades present broad rectangular faces to the gameplay camera rather than edge-on hairlines.
- Their camera-axis depth remains visually near zero; they do not read as rods, cylinders, or crossed cards.
- The current screen-space calibration is roughly three-to-five times taller than visible width, with the V4 height, clump spacing, and ground gaps retained.
- At phone size, the traveler, route, log, and boulders remain readable while grass reads as a distinct repeated plane shape.

### V6 result: retained material reference, rejected grass, geometry-preservation fail

- Ordinary pieces read as untextured solid-color geometry. Neighboring pieces use different curated colors, while sunlight, face normals, cast shadows, and ambient occlusion supply within-piece shading.
- The frame demonstrates the intended rule: no painted albedo gradients, grain, mottling, stains, or surface texture on a primitive; moss and accents are separate pieces.
- The generation also shifted tree silhouettes, rock forms, log construction, path width, and object placement. It therefore fails the strict geometry-lock requirement and must not replace V5 as the geometry source.
- Review V5 for grass geometry and V6 for material treatment independently. A final combined frame should be made only after both are approved.

See `VisualTests/Verification/V5-V6/Grass-Width-And-Flat-Color-Comparison.png`, `v5-grass-billboards-phone.png`, and `v6-flat-color-phone.png`.

## 2026-08-19 superseded grass-only V4 isolation

Ethan identified that the grass in the prior forest frame read as round ground marks instead of tall lines. V4 changed only the grass layer, but its rectangles were later rejected for reading too close to edge-on. V5 supersedes it for grass-width and orientation review.

| File | Dimensions | SHA-256 |
| --- | --- | --- |
| `02-Lush-Forest-Traversal-v4-grass.png` | 1672 × 941 | `E03AB96D257CDF9C78EF33F06DB79ABEEAA9C2F501D8110925892727A0928D02` |

- The first isolated edit achieved the requested rectangular shape but was rejected internally because it produced overly tall, dense reeds.
- The selected second edit retained straight flat-topped rectangular blades while reducing most grass to ankle-to-low-calf height and opening gaps between clumps.
- At phone size, grass read as repeated vertical lines rather than circular texture, but the visible faces were too narrow.
- Trees, rocks, log, flowers, character, camera, lighting, and composition were intentionally not evaluated or revised in this pass.
- See `VisualTests/Verification/V4-Grass/Grass-Only-Comparison.png` and `02-Lush-Forest-Traversal-v4-grass-phone.png`.

## 2026-08-19 thin-card V3 correction

Ethan identified that V2 oversimplified the reference into thick blocks. The corrected rule is: **thin where nature is sheet-like; irregular where nature is solid**. Grass, crops, conifer foliage, and low groundcover are nearly planar cards. Rocks, logs, ruins, terrain, and the guardian keep a small number of broad, uneven low-poly faces.

Two non-destructive V3 candidates were created. V1 and V2 files remain unchanged as calibration history.

| File | Dimensions | SHA-256 |
| --- | --- | --- |
| `01-Village-Spawn-v3.png` | 1672 × 941 | `31882364A4F99E8A95806B5FEB680E3A71397A28F3C644CBA305776CD70FE4FF` |
| `02-Lush-Forest-Traversal-v3.png` | 1672 × 941 | `BB7240F4785253D013168A58C1A778978AE073E1B66B7C03778F045600DC29D0` |

### V3 forest result: internal pass, awaiting approval

- Conifer tiers and grass now read as thin cards rather than extruded blocks.
- Rocks use broad irregular faces and include both exposed boulders and partially buried stones.
- A targeted second edit replaced the perfect beam log with a gently bent, tapered, uneven-ended faceted trunk.
- The traveler and path remain readable at phone size and in grayscale.

### V3 village result: internal pass, awaiting approval

- The simple two-ruin layout is preserved without returning to V1's masonry, shingles, cart, or rubble detail.
- Grass, crops, and conifer layers are thin; walls and roofs are shallow skewed planes instead of toy blocks.
- Guardian stones and field rocks use a few irregular faces, with some field stones embedded into the terrain.
- Traveler, guardian, route, hill beacon, and mountain beacon remain separate at phone size and in both color-vision simulations.

### V3 accessibility and scale check

- Normal previews were reviewed at 960 × 540.
- Grayscale preserves the route, traveler, guardian, ruin shells, and two beacon landmarks.
- Approximate protanopia and deuteranopia previews preserve navigation and landmark hierarchy through value and silhouette.
- See `VisualTests/Verification/V3/Contact-V3-Phone-Accessibility.png` and `V1-V2-V3-Construction-Comparison.png`.

## Superseded V2 primitive calibration

Close inspection of the supplied reference established a stricter rule: **complexity by aggregation, never by individual asset complexity**. The first forest frame remains a useful mood and composition anchor, but its rounded rocks, bushes, bark, and stump are too detailed. The first village frame is superseded because its masonry, shingles, debris, cart, and ruined silhouettes use too many small parts.

Two non-destructive V2 candidates were created and later rejected for blockiness. The original files remain unchanged.

| File | Dimensions | SHA-256 |
| --- | --- | --- |
| `01-Village-Spawn-v2.png` | 1672 × 941 | `49CF8D5AF94C8FA56570514289E5CC7E0A311F1C4A8AFA341EA145678D2B0EFB` |
| `02-Lush-Forest-Traversal-v2.png` | 1672 × 941 | `C0D529C3DFD14CD555324C032CADC4AFB27401443396A423811D1C0A4FB9E21B` |

### V2 forest result: superseded — too blocky

- It correctly removed the complex stump and reduced surface detail.
- Thick stair-step foliage, square-rod grass, cube rocks, tiled ground, and a perfect beam log overshot the reference into voxel-like construction.
- Retain it only as a calibration example of what “too blocky” means.

### V2 village result: superseded — too blocky

- It correctly reduced the village to two open shell ruins and removed most clutter.
- Its thick wall and roof blocks, post-like crops and grass, boxy stones, and boulder-box guardian still feel like toy blocks rather than the supplied reference.
- Retain it only as a calibration example of what “too blocky” means.

### V2 accessibility and scale check

- Normal phone previews were reviewed at 960 × 540.
- Grayscale preserves the bright route, dark traveler, guardian silhouette, ruin shells, and beacon landmarks.
- Approximate protanopia and deuteranopia previews preserve navigation and landmark separation because value and silhouette carry the hierarchy.
- The simulations are design checks, not medical diagnostics.
- See `VisualTests/Verification/V2/Contact-V2-Phone-Accessibility.png` and `Primitive-Grammar-Comparison.png`.

## First-pass artifact integrity

All four originals are PNG files at 1672 × 941 pixels (`1.7768:1`, effectively 16:9).

| File | SHA-256 |
| --- | --- |
| `01-Village-Spawn.png` | `DFABAB6F0DD5F506D0F096D63F7904ABC6351BE4F8C8D2AFFCC2F3F431B0D6F5` |
| `02-Lush-Forest-Traversal.png` | `A2E20BD5F5899012CB0181946CB1D1BA94A4FAC66B805AAC85D2A9510A7F0A26` |
| `03-Quarry-Guardian-Reveal.png` | `9D089A30D135FA45998AA153A616104E7EBE491B51FE02694C6FC4A6AA284F49` |
| `04-Beacon-Restoration-Comparison.png` | `E289B69ECA1C1BE999F64D6497A670C59E9B9DC3EE97EBF81C193ADF466575F9` |

## Review method

- Downsampled each original to 960 × 540 to approximate landscape phone viewing.
- Created grayscale previews to test value hierarchy.
- Created approximate full-protanopia and full-deuteranopia previews to expose color-only communication.
- Inspected four labeled contact sheets for character, route, landmark, guardian, shard, and restoration readability.
- Compared every frame against the art compass's camera, rendering, palette, outline, landmark, and originality rules.
- Regenerated the quarry once after QA found that the first version looked too much like a circular stepped arena.

The color-vision previews are design checks, not medical diagnostics.

## Results by frame

### 01 — Village spawn: superseded by the refined grammar

- Its traveler, guardian, route, and beacon hierarchy remain useful.
- Its detailed masonry, shingles, debris, cart, and ruin construction fail the new primitive budget.
- Keep it only as historical composition reference.

### 02 — Lush forest traversal: retained as mood anchor, superseded as asset grammar

- The route, daylight, palette, depth, and grass treatment remain the strongest first-pass qualities.
- Rounded rocks and shrubs, bark detail, irregular modeled tree crowns, and the complex stump fail the new primitive budget.
- Keep it as the mood/composition anchor used to create Forest V2.

### 03 — Quarry guardian reveal: pass after revision

- The selected version uses one raw cut face, a collapsed rubble slope, exposed roots, and a broken haul ramp instead of concentric terraces.
- The quarry reads as an irregular work site hidden behind the foreground hill, not a purpose-built boss arena.
- Traveler, guardian, and separate shard remain distinct at phone size, in grayscale, and under both color-vision simulations.
- Environmental shadow supplies danger while the surrounding forest and daytime sky remain consistent.

### 04 — Beacon restoration: pass

- Before and after remain understandable without labels.
- The change survives grayscale because it uses crop density, ground coverage, wildlife, beacon state, and value—not hue alone.
- Burned houses, collapsed roofs, stone walls, fences, cart, and field layout remain damaged in both states.
- Cyan-white light, returning plants, and wildlife make restoration hopeful without erasing human history.

## First-pass consistency result

The four first-pass frames retain the same traveler, camera family, lighting, and magic relationship, but they no longer represent one approved asset grammar. Quarry and restoration must inherit the selected V3 construction rules after the current review.

## Deferred gates

- **Guardian 360-degree readability:** A flat concept can prove only selected angles. Before final modeling, create an orthographic front/side/back/three-quarter sheet. After modeling, inspect a real turntable through the Cinemachine camera at gameplay and boss zoom distances.
- **Unity visual parity:** Reproduce one village-to-forest material and lighting slice in the project before approving a full asset pipeline.
- **Mobile performance:** HDRP currently prevents a meaningful Android shipping benchmark. Keep source assets pipeline-neutral, create the representative URP slice after the prototype, and require a stable 30 FPS on the weakest available phone before scaling production.
- **Team approval:** Forest V3 and Village V3 are candidates, not final approvals. Future revisions should change one named variable at a time and preserve the shared prompt and construction lock.

## Project isolation

This implementation added files only under `Docs/ArtDirection`. It did not edit Unity scenes, runtime scripts, packages, render settings, controls, HUD, or existing project context. The worktree already contained unrelated changes before this art-direction task; they were preserved.
