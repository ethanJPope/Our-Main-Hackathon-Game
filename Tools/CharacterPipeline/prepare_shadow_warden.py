"""Prepare the Clothy cloth layers as a skinned Unity-Cloth-ready FBX.

The low-poly Clothy simulation result for this image is only two thin strips,
so it is unsuitable as the visible cape.  This script preserves the textured
render garment layers, whose ragged islands match the reference, and skins
them to the supplied Humanoid rig for controlled Unity Cloth simulation.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--body", required=True)
    parser.add_argument("--cape", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--report", required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for data_collection in (bpy.data.meshes, bpy.data.materials, bpy.data.images):
        for item in list(data_collection):
            if item.users == 0:
                data_collection.remove(item)


def world_bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return {
        "min": [min(point[index] for point in points) for index in range(3)],
        "max": [max(point[index] for point in points) for index in range(3)],
    }


def find_object(object_type):
    return next(obj for obj in bpy.context.scene.objects if obj.type == object_type)


def rename_imported_body():
    armature = find_object("ARMATURE")
    mesh = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    armature.name = "ShadowWarden_Armature"
    armature.data.name = "ShadowWarden_Armature"
    mesh.name = "ShadowWarden_Body"
    mesh.data.name = "ShadowWarden_Body"
    return armature, mesh


def make_cloth_ready_cape(armature):
    bpy.ops.wm.obj_import(filepath=args.cape)
    cape = next(obj for obj in bpy.context.selected_objects if obj.type == "MESH")
    cape.name = "ShadowWarden_ClothLayers"
    cape.data.name = "ShadowWarden_ClothLayers"

    cape_bounds = world_bounds(cape)
    low_z = cape_bounds["min"][2]
    high_z = cape_bounds["max"][2]
    vertical_range = max(high_z - low_z, 0.0001)

    group_names = [
        "mixamorig:Hips",
        "mixamorig:Spine",
        "mixamorig:Spine1",
        "mixamorig:LeftUpLeg",
        "mixamorig:RightUpLeg",
    ]
    groups = {name: cape.vertex_groups.new(name=name) for name in group_names}

    # The cape must be skinned before Unity can add a Cloth component.  These
    # weights supply only a stable animated reference pose; Unity pins the top
    # edge and simulates the remaining vertices at runtime.
    for vertex in cape.data.vertices:
        position = cape.matrix_world @ vertex.co
        vertical = (position.z - low_z) / vertical_range
        attachment_band = max(0.0, min(1.0, (vertical - 0.70) / 0.30))
        left_share = max(0.0, min(1.0, 0.5 - position.x / 0.85))
        right_share = 1.0 - left_share

        hips_weight = 0.62 - attachment_band * 0.34
        spine_weight = 0.22 + attachment_band * 0.18
        spine1_weight = 0.06 + attachment_band * 0.28
        leg_weight = (1.0 - attachment_band) * 0.10
        groups["mixamorig:Hips"].add([vertex.index], hips_weight, "REPLACE")
        groups["mixamorig:Spine"].add([vertex.index], spine_weight, "REPLACE")
        groups["mixamorig:Spine1"].add([vertex.index], spine1_weight, "REPLACE")
        groups["mixamorig:LeftUpLeg"].add([vertex.index], leg_weight * left_share, "REPLACE")
        groups["mixamorig:RightUpLeg"].add([vertex.index], leg_weight * right_share, "REPLACE")

    armature_modifier = cape.modifiers.new("Armature", "ARMATURE")
    armature_modifier.object = armature
    cape.parent = armature
    cape.matrix_parent_inverse = armature.matrix_world.inverted()

    return cape, cape_bounds


def export_fbx(objects):
    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=str(output_path),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=True,
        axis_forward="-Z",
        axis_up="Y",
        mesh_smooth_type="FACE",
        use_armature_deform_only=True,
    )


args = parse_args()
clear_scene()
bpy.ops.import_scene.fbx(filepath=args.body)
armature, body = rename_imported_body()
cape, cape_bounds = make_cloth_ready_cape(armature)
export_fbx([armature, body, cape])

body_bounds = world_bounds(body)
report = {
    "output": os.path.abspath(args.output),
    "body": {"vertices": len(body.data.vertices), "polygons": len(body.data.polygons), "bounds": body_bounds},
    "cape": {
        "vertices": len(cape.data.vertices),
        "polygons": len(cape.data.polygons),
        "bounds": cape_bounds,
        "vertexGroups": [group.name for group in cape.vertex_groups],
        "source": "Clothy render garment layers (9 islands)",
    },
    "armature": {"name": armature.name, "boneCount": len(armature.data.bones)},
}
Path(args.report).parent.mkdir(parents=True, exist_ok=True)
Path(args.report).write_text(json.dumps(report, indent=2), encoding="utf-8")
print("SHADOW_WARDEN_REPORT=" + json.dumps(report))
