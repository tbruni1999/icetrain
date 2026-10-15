# Pulido de mallas (P4): saca fotos de un .blend del vagón desde vistas fijas, para comparar el original con el pulido.
#   blender.exe -b <archivo.blend> --python render_comparar.py -- <carpeta_salida> <prefijo>
import sys, math, os
import bpy
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
folder, prefix = args[0], args[1]
os.makedirs(folder, exist_ok=True)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
scene.render.resolution_x, scene.render.resolution_y = 1280, 720
scene.render.film_transparent = False
world = scene.world or bpy.data.worlds.new("Mundo")
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.6, 0.68, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.8

sun_data = bpy.data.lights.new("Sol_Foto", type='SUN')
sun_data.energy = 3.0
sun = bpy.data.objects.new("Sol_Foto", sun_data)
scene.collection.objects.link(sun)
sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(140))
cam_data = bpy.data.cameras.new("Camara_Foto")
cam = bpy.data.objects.new("Camara_Foto", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam

def shot(name, location, target, lens=35):
    cam.location = Vector(location)
    direction = Vector(target) - cam.location
    cam.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
    cam_data.lens = lens
    scene.render.filepath = os.path.join(folder, f"{prefix}_{name}.png")
    bpy.ops.render.render(write_still=True)

# Ejes del .blend: +Y adelante del archivo (la cabina está en -Y), +Z arriba.
shot("costado", (9.0, -14.0, 2.5), (0.0, -3.0, 1.8))
shot("bogie", (3.2, -6.5, 0.9), (0.0, -6.5, 0.5), lens=28)
shot("frente", (5.0, -17.0, 2.2), (0.0, -9.5, 1.6), lens=40)
print("[render] listo")
