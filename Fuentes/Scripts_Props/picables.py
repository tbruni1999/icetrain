# Lo que se pica en el mundo (G1, Claude, 2026-10-17; provisorios hasta el arte final, en el estilo MR-04 del kit de props):
# rocas (chica lisa, grande lisa, grande con veta de hierro, grande con veta de carbón), árbol muerto, tronco caído y tocón.
# Categoría D (exterior a la intemperie): la nieve de arriba y el montículo del pie van como objetos aparte (se apagan en Unity).
# Uso: blender -b -P Fuentes/Scripts_Props/picables.py -- <ruta_del_kit_del_skill> <carpeta_salida> <carpeta_fotos> [solo...]
# Cada pieza sale en <carpeta_salida>/<Nombre>/<Nombre>.fbx con su LOD0/LOD1/LOD2 y el pivote en la base.
import sys, os, math, json, glob, random
args = sys.argv[sys.argv.index('--') + 1:]
KIT, salida, fotos = args[0], args[1], args[2]
solo = set(args[3:])
sys.path.insert(0, KIT)
AQUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(AQUI, '..', 'Scripts_Manos'))
import bpy, bmesh
from mathutils import Vector, noise
import kit
from kit import nuevo_bm, cilindro, terminar, reporte, nieve_superior, monticulo, lod_decimado, pivote, agrupar, exportar, preview
from kit_manos import barrido

PROYECTO = os.path.abspath(os.path.join(AQUI, '..', '..'))
LIBRERIA = os.path.join(PROYECTO, 'Assets', '_Project', 'Art', 'Libreria')
CATALOGO = {e['nombre']: e for e in json.load(open(os.path.join(LIBRERIA, 'catalogo.json'), encoding='utf-8'))}
os.makedirs(fotos, exist_ok=True)


def material_local(nombre, receta=None, color=None, size=1024):
    """Como kit.material, pero con las texturas de la librería del proyecto (la misma idea que barreta_mr04.py)."""
    e = CATALOGO[nombre]
    nm = e['material']
    if nm in bpy.data.materials:
        return bpy.data.materials[nm]
    m = bpy.data.materials.new(nm); m.use_nodes = True; nt = m.node_tree; b = nt.nodes['Principled BSDF']
    def buscar(suf):
        return [x for x in glob.glob(os.path.join(LIBRERIA, '**', nm + '_' + suf + '.*'), recursive=True) if not x.endswith('.meta')]
    base, norm, ms = buscar('BaseColor'), buscar('Normal'), buscar('MetalSmooth')
    if base:
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(base[0]); nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
    if norm:
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(norm[0]); t.image.colorspace_settings.name = 'Non-Color'
        n = nt.nodes.new('ShaderNodeNormalMap'); nt.links.new(t.outputs['Color'], n.inputs['Color']); nt.links.new(n.outputs['Normal'], b.inputs['Normal'])
    if ms:
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(ms[0]); t.image.colorspace_settings.name = 'Non-Color'
        sep = nt.nodes.new('ShaderNodeSeparateColor'); nt.links.new(t.outputs['Color'], sep.inputs['Color'])
        nt.links.new(sep.outputs['Red'], b.inputs['Metallic'])
        inv = nt.nodes.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1.0
        nt.links.new(t.outputs['Alpha'], inv.inputs[1]); nt.links.new(inv.outputs[0], b.inputs['Roughness'])
    m['escala_m'] = float(e.get('escala_m', 1.0))
    return m


kit.material = material_local


def exportar_liviano(objs, nombre, carpeta):
    """FBX sin las texturas adentro (en Unity las pone ArtImport desde la librería, como en los demás props) y el .blend sin
    empaquetar (apunta a las texturas de la librería): unos cientos de KB en vez de decenas de MB por pieza."""
    os.makedirs(carpeta, exist_ok=True)
    objs = kit.con_lods([o for o in objs if o])
    sel = set(objs)
    for o in objs:
        p = o.parent
        while p:
            sel.add(p); p = p.parent
    for o in list(bpy.data.objects):
        if o not in sel:
            bpy.data.objects.remove(o, do_unlink=True)
    bpy.ops.object.select_all(action='DESELECT')
    for o in sel:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(carpeta, nombre + '.fbx'), use_selection=True, object_types={'EMPTY', 'MESH'},
                             apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                             use_mesh_modifiers=True, mesh_smooth_type='FACE', path_mode='STRIP', embed_textures=False, add_leaf_bones=False)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(carpeta, nombre + '.blend'))
    for f in os.listdir(carpeta):
        if f.endswith('.blend1'):
            os.remove(os.path.join(carpeta, f))


