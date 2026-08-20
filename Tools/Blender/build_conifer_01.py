"""Build the unapproved V2 conifer design and render review images.

DESIGN REVIEW ONLY: this script deliberately does not export an FBX and does
not write anything under Unity's Assets folder. The model stays in Blender
source form until Ethan explicitly approves the design.

Run with Blender 4.3+:
    blender --background --python Tools/Blender/build_conifer_01.py
"""

from __future__ import annotations

import json
import math
import random
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[2]
SOURCE_DIR = PROJECT_ROOT / "SourceArt" / "Blender" / "Environment" / "Trees" / "DesignV2"
DOC_DIR = PROJECT_ROOT / "Docs" / "ArtDirection" / "Assets" / "DesignV2"

BLEND_PATH = SOURCE_DIR / "SM_Conifer_01-design-v2.blend"
FRONT_PREVIEW_PATH = DOC_DIR / "SM_Conifer_01-design-v2-front.png"
THREE_QUARTER_PREVIEW_PATH = DOC_DIR / "SM_Conifer_01-design-v2-three-quarter.png"
STATS_PATH = DOC_DIR / "SM_Conifer_01-design-v2-stats.json"

RANDOM_SEED = 19082026
TREE_NAME = "SM_Conifer_01_DesignV2"


def clear_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)

    for datablocks in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def create_material(name: str, rgba: tuple[float, float, float, float]) -> bpy.types.Material:
    material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    material.diffuse_color = rgba
    material.use_nodes = True
    material.metallic = 0.0
    material.roughness = 0.92

    principled = material.node_tree.nodes.get("Principled BSDF")
    if principled is not None:
        principled.inputs["Base Color"].default_value = rgba
        principled.inputs["Metallic"].default_value = 0.0
        principled.inputs["Roughness"].default_value = 0.92
        if "Specular IOR Level" in principled.inputs:
            principled.inputs["Specular IOR Level"].default_value = 0.16

    return material


def make_mesh_object(
    name: str,
    vertices: list[tuple[float, float, float]],
    faces: list[tuple[int, ...]],
    materials: list[bpy.types.Material],
    material_indices: list[int] | None = None,
) -> bpy.types.Object:
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    for material in materials:
        mesh.materials.append(material)

    if material_indices is not None:
        for polygon, material_index in zip(mesh.polygons, material_indices):
            polygon.material_index = material_index

    for polygon in mesh.polygons:
        polygon.use_smooth = False

    mesh.validate(clean_customdata=False)
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj["approval_status"] = "design review only - not exported to Unity"
    return obj


def create_trunk(material: bpy.types.Material) -> bpy.types.Object:
    sides = 8
    rings = (
        # z, radius, center x, center y, angular offset
        (0.00, 0.49, 0.00, 0.00, 0.00),
        (0.18, 0.41, 0.02, -0.01, 0.035),
        (0.92, 0.32, -0.02, 0.02, -0.020),
        (2.35, 0.24, 0.03, 0.00, 0.030),
        (4.35, 0.15, -0.02, -0.01, -0.035),
        (6.20, 0.07, 0.00, 0.00, 0.010),
    )
    side_variation = (1.00, 0.92, 1.06, 0.96, 1.03, 0.91, 1.05, 0.95)

    vertices: list[tuple[float, float, float]] = []
    for z, radius, center_x, center_y, offset in rings:
        for side in range(sides):
            angle = math.tau * side / sides + offset
            varied = radius * side_variation[side]
            vertices.append(
                (
                    center_x + math.cos(angle) * varied,
                    center_y + math.sin(angle) * varied,
                    z,
                )
            )

    faces: list[tuple[int, ...]] = [tuple(reversed(range(sides)))]
    for ring_index in range(len(rings) - 1):
        current = ring_index * sides
        following = (ring_index + 1) * sides
        for side in range(sides):
            next_side = (side + 1) % sides
            faces.append((current + side, current + next_side, following + next_side, following + side))

    top_start = (len(rings) - 1) * sides
    faces.append(tuple(top_start + side for side in range(sides)))

    trunk = make_mesh_object(f"{TREE_NAME}_Trunk", vertices, faces, [material])
    trunk["asset_role"] = "faceted trunk; only the base should remain visible"
    trunk["pivot_contract"] = "tree base at local origin"
    return trunk


