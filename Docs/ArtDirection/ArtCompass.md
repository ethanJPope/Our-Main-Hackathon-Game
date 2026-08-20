# Main Hackathon Game — Art Compass

**Status:** Clean-reboot V4 active; simple-shape starting-soldier candidate awaiting approval with the overall theme  
**Target:** Landscape mobile, stable 30 FPS on the weakest available test phone  
**Prototype scope:** One region, one hero preset, one guardian boss, one optional beacon restoration  
**Primary visual references:** [Clean-reboot reference set](References/CleanReboot-2026-08-19/README.md)

The clean-reboot references define rendering mood, tree shape, grass shape, density, and composition as separate inputs. Derive construction and lighting principles from them, never their exact layouts or landmarks. V1–V9 remain decision history and are no longer active style sources. Characters, landmarks, symbols, architecture, creatures, and scene layouts must remain original.

## One-Sentence Direction

A handcrafted low-poly painterly wilderness where warm canopy light reveals inviting paths through cool teal-green depth, visible history hides beneath lush growth, and restrained cyan-white beacons suggest quiet magic.

## Four Pillars

### 1. Natural wonder

Exploration should feel inviting before it feels dangerous. Use layered vegetation, irregular terrain, distant landmarks, wildlife, and sunlight to create curiosity. The normal world is colorful and alive; gloom is local to danger instead of covering the whole game.

### 2. Functional asymmetry

Asymmetry must look caused, not random. A roof sags where it burned, one shoulder carries a repaired mantle, a golem arm is built from heavier stones, and a trail bends around roots. Avoid mirrored wear, evenly spaced damage, and decorative clutter without a story.

### 3. Visible world history

Ruins, repairs, paths, abandoned tools, and regrowth explain what happened without text. Magic heals magical damage, but it does not erase human choices. Relighting a beacon restores nature while the raided village remains ruined.

### 4. Mobile readability

Silhouette, value, motion, and spacing carry gameplay information. Fine detail is secondary. At phone size, the player, enemy type, attack direction, route, shard, and landmark must remain understandable without relying only on hue.

## Rendering Language

### Geometry and shape

- Use chunky, irregular handcrafted low-poly geometry with visible broad facets and strong readable silhouettes.
- Build organic complexity from overlapping simple masses, asymmetrical clustering, scale variation, and broad value groups—not fine geometry.
- Give every asset a short repeatable shape recipe while avoiding perfect primitives, voxel grids, sterile symmetry, and procedurally even spacing.
- Layer vegetation from tapered conifer branch masses to squared ribbon grass, broad leafy ground cover, sparse flowers, roots, logs, and beveled stones.
- Keep large and medium forms dominant. Tiny accents remain sparse and subordinate to traversal and landmark readability.
- Let objects overlap and crop at the frame edges so the forest feels like a dense physical diorama without a floating base.
- Keep terrain broad and playable. Use tree walls, light pools, path bends, roots, slab steps, ridges, and clearings to shape movement.
- Natural forms may be chunky and faceted, but they must not become smooth, toy-like, perfectly cubical, or excessively detailed.

### Primitive construction grammar

This section is a production constraint, not a loose suggestion.

