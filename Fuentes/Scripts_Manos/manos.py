# Brazos y manos en primera persona (Claude, 2026-10-16; provisorios hasta el arte final del chat de arte).
# Uso: blender -b -P Fuentes/Scripts_Manos/manos.py -- <carpeta_salida_fbx> <carpeta_fotos>
# Segunda versión de las manos (Tomi, 2026-10-16: "sigue agarrando re chueca la barreta y la mano se ve re mal"): cada guante es
# UNA sola superficie blanda (metaballs: palma, dedos y pulgar se funden como en un guante de verdad), reducida a unos 3.500
# triángulos, con dos materiales cortados por planos (bordes prolijos) y el puño del guante aparte.
# Mano DERECHA (el brazo izquierdo usa la misma espejada en Unity). Piezas rígidas que el juego acomoda solo (sin esqueleto):
#   Brazo_*      : del hombro (origen) al codo, a lo largo de +Z, 0,29 m.
#   Antebrazo_*  : del codo (origen) a la muñeca, a lo largo de +Z, 0,26 m.
#   Mano_*       : la muñeca en el origen, el eje de la muñeca hacia +Z, el dorso hacia +Y y la palma hacia -Y; el pulgar del
#                  lado +X.
#                  "Puno": cerrada alrededor de un mango de 2,9 cm. Vacíos: "Agarre" en el centro del mango y "Agarre_Pulgar"
#                  3 cm más allá sobre el mango, del lado del pulgar. El mango cruza la palma en diagonal (15 grados, como en
#                  una mano de verdad): el juego lee la dirección de los dos vacíos.
#                  "Abierta": para agarrar cajas (vacío "Agarre" en el centro de la palma).
# Variantes (decisión de Tomi del 2026-10-16: los brazos según el traje):
#   con el traje: campera de trabajo acolchada; guante grueso negro y mate, con puño negro;
#   sin el traje: buzo de lana; guante de trabajo de tejido gris con la palma bañada en goma negra y puño tejido.
import sys, os, math, glob, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kit_manos import *

args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else ['.', '.']
salida, fotos = args[0], args[1]
os.makedirs(salida, exist_ok=True)
os.makedirs(fotos, exist_ok=True)
PROYECTO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
LIBRERIA = os.path.join(PROYECTO, 'Assets', '_Project', 'Art', 'Libreria')

LARGO_BRAZO, LARGO_ANTEBRAZO = 0.29, 0.26
MANGO_R = 0.0145   # radio del mango que agarra el puño (la cinta de la barreta, el mango del martillo)


# ------------------------------------------------------------------ Mangas (sin cambios de la primera versión)
def manga(nombre, largo, r0, r1, mat_tela, mat_puno, acolchada, puno_largo):
    """Una manga a lo largo de +Z: de r0 a r1, con el puño (la parte de abajo de la manga) al final."""
    bm = nuevo_bm()
    n = 24
    pts, rad, mats = [], [], []
    for i in range(n + 1):
        z = largo * i / n
        r = r0 + (r1 - r0) * (i / n)
        if acolchada:                                   # campera acolchada: bultos entre costuras
            r *= 1.0 + 0.06 * abs(math.sin(math.pi * z / 0.065))
        pts.append((0, 0, z))
        rad.append(r)
        mats.append(1 if z > largo - puno_largo else 0)
    # Los extremos redondeados (el codo y el hombro): sin el corte plano del tubo.
    r0c, r1c = rad[0], rad[-1]
    for avance, escala in ((0.35, 0.92), (0.7, 0.7), (0.92, 0.35)):
        pts.insert(0, (0, 0, -r0c * avance)); rad.insert(0, r0c * escala); mats.insert(0, mats[0])
        pts.append((0, 0, largo + r1c * avance * 0.6)); rad.append(r1c * escala); mats.append(mats[-1])
    n = len(pts) - 1
    barrido(bm, pts, rad, lados=16, mat=lambda i: mats[i], escala_x=[1.0] * (n + 1), escala_y=[0.9] * (n + 1),
            normal_inicial=(1, 0, 0))
    return terminar(nombre, bm, [mat_tela, mat_puno], angulo=50, uv_tam=0.35)


