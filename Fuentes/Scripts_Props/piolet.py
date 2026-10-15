import sys, math; sys.path.insert(0, '/home/claude/skill_new/scripts'); from kit import *
from mathutils import Vector
reset()
bm = nuevo_bm()
# Piolet para hielo: mango de madera a lo largo de Y (regatón en y=0, cabeza arriba), pico hacia +Z, azuela hacia -Z.
L = 0.66; yh = 0.625                                           # largo total y altura del centro de la cabeza
# --- mango, puño de cuero y regatón
cilindro(bm, (0, 0.070, 0), (0, yh - 0.02, 0), 0.0130, seg=12, mat=0)            # mango de madera
cilindro(bm, (0, 0.085, 0), (0, 0.265, 0), 0.0146, seg=12, mat=3)                # puño de cuero
cilindro(bm, (0, 0.040, 0), (0, 0.085, 0), 0.0150, seg=12, mat=1)                # virola del regatón
tubo(bm, [(0, 0.000, 0), (0, 0.042, 0)], [0.0012, 0.0128], seg=12, mat=1)        # punta del regatón
# --- cabeza
c = 0.004; hy, hz = 0.025, 0.022                                                # cuerpo de la cabeza con chaflanes
perfil_extruido(bm, [(yh - hy + c, -hz), (yh + hy - c, -hz), (yh + hy, -hz + c), (yh + hy, hz - c), (yh + hy - c, hz),
                     (yh - hy + c, hz), (yh - hy, hz - c), (yh - hy, -hz + c)], -0.009, 0.018, mat=2)
cilindro(bm, (0, yh - 0.045, 0), (0, yh - 0.020, 0), 0.0150, seg=12, mat=1)      # cuello que abraza el mango
# pico curvo con dientes del lado del mango
arriba, abajo = [], []
n = 16
for i in range(n + 1):
    t = i / n
    cy = yh - 0.040 * t ** 1.6; cz = 0.020 + 0.170 * t
    w = 0.030 * (1 - t) + 0.004 * t
    arriba.append((cy + w / 2, cz))
    d = 0.0035 if (t > 0.4 and i % 2 == 1 and t < 0.95) else 0.0
    abajo.append((cy - w / 2 - d, cz))
perfil_extruido(bm, arriba + list(reversed(abajo)), -0.004, 0.008, mat=2)
# azuela (pala ancha para tallar escalones)
caja(bm, (0, yh, -0.034), (0.016, 0.022, 0.026), mat=2)                         # cuello de la azuela
az = [(yh + 0.004 - 0.006 * (k / 6) ** 2, -0.045 - 0.055 * k / 6) for k in range(7)]
perfil_extruido(bm, [(y + 0.003, z) for y, z in az] + [(y - 0.003, z) for y, z in reversed(az)], -0.024, 0.048, mat=2)
piolet = terminar('Piolet_LOD0', bm, ['madera', 'acero_oscuro', 'metal_gastado', 'cuero'],
                  bisel=0, apoyado=False, angulo=50)
pivote(piolet, 'punto', (0, 0.17, 0))                                           # agarre: en el puño
raiz = agrupar('Piolet', [piolet])
reporte([piolet], categoria='A')
preview([piolet], '/home/claude/props/prev/piolet.png', vistas=('34', 'cerca'))
import bpy as _b
cam_obj = None
sc = _b.context.scene
_b.ops.object.camera_add(location=(0.32, 0.50, 0.12)); cam = _b.context.active_object; cam.data.lens = 50; cam.data.clip_start = 0.005
cam.rotation_euler = (Vector((0, 0.625, 0.04)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
_b.ops.object.light_add(type='SUN'); sol = _b.context.active_object; sol.rotation_euler = (math.radians(50), 0, math.radians(-35))
w = _b.data.worlds.new('W2'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (0.62, 0.66, 0.72, 1)
for o in _b.data.objects:
    if o.type == 'MESH' and o.name != 'Piolet_LOD0': o.hide_render = True
sc.camera = cam; sc.cycles.samples = 16; sc.render.filepath = '/home/claude/props/prev/piolet_cabeza.png'; _b.ops.render.render(write_still=True)
for o in _b.data.objects: o.hide_render = False
_b.data.objects.remove(cam, do_unlink=True); _b.data.objects.remove(sol, do_unlink=True)
exportar([piolet], 'Piolet', '/mnt/user-data/outputs/Piolet', estatico=False)