| Asset family | Approved construction | Reject |
| --- | --- | --- |
| Conifer foliage | Several overlapping downward-hanging tapered polygonal branch masses made from broad shallow ribbon-like slabs; irregular lengths and offsets combine into a strong triangular silhouette; use a few broad warm/cool value groups | Perfect stacked cones, flat cone cutouts, individual needles, realistic branching, identical repeated trees, rounded blobs, or dense twig systems |
| Trunks and branches | One irregular faceted trunk mass with broad planes; only a few large branch or root forms where composition needs them | Realistic bark grooves, twig webs, fine roots, knots, or cylindrical smooth trunks |
| Grass | Dense clumps of broad squared flat ribbon blades; varied heights and slight width variation; mostly straight upright blades with a few gentle bends; small ground gaps; below knee height at gameplay scale | Bamboo posts, tubes, cylinders, ultra-thin hairlines, fuzzy realistic grass, identical picket rows, excessive height, or perfectly even spacing |
| Groundcover | Broad simplified leafy masses and low overlapping polygonal clusters that create soft organic borders around grass and paths | Individual realistic leaves, noisy carpets, smooth sphere bushes, procedural repetition, or foliage that erases traversal edges |
| Flowers | Sparse tiny clusters in purple, pale cyan-white, yellow, and warm orange-brown; use broad simple marks rather than botanical detail | Excessive flowers, modeled petals and stamens, evenly distributed color noise, or flowers competing with landmarks |
| Rocks and ruins | Chunky irregular beveled slabs and boulders with broad visible facets; mix exposed stones with pieces buried one-third to two-thirds into the ground | Perfect cubes, identical boxes, smooth rounded stones, tiny cracks, chips, pebbles, or dense rubble |
| Fallen logs and roots | One simple roughly squared trunk built from 2–3 joined low-sided masses; 4–6 broad visible planes; a slight angular kink or taper; solid uneven angled ends; 2–3 broad orange-brown value regions; only 1–2 thin flat moss shapes on top | Perfect manufactured beams, smooth cylinders, hollow ends or cavities, realistic bark, grain, growth rings, splinters, dense moss, or fine root networks |
| Village buildings | A few chunky irregular wall, timber, and roof masses with broad facets; show damage by removing, tilting, shortening, or darkening whole pieces | Toy blocks, perfect modular cubes, shingles, individual masonry, complex joints, wheel spokes, or debris carpets |
| Guardian | Roughly 12–20 large asymmetric irregular low-poly boulders with broad faces, varied proportions, a few flat moss patches, and fracture marks | Perfect boulder boxes, hundreds of stones, tangled roots, pebble detail, or ornamental armor |
| Beacon | A few large chipped crystal prisms on a simple block shrine | Filigree, tiny crystal clusters, ornate machinery, or dense carvings |

The forest should feel complex because a limited library of chunky silhouettes overlaps in asymmetrical layers. Each asset remains simple enough for mobile, while painterly value grouping, contact occlusion, and dappled sunlight create richness.

### Surfaces

- Use matte, nonmetallic, high-roughness materials with restrained specular response.
- Author broad hand-painted tonal variation and subtle per-face color differences so assets feel intentionally modeled and painted by hand.
- Keep all surface variation low frequency: large warm/cool shifts, broad moss shapes, restrained edge light, and simple value planes.
- Let facets, overlap, contact occlusion, sunlight, and cool ambient fill remain stronger than painted detail.
- Wood, stone, foliage, cloth, and soil may use compact shared atlases or vertex colors, but texture density stays low and consistent.
- Reserve emissive response and sharper highlights for crystal and magic; the cyan-white beacon remains a controlled focal accent.
- Reject realistic bark, grain, tiny cracks, photo scans, noisy normal maps, glossy plastic, metallic ordinary surfaces, and high-frequency painterly clutter.

The clean-reboot material rule supersedes the V6 flat-color experiment. V6 remains historical calibration only.

### Lighting and depth

- Broad warm sunlight filters through the canopy in irregular dappled patches that reveal routes and landmarks.
- Cool green-blue ambient fill shapes deep teal forest shadows without making the world globally black or hostile.
- Use soft shadows, strong contact occlusion, gentle atmospheric haze, restrained bloom, and filmic color grading.
- Keep the midground, player space, landmark, and traversal boundaries crisp. Only cropped foreground vegetation and far edges receive gentle depth-of-field softness.
- Highlights become warmer and more yellow-chartreuse; shadows become cooler and bluer.
- Boss spaces may deepen the same lighting language through terrain, overhangs, dust, and canopy rather than switching to a different global filter.

### Outlines

- Do not use outlines on characters, creatures, props, terrain, foliage, architecture, or effects.
- Preserve mobile readability through silhouette, value separation, rim light, contact shadow, animation, spacing, and controlled color contrast.

## Camera and Composition Contract

Visual concepts and later assets must be judged through the actual gameplay framing:

| Setting | Approved value |
| --- | --- |
| Camera system | Cinemachine 3 orbital follow |
| Projection | Soft perspective |
| Field of view | 40 degrees |
| Orbit radius | 12 world units |
| Starting elevation | 35 degrees |
| Starting heading | 45 degrees |
| Player framing | Slightly below screen center |
| Rotation | Full horizontal orbit around the player |
| Major boss framing | Modest zoom-out; preserve the same angle and controls |
| Horizon | Never visible during normal isometric gameplay |
| Depth of field | Midground and traversal crisp; only gentle foreground/far-edge softness |

Every signature asset needs a readable 360-degree silhouette. Do not approve an asset that works only from one concept-art angle.

## Color Script

These swatches establish relationships, not hard material constants.

