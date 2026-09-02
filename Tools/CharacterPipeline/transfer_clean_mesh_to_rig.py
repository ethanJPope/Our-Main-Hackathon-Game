"""Keep the clean Hunyuan mesh and transfer only rig weights from an MIA FBX."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import bpy


def arguments():
    parser = argparse.ArgumentParser()
    parser.add_argument("--clean", required=True)
    parser.add_argument("--rig", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def largest_mesh(exclude=()):
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj not in exclude]
    return max(meshes, key=lambda obj: len(obj.data.vertices))


def remove_object(obj):
    bpy.data.objects.remove(obj, do_unlink=True)


def apply_world_transform(obj):
    """Bake the FBX importer transform into data, leaving Unity-safe identity transforms."""
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def transfer_groups(source, target):
    for group in source.vertex_groups:
        if target.vertex_groups.get(group.name) is None:
            target.vertex_groups.new(name=group.name)

    transfer = target.modifiers.new("Transfer MIA Bone Weights", "DATA_TRANSFER")
    transfer.object = source
    transfer.use_vert_data = True
    transfer.data_types_verts = {"VGROUP_WEIGHTS"}
    transfer.vert_mapping = "POLYINTERP_NEAREST"
    transfer.mix_mode = "REPLACE"
    transfer.mix_factor = 1.0
    bpy.context.view_layer.objects.active = target
    target.select_set(True)
    bpy.ops.object.modifier_apply(modifier=transfer.name)


def main():
    args = arguments()
    clear_scene()

    bpy.ops.import_scene.fbx(filepath=args.rig)
    armature = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
    weighted_source = largest_mesh()

    # The rig FBX arrives with a 0.01 scale and an axis conversion on both the
    # mesh and armature. Bake that conversion into their data before weight
    # transfer. This keeps the source surface, target surface, bone bind pose,
    # and exported Unity hierarchy in the same metre-based coordinate space.
    apply_world_transform(weighted_source)
    apply_world_transform(armature)

    bpy.ops.import_scene.gltf(filepath=args.clean)
    clean_mesh = largest_mesh(exclude=(weighted_source,))

    # Transfer only weights; it never deforms or resamples the clean mesh.
    transfer_groups(weighted_source, clean_mesh)
    clean_mesh.name = "ShadowWarden_OriginalMesh"
    clean_mesh.data.name = "ShadowWarden_OriginalMesh"
    armature.name = "ShadowWarden_Armature"
    armature.data.name = "ShadowWarden_Armature"

    armature_modifier = clean_mesh.modifiers.new("ShadowWarden Armature", "ARMATURE")
    armature_modifier.object = armature
    clean_mesh.parent = armature
    clean_mesh.matrix_parent_inverse = armature.matrix_world.inverted()

    for obj in list(bpy.context.scene.objects):
        if obj != clean_mesh and obj != armature:
            remove_object(obj)

    bpy.context.view_layer.objects.active = clean_mesh
    clean_mesh.select_set(True)
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(output),
        use_selection=False,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=True,
    )


if __name__ == "__main__":
    main()
