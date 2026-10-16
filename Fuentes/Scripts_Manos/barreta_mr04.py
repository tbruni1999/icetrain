# Barreta en estilo MR-04 (categoría A, de mano: sin nieve; pivote en el agarre), con el kit del skill de props del proyecto.
# Pedido de Tomi (2026-10-16): "re puliendo la barreta y todo"; desde ese día se agarra con UNA mano.
# Uso: blender -b -P Fuentes/Scripts_Manos/barreta_mr04.py -- <ruta_del_kit> <carpeta_salida_fbx> <carpeta_fotos>
# Convención del juego (la misma de herramientas.py): el largo a lo largo de Z (en Unity, el eje Y del modelo), el origen en
# el agarre de la mano derecha; vacíos "Agarre_Derecha" y "Punta".
import sys, os, math, json, glob
args = sys.argv[sys.argv.index('--') + 1:]
KIT, salida, fotos = args[0], args[1], args[2]
sys.path.insert(0, KIT)
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh
from mathutils import Vector
import kit
from kit import nuevo_bm, cilindro, tubo, terminar, reporte
from kit_manos import vacio, exportar

PROYECTO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
LIBRERIA = os.path.join(PROYECTO, 'Assets', '_Project', 'Art', 'Libreria')
CATALOGO = {e['nombre']: e for e in json.load(open(os.path.join(LIBRERIA, 'catalogo.json'), encoding='utf-8'))}


def material_local(nombre, receta=None, color=None, size=1024):
    """Como kit.material, pero con las texturas de la librería que ya está en el proyecto (en esta PC no hay Linux)."""
    e = CATALOGO[nombre]
    nm = e['material']
    if nm in bpy.data.materials:
        return bpy.data.materials[nm]
    m = bpy.data.materials.new(nm); m.use_nodes = True; nt = m.node_tree; b = nt.nodes['Principled BSDF']
    base = glob.glob(os.path.join(LIBRERIA, '**', nm + '_BaseColor.*'), recursive=True)
    norm = glob.glob(os.path.join(LIBRERIA, '**', nm + '_Normal.*'), recursive=True)
    ms = glob.glob(os.path.join(LIBRERIA, '**', nm + '_MetalSmooth.*'), recursive=True)
    if base:
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(base[0]); nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
    if norm:
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(norm[0]); t.image.colorspace_settings.name = 'Non-Color'
        n = nt.nodes.new('ShaderNodeNormalMap'); nt.links.new(t.outputs['Color'], n.inputs['Color']); nt.links.new(n.outputs['Normal'], b.inputs['Normal'])
    if ms:                                   # R = metal, A = suavidad (formato de Unity)
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(ms[0]); t.image.colorspace_settings.name = 'Non-Color'
        sep = nt.nodes.new('ShaderNodeSeparateColor'); nt.links.new(t.outputs['Color'], sep.inputs['Color'])
        nt.links.new(sep.outputs['Red'], b.inputs['Metallic'])
        inv = nt.nodes.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1.0
        nt.links.new(t.outputs['Alpha'], inv.inputs[1]); nt.links.new(inv.outputs[0], b.inputs['Roughness'])
    m['escala_m'] = float(e.get('escala_m', 1.0))
    return m


kit.material = material_local          # terminar() lo usa por nombre

kit.reset()
bm = nuevo_bm()
from kit_manos import barrido
# Materiales: 0 pintura roja (el cuerpo), 1 metal gastado (las puntas, sin pintura), 2 goma (la cinta del mango).
R = 0.0127                              # hexágono de 22 mm entre caras
AGARRE = 0.11                           # la mano derecha, en el medio de la cinta
LARGO = 0.66                            # tramo recto
Rc, GIRO = 0.06, math.radians(150)      # cuello de ganso: radio y cuánto gira
# Un solo recorrido continuo: punta de cincel -> tramo recto (con la cinta) -> cuello -> uña. Cada punto lleva su radio, su
# aplanado (ancho en X, espesor en el plano del doblez) y su material: así queda una malla cerrada, sin piezas superpuestas.
pts, rad, sx, sy, mats = [], [], [], [], []
def punto(p, r, ax=1.0, ay=1.0, m=0):
    pts.append(tuple(p)); rad.append(r); sx.append(ax); sy.append(ay); mats.append(m)
for i in range(6, 0, -1):               # cincel: se aplana y se dobla unos 12 grados en los últimos 4,5 cm
    k = i / 6
    a = math.radians(12) * k
    z = -0.045 * k
    punto((0, z * math.sin(a), z * math.cos(a)), R * (1.0 - 0.12 * k), 1.0 + 0.45 * k, 1.0 - 0.65 * k, 1)