# ------------------------------------------------------------------ Guantes (metaballs)
ESC = 20.0                       # se modela 20 veces más grande: la resolución mínima de las metaballs es gruesa
UMBRAL = 0.6


def k_vis(rigidez):
    """Radio que se ve / radio de la metaball (con rigidez 2 da 0,575: medido en Blender)."""
    return math.sqrt(1.0 - (UMBRAL / rigidez) ** (1.0 / 3.0))


TILT = math.radians(15)          # el mango cruza la palma en diagonal: el lado del pulgar más cerca de los dedos
T = Vector((math.cos(TILT), 0, math.sin(TILT)))      # a lo largo del mango, hacia el pulgar
B = Vector((0, 1, 0))                                # hacia el dorso
U = Vector((-math.sin(TILT), 0, math.cos(TILT)))     # en el plano de los dedos, hacia las puntas


def P(x, y, z):
    """Un punto en el marco del mango: x a lo largo del mango, y hacia el dorso, z hacia los dedos."""
    return T * x + B * y + U * z


class Blando:
    """Una superficie blanda hecha de bolas y cápsulas que se funden (metaballs)."""

    def __init__(self, nombre, res):
        self.mb = bpy.data.metaballs.new('MB' + nombre)
        self.mb.resolution = res * ESC
        self.mb.render_resolution = res * ESC
        self.mb.threshold = UMBRAL
        self.ob = bpy.data.objects.new('MB' + nombre, self.mb)
        bpy.context.scene.collection.objects.link(self.ob)

    def bola(self, c, r, rig=2.0):
        e = self.mb.elements.new(type='BALL')
        e.co = Vector(c) * ESC
        e.radius = r / k_vis(rig) * ESC
        e.stiffness = rig

    def hueso(self, a, b, r, rig=2.0):
        a, b = Vector(a), Vector(b)
        d = b - a
        e = self.mb.elements.new(type='CAPSULE')
        e.co = (a + b) / 2 * ESC
        e.size_x = d.length / 2 * ESC
        e.rotation = Vector((1, 0, 0)).rotation_difference(d.normalized())
        e.radius = r / k_vis(rig) * ESC
        e.stiffness = rig

    def elipsoide(self, c, tam, eje_x, eje_y):
        e = self.mb.elements.new(type='ELLIPSOID')
        e.co = Vector(c) * ESC
        ex = Vector(eje_x).normalized(); ey = Vector(eje_y).normalized(); ez = ex.cross(ey)
        e.rotation = Matrix((ex, ey, ez)).transposed().to_quaternion()
        r = max(tam)
        e.radius = r / k_vis(2.0) * ESC
        e.size_x, e.size_y, e.size_z = tam[0] / r, tam[1] / r, tam[2] / r
        e.stiffness = 2.0

    def malla(self, nombre):
        dg = bpy.context.evaluated_depsgraph_get()
        me = bpy.data.meshes.new_from_object(self.ob.evaluated_get(dg))
        me.name = nombre
        me.transform(Matrix.Scale(1.0 / ESC, 4))
        bpy.data.objects.remove(self.ob)
        ob = bpy.data.objects.new(nombre, me)
        bpy.context.scene.collection.objects.link(ob)
        return ob


def camino_dedo(mcp_y, mcp_z, centro_y, centro_z, rc, largo, n=14):
    """El eje de un dedo cerrado (en el plano y-z del mango): del nudillo, tangente al mango, y después alrededor."""
    m = Vector((mcp_y, mcp_z)); c = Vector((centro_y, centro_z))
    d = (m - c).length
    ang_m = math.atan2(m.y - c.y, m.x - c.x)
    ang_t = ang_m + math.acos(min(1.0, rc / d))      # el dedo pasa por arriba del mango y baja por delante
    tang = c + Vector((math.cos(ang_t), math.sin(ang_t))) * rc
    pts = [m, tang]
    giro = (largo - (tang - m).length) / rc
    for k in range(1, n + 1):
        a = ang_t + giro * k / n
        pts.append(c + Vector((math.cos(a), math.sin(a))) * rc)
    return pts