def cerrar(nombre, objs, categoria='D'):
    """Agrupa, revisa, saca la foto y exporta una pieza."""
    objs = [o for o in objs if o]
    agrupar(nombre, objs)
    reporte(objs, categoria=categoria)
    preview(objs, os.path.join(fotos, nombre + ".png"), vistas=("34",))
    exportar_liviano(objs, nombre, os.path.join(salida, nombre))


def nieve_y_monticulo(o, espesor=0.06, altura=0.14, alcance=0.32, arriba=True):
    nieve = nieve_superior([o], espesor=espesor) if arriba else None
    if nieve:
        lod_decimado(nieve)
    mont = monticulo([o], viento=(1, 0.35), altura=altura, alcance=alcance)
    if mont:
        lod_decimado(mont)
    return nieve, mont


# ------------------------------------------------------------------ Rocas
def roca(nombre, tam, semilla, veta=None, ancho=0.08, sub=3):
    """Una roca: esfera deformada con ruido (dos escalas), facetada, la base plana. Las vetas son franjas finas de otro material
    (donde el ruido cruza por cero: líneas que serpentean por la piedra). Sin nieve arriba: el viento la barre de las rocas; queda
    el montículo del pie."""
    kit.reset()
    bm = nuevo_bm()
    bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=1.0)
    rnd = random.Random(semilla)
    off = Vector((rnd.uniform(0, 50), rnd.uniform(0, 50), rnd.uniform(0, 50)))
    for v in bm.verts:
        p = v.co.copy()
        d = 1.0 + 0.3 * noise.noise(p * 1.2 + off) + 0.1 * noise.noise(p * 3.4 + off * 1.3)
        v.co = Vector((p.x * tam[0] * 0.5 * d, p.y * tam[1] * 0.5 * d, p.z * tam[2] * 0.5 * d))
    zmin = min(v.co.z for v in bm.verts)
    corte = tam[2] * 0.14                                   # lo que queda enterrado: la base, plana contra el piso
    for v in bm.verts:
        v.co.z = max(0.0, v.co.z - zmin - corte)
    if veta:
        off2 = off * 1.7 + Vector((13.0, 7.0, 3.0))
        esc = 2.6 / max(tam)
        for f in bm.faces:
            c = f.calc_center_median()
            if c.z > 0.02 and abs(noise.noise(c * esc + off2)) < ancho:
                f.material_index = 1
    # La piedra oscura en las rocas grandes; la gris en las chicas y donde hay carbón (la veta negra no se vería sobre la oscura).
    oscura = 'Grande' in nombre and veta != 'plastico_negro'
    mats = ['roca_oscura' if oscura else 'roca_gris'] + ([veta] if veta else [])
    o = terminar(nombre + '_LOD0', bm, mats, bisel=0.0, angulo=32, uv_escala=1.0, apoyado=True)
    pivote(o, 'base')
    nieve, mont = nieve_y_monticulo(o, altura=0.1 if tam[2] < 0.8 else 0.16, arriba=False)
    cerrar(nombre, [o, nieve, mont])


# ------------------------------------------------------------------ Árbol muerto, tronco caído, tocón
def arbol():
    kit.reset()
    bm = nuevo_bm()
    alturas = [0.0, 0.35, 0.9, 1.6, 2.4, 3.2, 3.9, 4.5]
    tronco = [(0.05 * math.sin(z * 0.9), 0.04 * math.sin(z * 1.4 + 1.0), z) for z in alturas]
    barrido(bm, tronco, [0.2, 0.17, 0.15, 0.13, 0.11, 0.08, 0.055, 0.03], lados=10, mat=0, normal_inicial=(1, 0, 0))
    # Ramas secas: salen del tronco hacia arriba y afuera, cada vez más finas (sin hojas: está muerto).
    rnd = random.Random(5)
    for k, (z, ang, largo) in enumerate([(1.7, 20, 1.1), (2.3, 140, 1.0), (2.9, 250, 0.9), (3.4, 330, 0.7), (3.8, 80, 0.55)]):
        a = math.radians(ang)
        base = Vector((0.05 * math.sin(z * 0.9), 0.04 * math.sin(z * 1.4 + 1.0), z))
        dirr = Vector((math.cos(a), math.sin(a), 0.0))
        pts = [base + dirr * (largo * t) + Vector((0, 0, largo * 0.55 * t + 0.08 * math.sin(t * 3 + k))) for t in (0.0, 0.35, 0.7, 1.0)]
        barrido(bm, [tuple(p) for p in pts], [0.055, 0.04, 0.025, 0.01], lados=6, mat=0, normal_inicial=(0, 0, 1))
        # una ramita que sale de cada rama
        p0 = pts[2]
        d2 = (dirr + Vector((rnd.uniform(-0.6, 0.6), rnd.uniform(-0.6, 0.6), 0.6))).normalized()
        barrido(bm, [tuple(p0), tuple(p0 + d2 * 0.25), tuple(p0 + d2 * 0.45)], [0.02, 0.012, 0.005], lados=5, mat=0, normal_inicial=(0, 0, 1))
    o = terminar('Arbol_Muerto_LOD0', bm, ['corteza'], bisel=0.0, angulo=50, uv_escala=1.0, apoyado=True)
    pivote(o, 'base')
    nieve, mont = nieve_y_monticulo(o, espesor=0.04, altura=0.2, alcance=0.4)
    cerrar('Arbol_Muerto', [o, nieve, mont])