punto((0, 0, 0.0), R, m=0)
punto((0, 0, 0.034), R, m=0)
# La cinta (de 3,5 a 18,5 cm): un poco más gruesa, con cuatro vueltas marcadas.
punto((0, 0, 0.037), R * 1.13, m=2)
for z0 in (0.07, 0.11, 0.15):                # tres vueltas suaves (sin escalones: no rompen la malla)
    punto((0, 0, z0), R * 1.13, m=2)
    punto((0, 0, z0 + 0.008), R * 1.17, m=2)
    punto((0, 0, z0 + 0.016), R * 1.13, m=2)
punto((0, 0, 0.185), R * 1.13, m=2)
punto((0, 0, 0.187), R, m=0)
punto((0, 0, LARGO * 0.5), R, m=0)
punto((0, 0, LARGO), R, m=0)
for i in range(1, 13):                  # el cuello
    th = GIRO * i / 12
    punto((0, Rc - Rc * math.cos(th), LARGO + Rc * math.sin(th)), R, m=0)
fin = Vector(pts[-1])
t = Vector((0, math.sin(GIRO), math.cos(GIRO)))
for i in range(1, 6):                   # la uña: metal gastado, se ensancha y se aplana
    u = i / 5
    punto(fin + t * (0.04 * u), R * (1.0 - 0.2 * u), 1.0 + 0.55 * u, 1.0 - 0.62 * u, 1)
# Las caras de cada tramo toman el material del punto donde empieza el tramo.
barrido(bm, pts, rad, lados=6, mat=lambda i: mats[min(i + 1, len(mats) - 1)] if mats[i] != mats[min(i + 1, len(mats) - 1)] and mats[i] == 0 else mats[i],
        escala_x=sx, escala_y=sy, giro=math.radians(30), normal_inicial=(1, 0, 0))
# Las dos puntas de la V (se unen solas a la uña).
base = fin + t * 0.04
for lado in (-1, 1):
    punta = [tuple(base - t * 0.006 + Vector((lado * (0.006 + 0.0045 * u), 0, 0)) + t * (0.03 * u)) for u in (0, 0.33, 0.66, 1.0)]
    barrido(bm, punta, [R * 0.62, R * 0.55, R * 0.42, R * 0.18], lados=6, mat=1, escala_x=[0.75] * 4, escala_y=[0.42] * 4,
            normal_inicial=(1, 0, 0))
bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, -AGARRE))

ob = terminar('Barreta_LOD0', bm, ['pintura_roja', 'metal_gastado', 'goma'], bisel=0.0, angulo=40, uv_escala=2.0,
              apoyado=False, lods=False, bisel_min=0.008)
ob.name = 'Barreta'
vacio('Agarre_Derecha', (0, 0, 0), ob)
vacio('Punta', tuple(base + t * 0.03 - Vector((0, 0, AGARRE))), ob)
reporte([ob], categoria='A')
os.makedirs(salida, exist_ok=True); os.makedirs(fotos, exist_ok=True)
exportar(os.path.join(salida, 'Barreta.fbx'))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(salida, 'Barreta.blend'))

# Fotos con las texturas de la librería (Eevee).
sc = bpy.context.scene
w = bpy.data.worlds.new('Fondo'); sc.world = w; w.use_nodes = True
w.node_tree.nodes['Background'].inputs[0].default_value = (0.55, 0.6, 0.66, 1); w.node_tree.nodes['Background'].inputs[1].default_value = 0.9
sol = bpy.data.objects.new('Sol', bpy.data.lights.new('Sol', 'SUN')); sc.collection.objects.link(sol)
sol.data.energy = 3.2; sol.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.clip_start = 0.005
sc.render.engine = 'BLENDER_EEVEE'; sc.render.resolution_x = 1000; sc.render.resolution_y = 800
for nombre, objetivo, desde, lente in (('barreta_mr04', (0, 0.03, 0.22), (0.95, -0.65, 0.5), 45),
                                       ('barreta_mr04_una', (0, 0.07, 0.6), (0.2, -0.16, 0.62), 45),
                                       ('barreta_mr04_mango', (0, 0, 0.0), (0.16, -0.14, 0.05), 45)):
    cam.location = desde; cam.data.lens = lente
    cam.rotation_euler = (Vector(objetivo) - Vector(desde)).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = os.path.join(fotos, nombre + '.png'); bpy.ops.render.render(write_still=True)
print('BARRETA LISTA')