def palma_y_muneca(g, gl, inclinada):
    """La palma (filas de cápsulas a lo largo del mango, de la muñeca a los nudillos), la eminencia del pulgar y la muñeca."""
    for z, ancho, esp in ((0.012, 0.030, 0.0165), (0.035, 0.036, 0.0172), (0.058, 0.040, 0.0172), (0.076, 0.041, 0.0158)):
        x0 = z * math.tan(TILT) if inclinada else 0.0
        q = (lambda x, y, zz: P(x, y, zz)) if inclinada else (lambda x, y, zz: Vector((x, y, zz)))
        g.hueso(q(x0 - ancho * 0.62, 0.0, z), q(x0 + ancho * 0.62, 0.0, z), esp + gl)
    eje_t = T if inclinada else Vector((1, 0, 0))
    c_tenar = P(0.026, -0.010, 0.030) if inclinada else Vector((0.024, -0.009, 0.030))
    g.elipsoide(c_tenar, (0.020 + gl, 0.012 + gl, 0.016 + gl), eje_t, B)
    g.elipsoide((0, 0, -0.006), (0.030 + gl, 0.019 + gl, 0.024), (1, 0, 0), (0, 1, 0))


DEDOS = ((+0.0318, 0.0094, 0.098), (+0.0107, 0.0099, 0.106), (-0.0104, 0.0094, 0.099), (-0.0296, 0.0083, 0.082))  # x, radio, largo


RIG_DEDO = 5.0      # los dedos se funden menos entre sí (quedan los pliegues entre dedo y dedo)
UV_TAM = 0.15       # una vuelta de textura cada 15 cm: la trama del tejido y el grano del cuero a la escala de la mano


def puno(g, gl):
    """Cerrada alrededor de un mango de radio MANGO_R. Devuelve el centro del mango y un punto hacia el pulgar."""
    z_mcp = 0.088 + gl
    yh, zh = -0.031 - gl, z_mcp - 0.010              # el mango: delante de la palma, apenas abajo de los nudillos
    xc = z_mcp * math.tan(TILT)                      # la línea del medio de la mano a la altura de los nudillos
    palma_y_muneca(g, gl, True)
    for x, r, largo in DEDOS:                         # nudillos: los bultos del dorso
        g.bola(P(xc + x, 0.006, z_mcp - 0.003), r + gl + 0.0016, 3.0)
    for x, r, largo in DEDOS:
        rc = MANGO_R + r + gl + 0.0006
        pts = [P(xc + x, p.x, p.y) for p in camino_dedo(0.0, z_mcp, yh, zh, rc, largo + gl * 2)]
        n = len(pts)
        for k in range(n - 1):
            g.hueso(pts[k], pts[k + 1], (r + gl) * (1.0 - 0.16 * k / (n - 2)), RIG_DEDO)
        for frac in (0.42, 0.70):                     # las articulaciones de los dedos, apenas marcadas
            g.bola(pts[int(frac * (n - 1))], (r + gl) * 1.06, RIG_DEDO)
    # Pulgar: sale de la base de la palma, rodea el mango por delante y apoya sobre el índice y el mayor.
    x_ind = xc + DEDOS[0][0]
    yf = yh - (MANGO_R + 0.0100 + gl)
    pul = [P(0.030, -0.004, 0.012), P(0.046, -0.020 - gl, 0.036), P(x_ind + 0.016, yf - 0.011 - gl, zh + 0.006),
           P(x_ind - 0.004, yf - 0.014 - gl, zh - 0.004), P(x_ind - 0.019, yf - 0.012 - gl, zh - 0.010)]
    rp = (0.0130, 0.0114, 0.0106, 0.0099, 0.0090)
    for k in range(len(pul) - 1):
        g.hueso(pul[k], pul[k + 1], rp[k] + gl)
    g.bola(pul[2], rp[2] + gl + 0.0008)               # la articulación del pulgar
    agarre = P(xc, yh, zh)
    return agarre, agarre + T * 0.03, z_mcp


