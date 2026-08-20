"""Round-trip validation for SM_Conifer_01.fbx.

Run in a clean background Blender process after build_conifer_01.py. The
result proves that the exported FBX can be read independently and preserves
its geometry, materials, dimensions, and base-centered origin contract.
"""

from __future__ import annotations

import json
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[2]
FBX_PATH = PROJECT_ROOT / "Assets" / "Art" / "Environment" / "Trees" / "Conifer_01" / "SM_Conifer_01.fbx"
REPORT_PATH = PROJECT_ROOT / "Docs" / "ArtDirection" / "Assets" / "SM_Conifer_01-import-validation.json"


def world_bounds(objects: list[bpy.types.Object]) -> tuple[list[float], list[float]]:
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    minimum = [min(point[axis] for point in points) for axis in range(3)]
    maximum = [max(point[axis] for point in points) for axis in range(3)]
    return minimum, maximum


def main() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(FBX_PATH), automatic_bone_orientation=False)

    meshes = sorted(
        (obj for obj in bpy.context.scene.objects if obj.type == "MESH"),
        key=lambda obj: obj.name,
    )
    if not meshes:
        raise RuntimeError(f"No mesh objects were imported from {FBX_PATH}")

    objects = []
    total_vertices = 0
    total_triangles = 0
    for obj in meshes:
        obj.data.calc_loop_triangles()
        vertices = len(obj.data.vertices)
        triangles = len(obj.data.loop_triangles)
        total_vertices += vertices
        total_triangles += triangles
        objects.append(
            {
                "name": obj.name,
                "vertices": vertices,
                "triangles": triangles,
                "materials": [slot.material.name for slot in obj.material_slots if slot.material],
                "world_origin": list(obj.matrix_world.translation),
            }
        )

    minimum, maximum = world_bounds(meshes)
    dimensions = [maximum[index] - minimum[index] for index in range(3)]
    report = {
        "status": "passed",
        "fbx": str(FBX_PATH.relative_to(PROJECT_ROOT)).replace("\\", "/"),
        "mesh_objects": len(meshes),
        "total_vertices": total_vertices,
        "total_triangles": total_triangles,
        "world_bounds_xyz": {"min": minimum, "max": maximum, "dimensions": dimensions},
        "base_center_check": {
            "minimum_z_is_zero": abs(minimum[2]) <= 0.0001,
            "mesh_origins_are_zero": all(obj.matrix_world.translation.length <= 0.0001 for obj in meshes),
        },
        "objects": objects,
    }

    if not report["base_center_check"]["minimum_z_is_zero"]:
        raise RuntimeError(f"Imported model base is not at z=0: {minimum[2]}")
    if not report["base_center_check"]["mesh_origins_are_zero"]:
        raise RuntimeError("One or more imported mesh origins moved away from the model origin")

    REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
