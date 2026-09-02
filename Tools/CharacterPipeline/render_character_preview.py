"""Render an imported character from front and rear for pipeline comparison."""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def arguments():
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--rear", action="store_true")
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])


def clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def import_asset(path: str):
    suffix = Path(path).suffix.lower()
    if suffix == ".fbx":
        bpy.ops.import_scene.fbx(filepath=path)
    elif suffix in {".glb", ".gltf"}:
        bpy.ops.import_scene.gltf(filepath=path)
    else:
        raise ValueError(f"Unsupported asset type: {suffix}")


def bounds():
    points = []
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue
        points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    minimum = Vector((min(point.x for point in points), min(point.y for point in points), min(point.z for point in points)))
    maximum = Vector((max(point.x for point in points), max(point.y for point in points), max(point.z for point in points)))
    return minimum, maximum


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def main():
    args = arguments()
    clear()
    import_asset(args.input)
    minimum, maximum = bounds()
    center = (minimum + maximum) * 0.5
    extent = max(maximum.x - minimum.x, maximum.y - minimum.y, maximum.z - minimum.z)

    camera_data = bpy.data.cameras.new("PreviewCamera")
    camera = bpy.data.objects.new("PreviewCamera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    direction = Vector((0.0, -1.0 if not args.rear else 1.0, 0.0))
    camera.location = center + direction * extent * 2.5 + Vector((0.0, 0.0, extent * 0.04))
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = extent * 1.22
    look_at(camera, center)
    bpy.context.scene.camera = camera

    key_data = bpy.data.lights.new("Key", "AREA")
    key_data.energy = 1000
    key_data.shape = "DISK"
    key_data.size = extent
    key = bpy.data.objects.new("Key", key_data)
    bpy.context.scene.collection.objects.link(key)
    key.location = camera.location + Vector((extent * 0.55, 0.0, extent * 0.7))
    look_at(key, center)

    fill_data = bpy.data.lights.new("Fill", "AREA")
    fill_data.energy = 500
    fill_data.size = extent
    fill = bpy.data.objects.new("Fill", fill_data)
    bpy.context.scene.collection.objects.link(fill)
    fill.location = center + Vector((-extent, -extent, extent * 0.4))
    look_at(fill, center)

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 700
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = args.output
    scene.world.color = (0.025, 0.025, 0.025)
    bpy.ops.wm.save_as_mainfile(filepath=str(Path(args.output).with_suffix(".blend")))
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