def create_foliage_core(material: bpy.types.Material) -> bpy.types.Object:
    """Create a dark irregular inner mass that prevents trunk-shaped gaps."""

    sides = 14
    rings = (
        # z, average radius, center x, center y, angular offset
        # The core stays narrow at the bottom so individual hanging strips,
        # rather than a flat cone edge, define the lower silhouette.
        (0.90, 0.68, -0.03, 0.01, 0.02),
        (1.25, 1.18, 0.02, -0.01, 0.05),
        (1.62, 1.42, 0.03, -0.02, 0.07),
        (2.16, 1.34, -0.03, 0.02, -0.03),
        (2.90, 1.16, 0.04, 0.01, 0.04),
        (3.62, 0.94, -0.02, -0.02, -0.05),
        (4.28, 0.73, 0.02, 0.01, 0.03),
        (4.86, 0.53, -0.02, 0.00, -0.02),
        (5.35, 0.36, 0.01, -0.01, 0.04),
        (5.78, 0.20, 0.00, 0.01, -0.03),
        (6.34, 0.025, 0.00, 0.00, 0.00),
    )
    side_variation = (1.00, 0.94, 1.04, 0.97, 1.02, 0.93, 1.06, 0.96, 1.03, 0.92, 1.05, 0.98, 1.01, 0.95)

    vertices: list[tuple[float, float, float]] = []
    for z, radius, center_x, center_y, offset in rings:
        for side in range(sides):
            angle = math.tau * side / sides + offset
            varied = radius * side_variation[side]
            vertices.append(
                (
                    center_x + math.cos(angle) * varied,
                    center_y + math.sin(angle) * varied,
                    z,
                )
            )

    faces: list[tuple[int, ...]] = [tuple(reversed(range(sides)))]
    for ring_index in range(len(rings) - 1):
        current = ring_index * sides
        following = (ring_index + 1) * sides
        for side in range(sides):
            next_side = (side + 1) % sides
            faces.append((current + side, current + next_side, following + next_side, following + side))

    core = make_mesh_object(f"{TREE_NAME}_FoliageCore", vertices, faces, [material])
    core["asset_role"] = "dark foliage shadow mass; hides trunk between hanging panels"
    return core


def hanging_panel(
    angle: float,
    anchor_z: float,
    root_radius: float,
    outward_length: float,
    drop: float,
    width: float,
    thickness: float,
    side_sway: float,
    lower_kick: float,
) -> tuple[list[tuple[float, float, float]], list[tuple[int, ...]]]:
    """Build one long, near-vertical, shallow foliage strip.

    The centerline drops much farther than it travels outward. Four cross
    sections introduce one restrained bend while preserving broad planar
    faces and a squared lower end.
    """

    radial = Vector((math.cos(angle), math.sin(angle), 0.0))
    tangent = Vector((-math.sin(angle), math.cos(angle), 0.0))

    centers = (
        radial * root_radius + Vector((0.0, 0.0, anchor_z)),
        radial * (root_radius + outward_length * 0.24)
        + tangent * (side_sway * 0.28)
        + Vector((0.0, 0.0, anchor_z - drop * 0.36)),
        radial * (root_radius + outward_length * 0.62)
        + tangent * (side_sway * 0.72)
        + Vector((0.0, 0.0, anchor_z - drop * 0.74)),
        radial * (root_radius + outward_length + lower_kick)
        + tangent * side_sway
        + Vector((0.0, 0.0, anchor_z - drop)),
    )
    half_widths = (
        width * 0.15,
        width * 0.45,
        width * 0.51,
        width * 0.43,
    )

    overall_direction = centers[-1] - centers[0]
    normal = tangent.cross(overall_direction).normalized()

    vertices: list[tuple[float, float, float]] = []
    for normal_offset in (normal * (thickness * 0.5), normal * (-thickness * 0.5)):
        for center, half_width in zip(centers, half_widths):
            left = center - tangent * half_width + normal_offset
            right = center + tangent * half_width + normal_offset
            vertices.extend((tuple(left), tuple(right)))

    faces: list[tuple[int, ...]] = [
        (0, 2, 4, 6, 7, 5, 3, 1),
        (8, 9, 11, 13, 15, 14, 12, 10),
        (0, 8, 10, 2),
        (2, 10, 12, 4),
        (4, 12, 14, 6),
        (1, 3, 11, 9),
        (3, 5, 13, 11),
        (5, 7, 15, 13),
        (0, 1, 9, 8),
        (6, 14, 15, 7),
    ]
    return vertices, faces