def abierta(g, gl):
    """Abierta, con los dedos juntos y apenas curvos (apoyada contra el costado de una caja)."""
    z_mcp = 0.088 + gl
    palma_y_muneca(g, gl, False)
    for x, r, largo in DEDOS:
        g.bola((x, 0.006, z_mcp - 0.003), r + gl + 0.0016, 3.0)
        pts = []
        for k in range(9):
            u = k / 8
            a = math.radians(30) * u                  # se curvan de a poco hacia la palma
            pts.append(Vector((x * (1 + 0.16 * u), -largo * 0.5 * (1 - math.cos(a)) * u, z_mcp + largo * 0.92 * u)))
        for k in range(len(pts) - 1):
            g.hueso(pts[k], pts[k + 1], (r + gl) * (1.0 - 0.16 * k / 7), RIG_DEDO)
        g.bola(pts[4], (r + gl) * 1.05, RIG_DEDO)
    pul = [(0.030, -0.004, 0.012), (0.050, -0.017, 0.034), (0.064, -0.025, 0.056), (0.072, -0.029, 0.077)]
    rp = (0.0130, 0.0114, 0.0104, 0.0094)
    for k in range(len(pul) - 1):
        g.hueso(pul[k], pul[k + 1], rp[k] + gl)
    return Vector((0.0, -0.0172 - gl, 0.056)), z_mcp


def reducir(ob, tris):
    """Deja la malla en unos 'tris' triángulos (reducción que respeta la forma) y la suaviza."""
    actual = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    mod = ob.modifiers.new('Reducir', 'DECIMATE')
    mod.decimate_type = 'COLLAPSE'
    mod.ratio = min(1.0, tris / max(actual, 1))
    bpy.ops.object.modifier_apply(modifier=mod.name)
    ob.select_set(False)


def cortar_materiales(ob, z_nudillos, inclinada, mat_palma, mat_resto):
    """Dos materiales cortados por planos (bordes rectos y prolijos aunque la malla esté reducida): la palma (lo que toca,
    de los nudillos para atrás y del lado de la palma) en mat_palma; el resto (el dorso y los dedos, lo que se ve desde la
    cámara) en mat_resto."""
    bm = bmesh.new(); bm.from_mesh(ob.data)
    eje_u = U if inclinada else Vector((0, 0, 1))
    planos = ((Vector((0, 0, 0)) + eje_u * (z_nudillos - 0.010), eje_u),   # detrás de los nudillos
              (Vector((0, -0.004, 0)), Vector((0, 1, 0))))                  # el costado de la mano (mitad del espesor)
    for co, no in planos:
        geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
        bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no, dist=1e-6)
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
    for f in bm.faces:
        c = f.calc_center_median()
        palma = (c - planos[0][0]).dot(planos[0][1]) <= 0 and c.y < -0.004
        f.material_index = 0 if palma else 1
        f.smooth = True
    bm.to_mesh(ob.data); bm.free()
    for m in (mat_palma, mat_resto):
        ob.data.materials.append(bpy.data.materials.get(m) or bpy.data.materials.new(m))


def puno_del_guante(ob, largo, r_x, r_y, acampanado, mat):
    """El puño del guante (aparte, sobre la muñeca): un tubo ovalado que se abre un poco hacia el antebrazo."""
    bm = bmesh.new(); bm.from_mesh(ob.data)
    n = 10
    pts, rad, sx, sy = [], [], [], []
    z0, z1 = -largo, 0.014
    for i in range(n + 1):
        u = i / n
        z = z0 + (z1 - z0) * u
        pts.append((0, 0, z))
        abre = 1.0 + acampanado * (1 - u) ** 2
        rad.append(1.0); sx.append(r_x * abre); sy.append(r_y * abre)
    # Borde de abajo redondeado hacia adentro (el canto del puño), y arriba se cierra contra la muñeca.
    pts.insert(0, (0, 0, z0 - 0.003)); rad.insert(0, 1.0); sx.insert(0, sx[0] * 0.9); sy.insert(0, sy[0] * 0.9)
    pts.append((0, 0, z1 + 0.006)); rad.append(1.0); sx.append(r_x * 0.86); sy.append(r_y * 0.82)
    nombres = [x.name for x in ob.data.materials]
    indice = nombres.index(mat) if mat in nombres else len(nombres)
    barrido(bm, pts, rad, lados=20, mat=indice, escala_x=sx, escala_y=sy, normal_inicial=(1, 0, 0))
    for f in bm.faces:
        f.smooth = True
    bm.to_mesh(ob.data); bm.free()
    if indice == len(nombres):
        ob.data.materials.append(bpy.data.materials.get(mat) or bpy.data.materials.new(mat))