def tronco_caido():
    kit.reset()
    bm = nuevo_bm()
    largo = 2.8
    pts = [(-largo / 2 + largo * i / 6, 0.03 * math.sin(i * 1.3), 0.21) for i in range(7)]
    barrido(bm, pts, [0.21, 0.215, 0.205, 0.2, 0.195, 0.19, 0.185], lados=12, mat=0, normal_inicial=(0, 0, 1))
    for x, r in ((-largo / 2, 0.205), (largo / 2, 0.18)):    # las puntas cortadas: madera clara
        s = -1 if x < 0 else 1
        cilindro(bm, (x - s * 0.01, 0, 0.21), (x + s * 0.006, 0, 0.21), r, seg=12, mat=1)
    o = terminar('Tronco_Caido_LOD0', bm, ['corteza', 'madera_intemperie'], bisel=0.0, angulo=40, uv_escala=1.0, apoyado=False)
    pivote(o, 'base')
    nieve, mont = nieve_y_monticulo(o, espesor=0.06, altura=0.12, alcance=0.3)
    cerrar('Tronco_Caido', [o, nieve, mont])


def tocon():
    kit.reset()
    bm = nuevo_bm()
    barrido(bm, [(0, 0, 0), (0, 0, 0.25), (0, 0, 0.44)], [0.3, 0.26, 0.24], lados=12, mat=0, normal_inicial=(1, 0, 0))
    cilindro(bm, (0, 0, 0.435), (0, 0, 0.45), 0.235, seg=12, mat=1)  # el corte de arriba
    for ang in (30, 150, 270):                                   # raíces que se meten en la nieve
        a = math.radians(ang)
        d = Vector((math.cos(a), math.sin(a), 0))
        pts = [d * 0.2 + Vector((0, 0, 0.12)), d * 0.45 + Vector((0, 0, 0.04)), d * 0.62 + Vector((0, 0, -0.03))]
        barrido(bm, [tuple(p) for p in pts], [0.08, 0.05, 0.025], lados=6, mat=0, normal_inicial=(0, 0, 1))
    o = terminar('Tocon_LOD0', bm, ['corteza', 'madera_intemperie'], bisel=0.0, angulo=45, uv_escala=1.0, apoyado=True)
    pivote(o, 'base')
    nieve, mont = nieve_y_monticulo(o, espesor=0.05, altura=0.12, alcance=0.3)
    cerrar('Tocon', [o, nieve, mont])


PIEZAS = {
    'Roca_Chica': lambda: roca('Roca_Chica', (0.75, 0.6, 0.45), 11, sub=2),
    'Roca_Grande': lambda: roca('Roca_Grande', (1.8, 1.3, 1.1), 23),
    'Roca_Grande_Hierro': lambda: roca('Roca_Grande_Hierro', (1.7, 1.4, 1.05), 37, veta="acero_oxidado", ancho=0.09, sub=4),
    'Roca_Grande_Carbon': lambda: roca('Roca_Grande_Carbon', (1.9, 1.2, 1.0), 41, veta="plastico_negro", ancho=0.1, sub=4),
    'Arbol_Muerto': arbol,
    'Tronco_Caido': tronco_caido,
    'Tocon': tocon,
}
for nombre, hacer in PIEZAS.items():
    if solo and nombre not in solo:
        continue
    hacer()
    print('LISTO', nombre)
print('PICABLES LISTOS')