def create_foliage_panels(materials: list[bpy.types.Material]) -> bpy.types.Object:
    rng = random.Random(RANDOM_SEED)

    # Long strips are grouped into ten overlapping skirts. Each strip spans
    # several skirt intervals, but the intervals are wide enough that the
    # visible portions remain long instead of reading as roof shingles.
    tiers = (
        # anchor z, root radius, outward, drop, width, count, angular offset
        # Roots already sit near the conical surface; each strip then travels
        # only a short distance outward while hanging a long distance down.
        (2.66, 1.28, 0.47, 1.78, 0.53, 20, 0.06),
        (3.10, 1.22, 0.50, 1.72, 0.52, 20, 0.27),
        (3.54, 1.14, 0.51, 1.64, 0.51, 19, 0.47),
        (3.98, 1.04, 0.50, 1.54, 0.49, 19, 0.12),
        (4.40, 0.93, 0.48, 1.42, 0.47, 18, 0.36),
        (4.80, 0.80, 0.45, 1.29, 0.44, 17, 0.58),
        (5.16, 0.66, 0.41, 1.13, 0.41, 16, 0.19),
        (5.48, 0.52, 0.36, 0.96, 0.38, 15, 0.43),
        (5.75, 0.38, 0.30, 0.78, 0.35, 14, 0.09),
        (5.97, 0.25, 0.23, 0.58, 0.31, 12, 0.39),
    )

    vertices: list[tuple[float, float, float]] = []
    faces: list[tuple[int, ...]] = []
    material_indices: list[int] = []

    def append_panel(
        panel_vertices: list[tuple[float, float, float]],
        panel_faces: list[tuple[int, ...]],
        material_index: int,
    ) -> None:
        offset = len(vertices)
        vertices.extend(panel_vertices)
        faces.extend(tuple(index + offset for index in face) for face in panel_faces)
        material_indices.extend([material_index] * len(panel_faces))

    for tier_index, (anchor_z, root_radius, outward, drop, width, count, angular_offset) in enumerate(tiers):
        for panel_index in range(count):
            angle = math.tau * panel_index / count + angular_offset
            angle += rng.uniform(-0.035, 0.035)

            panel = hanging_panel(
                angle=angle,
                anchor_z=anchor_z + rng.uniform(-0.065, 0.065),
                root_radius=root_radius * rng.uniform(0.97, 1.035),
                outward_length=outward * rng.uniform(0.92, 1.08),
                drop=drop * rng.uniform(0.94, 1.06),
                width=width * rng.uniform(0.91, 1.07),
                thickness=rng.uniform(0.026, 0.044),
                side_sway=rng.uniform(-0.11, 0.11) * (1.0 - tier_index * 0.025),
                lower_kick=rng.uniform(-0.035, 0.065),
            )

            light_facing = 0.5 + 0.5 * math.cos(angle - math.radians(225.0))
            variation = rng.random()
            if light_facing > 0.70 and variation > 0.56:
                material_index = 2
            elif light_facing < 0.25 or (tier_index < 3 and variation < 0.32):
                material_index = 0
            else:
                material_index = 1
            append_panel(*panel, material_index)

    # Apex strips hang from one point and overlap the upper tiers. They are
    # not a detached cone cap.
    apex_count = 12
    for panel_index in range(apex_count):
        angle = math.tau * panel_index / apex_count + 0.12
        panel = hanging_panel(
            angle=angle,
            anchor_z=6.38 - (panel_index % 3) * 0.025,
            root_radius=0.018,
            outward_length=0.58 if panel_index % 2 == 0 else 0.52,
            drop=1.06 if panel_index % 2 == 0 else 0.96,
            width=0.38 if panel_index % 2 == 0 else 0.34,
            thickness=0.030,
            side_sway=0.035 if panel_index % 2 == 0 else -0.025,
            lower_kick=0.015,
        )
        append_panel(*panel, 2 if panel_index in (7, 8) else 1)

    foliage = make_mesh_object(
        f"{TREE_NAME}_FoliagePanels",
        vertices,
        faces,
        materials,
        material_indices,
    )
    foliage["asset_role"] = "dense near-vertical overlapping foliage strips"
    foliage["construction"] = "10 overlapping skirts plus 12 apex strips; one solid color per strip"
    return foliage