def guante(nombre, cerrada, grueso):
    gl = 0.0055 if grueso else 0.0024                 # cuánto abulta el guante sobre la mano
    g = Blando(nombre, 0.0016)
    if cerrada:
        agarre, pulgar, z_mcp = puno(g, gl)
    else:
        (agarre, z_mcp), pulgar = abierta(g, gl), None
    ob = g.malla(nombre)
    reducir(ob, 2800 if cerrada else 2600)
    if grueso:   # guante grueso: negro y mate, todo de una pieza (el cuero marrón de la palma se leía como piel), puño negro
        ob.data.materials.append(bpy.data.materials.get('MT_Goma') or bpy.data.materials.new('MT_Goma'))
        for p in ob.data.polygons:
            p.use_smooth = True
        puno_del_guante(ob, 0.055, 0.039, 0.031, 0.10, 'MT_Goma')
    else:        # guante de trabajo: tejido gris, la palma bañada en goma negra, puño tejido
        cortar_materiales(ob, z_mcp, cerrada, 'MT_Goma', 'MT_TelaGris')
        puno_del_guante(ob, 0.035, 0.033, 0.026, 0.05, 'MT_TelaGris')
    me = ob.data
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.cube_project(cube_size=UV_TAM)
    bpy.ops.object.mode_set(mode='OBJECT'); ob.select_set(False)
    vacio('Agarre', tuple(agarre), ob)
    if pulgar is not None:
        vacio('Agarre_Pulgar', tuple(pulgar), ob)
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    print(f'{nombre}: {tris} triángulos')
    return ob


# ------------------------------------------------------------------ Materiales de la librería (para las fotos)
CATALOGO = {e.get("material"): e for e in json.load(open(os.path.join(LIBRERIA, 'catalogo.json'), encoding='utf-8'))}


def texturas_de_libreria():
    """Conecta las texturas de la librería a los materiales (solo para las fotos; en Unity los conecta ArtImport)."""
    for m in bpy.data.materials:
        if m.name not in CATALOGO:
            continue
        m.use_nodes = True
        nt = m.node_tree; b = nt.nodes['Principled BSDF']
        for t in [n for n in nt.nodes if n.type in ('TEX_IMAGE', 'NORMAL_MAP', 'SEPARATE_COLOR', 'MATH', 'MAPPING', 'TEX_COORD')]:
            nt.nodes.remove(t)
        esc = 1.0                                   # igual que en Unity: una textura cada UV_TAM metros
        coord = nt.nodes.new('ShaderNodeTexCoord'); mp = nt.nodes.new('ShaderNodeMapping')
        mp.inputs['Scale'].default_value = (esc, esc, esc); nt.links.new(coord.outputs['UV'], mp.inputs['Vector'])
        def tex(suf, color=True):
            f = glob.glob(os.path.join(LIBRERIA, '**', m.name + '_' + suf + '.*'), recursive=True)
            f = [x for x in f if not x.endswith('.meta')]
            if not f:
                return None
            t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(f[0])
            if not color:
                t.image.colorspace_settings.name = 'Non-Color'
            nt.links.new(mp.outputs['Vector'], t.inputs['Vector'])
            return t
        t = tex('BaseColor')
        if t: nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
        t = tex('Normal', False)
        if t:
            n = nt.nodes.new('ShaderNodeNormalMap'); nt.links.new(t.outputs['Color'], n.inputs['Color'])
            n.inputs['Strength'].default_value = 0.8
            nt.links.new(n.outputs['Normal'], b.inputs['Normal'])
        t = tex('MetalSmooth', False)
        if t:
            sep = nt.nodes.new('ShaderNodeSeparateColor'); nt.links.new(t.outputs['Color'], sep.inputs['Color'])
            nt.links.new(sep.outputs['Red'], b.inputs['Metallic'])
            inv = nt.nodes.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1.0
            nt.links.new(t.outputs['Alpha'], inv.inputs[1]); nt.links.new(inv.outputs[0], b.inputs['Roughness'])


def escena_de_fotos():
    sc = bpy.context.scene
    w = bpy.data.worlds.new('Fondo'); sc.world = w; w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (0.55, 0.6, 0.66, 1)
    w.node_tree.nodes['Background'].inputs[1].default_value = 0.9
    sol = bpy.data.objects.new('Sol', bpy.data.lights.new('Sol', 'SUN')); sc.collection.objects.link(sol)
    sol.data.energy = 3.2; sol.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
    cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.clip_start = 0.005; cam.data.lens = 50
    sc.render.engine = 'BLENDER_EEVEE'; sc.render.resolution_x = 800; sc.render.resolution_y = 640
    return cam