| Role | Color family | Starting swatches |
| --- | --- | --- |
| Deep evergreen | Cool teal-green shadow | `#0D302D`, `#17463F` |
| Living foliage | Muted evergreen and moss | `#2E5B3A`, `#55793A`, `#78933F` |
| Sunlit foliage | Warm yellow-chartreuse | `#A4B943`, `#C7CD53` |
| Soil and timber | Warm grounded brown | `#59432F`, `#7B5734`, `#A6743D` |
| Fieldstone | Cool green-gray neutral | `#5B7068`, `#819184`, `#A8AE91` |
| Dry crops and paths | Warm ochre | `#A77C36`, `#C9A34D` |
| Wildflower accents | Violet, blue, soft white | `#7667C7`, `#78A8D8`, `#E7E9D6` |
| Corruption body | Bruised violet-black | `#211A28`, `#38263F` |
| Corruption fractures | Muted luminous violet | `#76508E`, `#A36BC1` |
| Beacon core | Cyan-white | `#DFFFFF`, `#91E4E8` |
| Fire and human warmth | Restrained amber | `#D49A45`, `#F0BD66` |

Player-selected spell hue may change, but spell identity must also come from glyph, silhouette, motion, sound, and timing. Corruption and beacon colors never change with the player palette.

## Original Motifs

### Fractures

Fractures appear in damaged beacons, corrupted stone, major creatures, ruins touched by beacon magic, and spell impacts. Use a few large branching cracks instead of uniform glowing veins.

### Handwritten script

Magic uses imperfect handwritten marks: interrupted circles, offset strokes, corrections, and overlapping symbols. Script may be carved, painted, briefly projected, or drawn through the air. It should feel authored by people, not generated by a perfect machine.

### Crystal blades

Beacon crystals use tall, chipped blade silhouettes. The main beacon is one monumental fractured crystal piercing the mountain summit. Regional beacons are smaller crystal towers with related proportions, never identical copies.

## First Region: The Raided Farmland

### Story geography

1. The player begins in a destroyed farming village under bright midday light.
2. A hunched stone guardian is visible near the village and is clearly too dangerous for an immediate fight.
3. Natural composition leads the player around it and into the evergreen forest.
4. The damaged regional beacon stands at a broken hill shrine overlooking the farms.
5. When the player later returns, the guardian has withdrawn.
6. A hill hides an overgrown quarry bowl. Cresting it reveals the true boss space.
7. The guardian protects the stolen beacon shard separately inside the quarry.
8. Defeating it lets the player retrieve the shard and optionally return it to the hill beacon.

### Destroyed village

- Architecture: box walls, large fieldstone blocks, a few chunky timber bars, broad roof slabs, simple rope, troughs, and block-built tools.
- Damage is old: blackened whole beams, missing or tilted roof slabs, broken fence bars, trampled soil, broad dead crop rows, and cool ash fields. There are no active flames.
- Keep each house legible as a handful of large construction pieces. Do not model individual shingles, masonry units, wheel spokes, splinters, or scattered rubble carpets.
- Keep the layout open enough for camera rotation and mobile navigation.
- The village has no surviving settlement population. Friendly people appear later as lone travelers and traders.
- Restoration does not rebuild houses or erase burn scars.

### Evergreen forest

- Conifers made from overlapping tapered polygonal branch masses dominate the silhouette and follow the clean-reboot tree-shape reference.
- Use broad squared ribbon grass, chunky leafy ground cover, beveled stones at mixed burial depths, mossy roots, faceted logs, sparse color accents, and irregular warm sun patches.
- Keep individual assets simple and chunky. Density, overlap, faceted value shifts, asymmetry, and broad light pools create the organic result.
- Dense edges frame clearer traversable centers. Never hide the player beneath continuous foliage.
- Paths are shown by compressed grass, warmer soil, leaning plants, open sky, and light—not glowing arrows or magical breadcrumbs.
- Most forest is healthy and beautiful. Corruption appears as localized wounds near danger.

### Quarry boss bowl

- The hill fully conceals the quarry until the reveal moment.
- The space remains a believable abandoned quarry, not a circular purpose-built arena.
- Use terraced cuts, broken extraction faces, overgrown ramps, pooled dust, exposed roots, and scattered work remnants.
- High stone walls and overhangs create cooler shadow while the normal midday sky remains visible above.
- The separate shard sits on or beside a fractured stone rest where it reads clearly from the entrance.
- Violet-black corruption concentrates around the guardian, shard, and a few quarry fractures; it does not repaint every surface.

## Restoration States

