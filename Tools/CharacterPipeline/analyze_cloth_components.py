"""Print connected-component statistics for a Clothy OBJ mesh."""

from __future__ import annotations

import argparse
import json
import sys

import bpy


parser = argparse.ArgumentParser()
parser.add_argument("--mesh", required=True)
args = parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.wm.obj_import(filepath=args.mesh)
mesh_object = next(obj for obj in bpy.context.selected_objects if obj.type == "MESH")
mesh = mesh_object.data

vertex_faces = [[] for _ in mesh.vertices]
for polygon in mesh.polygons:
    for vertex in polygon.vertices:
        vertex_faces[vertex].append(polygon.index)

seen = set()
components = []
for start in range(len(mesh.polygons)):
    if start in seen:
        continue
    stack = [start]
    seen.add(start)
    faces = []
    vertices = set()
    while stack:
        face_index = stack.pop()
        face = mesh.polygons[face_index]
        faces.append(face_index)
        for vertex in face.vertices:
            vertices.add(vertex)
            for neighbor in vertex_faces[vertex]:
                if neighbor not in seen:
                    seen.add(neighbor)
                    stack.append(neighbor)
    coords = [mesh.vertices[index].co for index in vertices]
    components.append(
        {
            "faces": len(faces),
            "vertices": len(vertices),
            "area": round(sum(mesh.polygons[index].area for index in faces), 6),
            "min": [round(min(coord[axis] for coord in coords), 4) for axis in range(3)],
            "max": [round(max(coord[axis] for coord in coords), 4) for axis in range(3)],
            "center": [round(sum(coord[axis] for coord in coords) / len(coords), 4) for axis in range(3)],
        }
    )

components.sort(key=lambda item: item["area"], reverse=True)
print("CLOTH_COMPONENTS=" + json.dumps({"componentCount": len(components), "components": components}))