def foto_tex(cam, ruta, objetivo, desde, visibles):
    for o in bpy.data.objects:
        if o.type == 'MESH':
            o.hide_render = o.name not in visibles
    cam.location = desde
    cam.rotation_euler = (Vector(objetivo) - Vector(desde)).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.render.filepath = ruta
    bpy.ops.render.render(write_still=True)


def armar():
    reset()
    piezas = []
    # Con el traje: campera de trabajo acolchada y guante grueso.
    piezas.append(manga('Brazo_Campera', LARGO_BRAZO, 0.072, 0.066, 'MT_LonaMarron', 'MT_LonaMarron', True, 0.0))
    piezas.append(manga('Antebrazo_Campera', LARGO_ANTEBRAZO, 0.066, 0.056, 'MT_LonaMarron', 'MT_TelaGris', True, 0.035))
    piezas.append(guante('Mano_Grueso_Puno', True, True))
    piezas.append(guante('Mano_Grueso_Abierta', False, True))
    # Sin el traje: buzo de lana y guante de trabajo.
    piezas.append(manga('Brazo_Buzo', LARGO_BRAZO, 0.052, 0.047, 'MT_TelaManta', 'MT_TelaManta', False, 0.0))
    piezas.append(manga('Antebrazo_Buzo', LARGO_ANTEBRAZO, 0.047, 0.037, 'MT_TelaManta', 'MT_TelaGris', False, 0.045))
    piezas.append(guante('Mano_Trabajo_Puno', True, False))
    piezas.append(guante('Mano_Trabajo_Abierta', False, False))
    for p in piezas:
        p.location = (0, 0, 0)
    exportar(os.path.join(salida, 'Manos.fbx'))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(fotos, 'Manos.blend'))   # fuera de Assets (Unity no lo importa)

    # Fotos de revisión con las texturas de la librería: cada puño agarrando un mango, y las manos abiertas.
    texturas_de_libreria()
    palo_me = bpy.data.meshes.new('Palo'); bmp = nuevo_bm()
    barrido(bmp, [(0, 0, -0.16), (0, 0, 0.16)], MANGO_R, lados=6, mat=0)
    bmp.to_mesh(palo_me); bmp.free()
    palo = bpy.data.objects.new('Palo', palo_me); bpy.context.scene.collection.objects.link(palo)
    rojo = bpy.data.materials.get('MT_PinturaRoja') or bpy.data.materials.new('MT_PinturaRoja')
    rojo.use_nodes = True; rojo.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.42, 0.04, 0.03, 1)
    palo_me.materials.append(rojo)
    cam = escena_de_fotos()
    for nombre in ('Mano_Grueso_Puno', 'Mano_Trabajo_Puno'):
        m = bpy.data.objects[nombre]
        ag = Vector(next(c for c in m.children if c.name.startswith('Agarre') and 'Pulgar' not in c.name).location)
        pu = Vector(next(c for c in m.children if c.name.startswith('Agarre_Pulgar')).location)
        palo.location = ag
        palo.rotation_mode = 'QUATERNION'; palo.rotation_quaternion = Vector((0, 0, 1)).rotation_difference((pu - ag).normalized())
        corto = nombre.split('_')[1].lower()
        for vista, desde in (('frente', (0.06, -0.30, 0.14)), ('dorso', (0.14, 0.28, 0.16)), ('pulgar', (0.28, -0.18, 0.20))):
            foto_tex(cam, os.path.join(fotos, f'guante_{corto}_{vista}.png'), ag, tuple(Vector(desde)), (nombre, 'Palo'))
    for nombre in ('Mano_Grueso_Abierta', 'Mano_Trabajo_Abierta'):
        corto = nombre.split('_')[1].lower()
        foto_tex(cam, os.path.join(fotos, f'guante_{corto}_abierta.png'), (0, -0.01, 0.07), (0.16, -0.26, 0.12), (nombre,))


armar()
print('MANOS LISTAS')