def look_at(obj: bpy.types.Object, target: tuple[float, float, float]) -> None:
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def configure_preview() -> bpy.types.Object:
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 1100
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = False

    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except Exception:
        pass

    scene.world.use_nodes = True
    world_background = scene.world.node_tree.nodes.get("Background")
    world_background.inputs["Color"].default_value = (0.16, 0.18, 0.16, 1.0)
    world_background.inputs["Strength"].default_value = 0.55

    ground_material = create_material("M_DesignPreview_Ground", (0.19, 0.22, 0.18, 1.0))
    bpy.ops.mesh.primitive_plane_add(size=20.0, location=(0.0, 0.0, -0.012))
    ground = bpy.context.active_object
    ground.name = "DesignPreviewOnly_Ground"
    ground.data.materials.append(ground_material)

    camera_data = bpy.data.cameras.new("DesignPreviewOnly_Camera")
    camera = bpy.data.objects.new("DesignPreviewOnly_Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.data.type = "ORTHO"
    camera.data.lens = 55.0
    scene.camera = camera

    key_data = bpy.data.lights.new("DesignPreviewOnly_Key", type="AREA")
    key_data.energy = 1050.0
    key_data.shape = "DISK"
    key_data.size = 5.5
    key_data.color = (1.0, 0.86, 0.62)
    key = bpy.data.objects.new("DesignPreviewOnly_Key", key_data)
    bpy.context.collection.objects.link(key)
    key.location = (-4.8, -5.5, 9.5)
    look_at(key, (0.0, 0.0, 3.0))

    fill_data = bpy.data.lights.new("DesignPreviewOnly_Fill", type="AREA")
    fill_data.energy = 520.0
    fill_data.size = 6.0
    fill_data.color = (0.38, 0.58, 0.51)
    fill = bpy.data.objects.new("DesignPreviewOnly_Fill", fill_data)
    bpy.context.collection.objects.link(fill)
    fill.location = (4.5, -1.0, 6.0)
    look_at(fill, (0.0, 0.0, 3.1))

    rim_data = bpy.data.lights.new("DesignPreviewOnly_Rim", type="AREA")
    rim_data.energy = 420.0
    rim_data.size = 4.0
    rim_data.color = (0.28, 0.48, 0.38)
    rim = bpy.data.objects.new("DesignPreviewOnly_Rim", rim_data)
    bpy.context.collection.objects.link(rim)
    rim.location = (1.5, 4.5, 7.0)
    look_at(rim, (0.0, 0.0, 3.4))

    return camera


def render_previews(camera: bpy.types.Object) -> None:
    scene = bpy.context.scene

    camera.location = (0.0, -11.5, 3.25)
    camera.data.ortho_scale = 7.25
    look_at(camera, (0.0, 0.0, 3.15))
    scene.render.filepath = str(FRONT_PREVIEW_PATH)
    bpy.ops.render.render(write_still=True)

    camera.location = (7.8, -9.6, 5.2)
    camera.data.ortho_scale = 7.55
    look_at(camera, (0.0, 0.0, 3.05))
    scene.render.filepath = str(THREE_QUARTER_PREVIEW_PATH)
    bpy.ops.render.render(write_still=True)


def world_bounds(objects: list[bpy.types.Object]) -> tuple[list[float], list[float]]:
    points: list[Vector] = []
    for obj in objects:
        points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    minimum = [min(point[axis] for point in points) for axis in range(3)]
    maximum = [max(point[axis] for point in points) for axis in range(3)]
    return minimum, maximum


def write_stats(model_objects: list[bpy.types.Object], materials: list[bpy.types.Material]) -> None:
    object_stats = []
    total_vertices = 0
    total_triangles = 0
    for obj in model_objects:
        obj.data.calc_loop_triangles()
        vertices = len(obj.data.vertices)
        triangles = len(obj.data.loop_triangles)
        total_vertices += vertices
        total_triangles += triangles
        object_stats.append(
            {
                "name": obj.name,
                "vertices": vertices,
                "triangles": triangles,
                "material_slots": [slot.material.name for slot in obj.material_slots if slot.material],
            }
        )

    minimum, maximum = world_bounds(model_objects)
    dimensions = [maximum[index] - minimum[index] for index in range(3)]
    payload = {
        "asset": TREE_NAME,
        "design_version": 2,
        "approval_status": "awaiting Ethan approval; no Unity export exists",
        "units": "meters",
        "pivot": "base center at local origin",
        "construction": {
            "main_foliage_tiers": 10,
            "apex_strips": 12,
            "trunk_visibility_target": "base only",
            "panel_orientation": "near vertical; vertical drop exceeds outward travel",
            "material_rule": "one solid color per separate foliage panel; no textures",
        },
        "bounds_blender_xyz": {"min": minimum, "max": maximum, "dimensions": dimensions},
        "total_vertices": total_vertices,
        "total_triangles": total_triangles,
        "objects": object_stats,
        "materials": [
            {
                "name": material.name,
                "base_color_rgba": list(material.diffuse_color),
                "metallic": material.metallic,
                "roughness": material.roughness,
            }
            for material in materials
        ],
        "source_blend": str(BLEND_PATH.relative_to(PROJECT_ROOT)).replace("\\", "/"),
        "front_preview": str(FRONT_PREVIEW_PATH.relative_to(PROJECT_ROOT)).replace("\\", "/"),
        "three_quarter_preview": str(THREE_QUARTER_PREVIEW_PATH.relative_to(PROJECT_ROOT)).replace("\\", "/"),
        "unity_export": None,
    }
    STATS_PATH.write_text(json.dumps(payload, indent=2), encoding="utf-8")


def main() -> None:
    for directory in (SOURCE_DIR, DOC_DIR):
        directory.mkdir(parents=True, exist_ok=True)

    clear_scene()
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    bark = create_material("M_Tree_Bark", (0.25, 0.14, 0.065, 1.0))
    foliage_core = create_material("M_Foliage_CoreShadow", (0.025, 0.105, 0.055, 1.0))
    foliage_shadow = create_material("M_Foliage_Shadow", (0.045, 0.17, 0.075, 1.0))
    foliage_mid = create_material("M_Foliage_Mid", (0.085, 0.285, 0.115, 1.0))
    foliage_sun = create_material("M_Foliage_Sun", (0.18, 0.39, 0.14, 1.0))
    design_materials = [bark, foliage_core, foliage_shadow, foliage_mid, foliage_sun]

    trunk = create_trunk(bark)
    core = create_foliage_core(foliage_core)
    panels = create_foliage_panels([foliage_shadow, foliage_mid, foliage_sun])
    model_objects = [trunk, core, panels]

    camera = configure_preview()
    write_stats(model_objects, design_materials)
    render_previews(camera)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH), check_existing=False)

    print(f"Built design-only {TREE_NAME}")
    print("Unity export: intentionally skipped pending Ethan approval")
    print(f"Blend: {BLEND_PATH}")
    print(f"Front preview: {FRONT_PREVIEW_PATH}")
    print(f"Three-quarter preview: {THREE_QUARTER_PREVIEW_PATH}")
    print(f"Stats: {STATS_PATH}")


if __name__ == "__main__":
    main()
