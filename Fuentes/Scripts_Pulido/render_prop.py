# Pulido de mallas (P4): saca fotos de un prop (cada LOD por separado, de dos lados) para comparar el original con el pulido.
#   blender.exe -b <archivo.blend> --python render_prop.py -- <carpeta_salida> <prefijo>
import sys, math, os
import bpy
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
folder, prefix = args[0], args[1]
os.makedirs(folder, exist_ok=True)
scene = bpy.context.scene
engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
scene.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
scene.render.resolution_x, scene.render.resolution_y = 640, 640
world = scene.world or bpy.data.worlds.new("Mundo")
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.6, 0.68, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.8
sun = bpy.data.objects.new("Sol_Foto", bpy.data.lights.new("Sol_Foto", type='SUN'))
sun.data.energy = 3.0
scene.collection.objects.link(sun)
sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(140))
cam = bpy.data.objects.new("Camara_Foto", bpy.data.cameras.new("Camara_Foto"))
scene.collection.objects.link(cam)
scene.camera = cam

meshes = [o for o in bpy.data.objects if o.type == 'MESH']
for lod in ["LOD0", "LOD1", "LOD2"]:
    group = [o for o in meshes if o.name.endswith(lod)]
    if not group:
        continue
    for o in meshes:
        o.hide_render = o not in group
    corners = [o.matrix_world @ Vector(c) for o in group for c in o.bound_box]
    lo = Vector((min(c.x for c in corners), min(c.y for c in corners), min(c.z for c in corners)))
    hi = Vector((max(c.x for c in corners), max(c.y for c in corners), max(c.z for c in corners)))
    center, size = (lo + hi) / 2, max((hi - lo).length, 0.05)
    for side, direction in (("a", Vector((1.0, -1.2, 0.6))), ("b", Vector((-1.1, 0.9, 0.4)))):
        cam.location = center + direction.normalized() * size * 1.6
        cam.rotation_euler = (center - cam.location).to_track_quat('-Z', 'Y').to_euler()
        cam.data.lens = 50
        scene.render.filepath = os.path.join(folder, f"{prefix}_{lod}_{side}.png")
        bpy.ops.render.render(write_still=True)
print("[render_prop] listo")
