import argparse
import hashlib
import json
import os
import sys

import bpy


def parse_args():
    separator = sys.argv.index("--") if "--" in sys.argv else len(sys.argv)
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--format", choices=("glb", "obj"), required=True)
    parser.add_argument("--scale", type=float, required=True)
    parser.add_argument("--apply-transforms", choices=("true", "false"), required=True)
    parser.add_argument("--triangulate", choices=("true", "false"), required=True)
    parser.add_argument("--report", required=True)
    return parser.parse_args(sys.argv[separator + 1 :])


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def import_model(path):
    extension = os.path.splitext(path)[1].lower()
    if extension == ".blend":
        bpy.ops.wm.open_mainfile(filepath=path, load_ui=False, use_scripts=False)
    elif extension == ".fbx":
        bpy.ops.import_scene.fbx(filepath=path, use_anim=False)
    elif extension == ".obj":
        if hasattr(bpy.ops.wm, "obj_import"):
            bpy.ops.wm.obj_import(filepath=path)
        else:
            bpy.ops.import_scene.obj(filepath=path)
    elif extension in (".glb", ".gltf"):
        bpy.ops.import_scene.gltf(filepath=path, import_pack_images=True)
    else:
        raise RuntimeError(f"Unsupported input extension: {extension}")


def normalize_scene(scale, apply_transforms, triangulate):
    meshes = sorted(
        (item for item in bpy.context.scene.objects if item.type == "MESH"),
        key=lambda item: item.name,
    )
    if not meshes:
        raise RuntimeError("Input contains no mesh object.")
    for index, item in enumerate(meshes):
        item.name = f"Mesh{index:03d}"
        item.data.name = f"MeshData{index:03d}"
        item.scale = tuple(component * scale for component in item.scale)
        bpy.context.view_layer.objects.active = item
        item.select_set(True)
        if apply_transforms:
            bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        if triangulate:
            modifier = item.modifiers.new(name="P4M_Triangulate", type="TRIANGULATE")
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        item.select_set(False)
    return meshes


def export_model(path, target_format):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if target_format == "glb":
        bpy.ops.export_scene.gltf(
            filepath=path,
            export_format="GLB",
            export_apply=True,
            export_animations=False,
            export_cameras=False,
            export_lights=False,
            export_extras=False,
        )
    elif hasattr(bpy.ops.wm, "obj_export"):
        bpy.ops.wm.obj_export(filepath=path, export_materials=True, export_triangulated_mesh=True)
    else:
        bpy.ops.export_scene.obj(filepath=path, use_materials=True, use_triangles=True)


def main():
    args = parse_args()
    if os.path.splitext(args.input)[1].lower() != ".blend":
        clear_scene()
    import_model(args.input)
    meshes = normalize_scene(
        args.scale,
        args.apply_transforms == "true",
        args.triangulate == "true",
    )
    export_model(args.output, args.format)
    triangles = sum(len(item.data.polygons) for item in meshes)
    materials = {material.name for item in meshes for material in item.data.materials if material}
    textures = {image.name for image in bpy.data.images if image.source != "VIEWER"}
    report = {
        "blenderVersion": bpy.app.version_string,
        "meshCount": len(meshes),
        "triangleCount": triangles,
        "materialCount": len(materials),
        "textureCount": len(textures),
        "warnings": [],
    }
    with open(args.report, "w", encoding="utf-8", newline="\n") as stream:
        json.dump(report, stream, indent=2, sort_keys=True)
        stream.write("\n")


if __name__ == "__main__":
    main()