| Layer | Before relighting | After relighting |
| --- | --- | --- |
| Beacon | Dark, broken, weak violet contamination | Stable cyan-white core and rising light |
| Corruption | Fractures, soot-like growth, sparse haze | Retracts or becomes inert dark scars |
| Plants | Thin, gray-green, interrupted growth near wounds | Fuller green layers, flowers, crop recovery |
| Wildlife | Absent, hiding, or agitated | Familiar animals and birds return |
| Lighting | Slightly flatter and cooler near wounds | Brighter local bounce and clearer color |
| Human ruins | Burned and broken | Unchanged except for natural regrowth |

The restoration reward should be obvious without rebuilding the entire map. Implement it later through controlled material parameters, toggled foliage groups, particles, ambient audio, wildlife spawns, and selective prop-state swaps.

## Characters and Creatures

### Starting soldier

- Adult male soldier beginning the game without his armor; one polished preset in the first prototype.
- Deliberately short and broad proportions, approximately four-and-a-half heads tall, with broad shoulders and torso, sturdy arms and legs, and a low center of gravity.
- Keep the stocky build grounded and heroic. Reject dwarf, chibi, obese, bodybuilder, or comedy-caricature proportions.
- Build the complete starting body from roughly 12–16 visibly separate simple low-poly pieces: one faceted block head and one angular hair cap, one broad torso wedge, two-piece prism arms with block hands, one hip block and flat belt, one tapered prism per leg, one wedge per boot, and a three-piece sword/scabbard.
- Use broad flat faces, obvious angular plane breaks, slight taper, and restrained asymmetry. Each basic piece has one plain base color; scene lighting supplies face shading.
- Reject smooth sculpted anatomy, round cylinders or spheres, realistic shoulder and muscle forms, clothing folds, wrinkles, seams, piping, cuffs, stitching, buckles, fingers, hair strands, facial micro-detail, leather grain, and small decorative geometry.
- Starting silhouette: short dark hair, plain faded rust-brown long-sleeve under-tunic, dark trousers, sturdy leather boots, and one simple wide belt.
- Starting weapon: weathered one-handed sword, roughly 10–15 percent oversized for mobile readability, carried in a plain scabbard at the left hip.
- No starting helmet, shield, mantle, cloak, backpack, metal plates, chainmail, bracers, pauldrons, or other visible armor.
- Face: simple expressive planes, readable brows and eyes, minimal small detail.
- Equipment later defines identity. New gear changes strong silhouette zones such as head, shoulders, weapon, torso, and back.
- Future body, skin, hair, face, and curated dye choices are planned but not implemented in the first prototype.

### Masked scavengers

- Grounded human proportions with cloth or carved salvage masks.
- Mismatched layers, patched packs, stolen tools, farm implements, short weapons, and repaired armor.
- Repeat two or three faction motifs while varying shoulders, masks, and carried gear.
- Avoid uniform soldiers, comedy bandits, modern tactical gear, and exaggerated spikes.

### Wildlife

- Start from recognizable deer, foxes, birds, and boars, simplified into angular regional silhouettes.
- Healthy wildlife has no constant glow. Magical change must remain noticeable.
- Corruption preserves the base silhouette but adds broken motion, localized violet fractures, and sparse soot-like growth.

### Living-mountain guardian

- Approximately four times the player's height.
- Hunched boulder giant with long heavy arms, a low head, uneven shoulders, and one visibly heavier side.
- Built from a small set of large irregular low-poly boulder masses, packed-soil color planes, flat moss patches, and a few restrained crystal traces.
- Its silhouette must resemble terrain while dormant and become unmistakably alive when it unfolds.
- Corruption appears through large violet-black fractures and broken handwritten marks, not a shard embedded in its chest.
- The guarded shard remains a separate object in the quarry.
- Animation is slow and weighty between sudden committed bursts. Impacts use dust, stone chips, brief magic, and camera response without gore.

## Magic and Combat Effects

- Magic is learned from scrolls and visualized as handwritten strokes becoming physical force.
- Each spell family needs a unique silhouette and motion pattern before color is applied.
- Keep effects bold and brief so attacks read without obscuring the player or boss.
- Telegraphs use shape, direction, timing, pose, and value contrast in addition to hue.
- Damage effects are stylized and non-gory: dust, sparks, chips, short trails, and magical bursts.
- Beacon magic uses stable cyan-white light and upward motion. Corruption uses interrupted violet strokes, downward residue, and irregular pulses.

## Regional Variation Rule

Future regions must change nature and culture together. Each region defines:

- one dominant terrain rhythm;
- one vegetation silhouette family;
- one local building method and material hierarchy;
- one weather or atmospheric behavior;
- one wildlife family;
- one restrained accent palette;
- one regional interpretation of beacon structures and handwritten script.

Do not create a new biome by recoloring the first region's trees and rocks.

## Asset and Rendering Contract

### Custom signature assets

Build the player, living-mountain guardian, regional beacon, main mountain beacon, shard, key village kit, major enemy silhouettes, and region-defining trees specifically for this game.

### Secondary asset packs

Rocks, small plants, flowers, debris, and generic props may begin from selected packs only when:

- their silhouettes fit the compass;
- materials are replaced or unified;
- color and value ranges match the region palette;
- their geometry scale, painterly texture density, roughness, and warm/cool value relationships are consistent;
- recognizable pack-demo compositions are not reused.

### Pipeline compatibility

The prototype currently uses HDRP 17.5.0 in Unity 6000.5.8f1. Keep HDRP temporarily, but author meshes, compact painterly atlases, vertex colors, palettes, masks, and VFX concepts independently of HDRP-only features. Avoid building the identity around proprietary HDRP shaders, ray tracing, high-cost volumetrics, or effects that cannot be recreated in URP.

Before expanding production, migrate or recreate one representative village-to-forest slice in URP and profile it on the weakest available phone. The gate is a stable 30 FPS with representative foliage density, painterly materials, particles, contact shadows, and camera rotation.

## Do / Don't

### Do

- Compose with large readable masses and clear traversal space.
- Use warm dappled sunlight, cool teal depth, and painterly value grouping for wonder.
- Let asymmetry explain wear, repair, growth, or weight.
- Keep corruption localized and restoration causally honest.
- Judge every asset through the rotating gameplay camera and a phone-sized preview.
- Use original landmarks, scripts, silhouettes, and creature designs.
- Reuse a small handcrafted shape library and create richness through clustering, overlap, broad painterly shifts, contact occlusion, and sunlight.
- Make damage by removing, rotating, or darkening whole construction pieces.
- Build foliage from tapered shallow polygonal masses and grass from broad flat ribbons; reserve chunkier faceted volume for rocks, logs, roots, terrain, architecture, and characters.
- Mix exposed rocks with partially buried stones so the ground feels grown-in rather than decorated with boxes.

### Don't

- Drift toward realistic medieval grit, plastic cartoon rendering, or generic flat low poly.
- Cover normal exploration in permanent fog, gloom, bloom, or magical effects.
- Use outlines anywhere in the scene.
- Use glowing route markers when composition can guide the player.
- Rebuild burned homes when a beacon is restored.
- Copy recognizable characters, symbols, architecture, enemies, trees, or interface elements from existing game franchises.
- Approve a concept that cannot plausibly run or remain readable on mobile.
- Model individual needles or leaves, realistic bark, fine root webs, smooth sphere bushes, fuzzy grass, tiny cracks, shingles, masonry units, wheel spokes, or decorative rubble.
- Confuse “low poly” with many small faceted parts; fewer, larger shapes are the target.
- Confuse “simple” with “everything is a perfect rectangular prism.”
- Add fine grain, noisy mottling, photo detail, or high-frequency texture that competes with the broad facets.

## Approval Checklist

An environment or asset is approved only when all applicable answers are yes:

- Does it support one or more of the four pillars?
- Does its silhouette read at gameplay distance and from multiple orbit angles?
- Is important information carried by shape and value, not color alone?
- Are large and medium forms stronger than surface noise?
- Can its apparent detail be explained by handcrafted masses, overlap, broad painterly value grouping, contact occlusion, and light rather than complex individual meshes?
- Are material shifts broad, matte, low frequency, and subordinate to silhouette and facets?
- Do tree masses, grass ribbons, rocks, roots, and wood retain controlled low-poly irregularity instead of looking voxel-built or procedurally repeated?
- Does asymmetry have an understandable cause?
- Does it preserve the bright-world/local-danger contrast?
- Does it use the fracture, script, or crystal language only where appropriate?
- Is it original rather than a recognizable copy?
- Can it be authored without dependence on HDRP-only features?
- Does it remain clear in a phone-sized downsample?

## Current Scope Boundary

This compass restarts world art only. Existing controls, HUD, hotbar, vitals, joystick, dodge button, and camera-control behavior remain untouched. Towns, full character customization, additional regions, PC support, and final render-pipeline migration are outside the first two-week art slice.
