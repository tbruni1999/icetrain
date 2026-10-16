# Brazos en primera persona con el rig que pasó Tomi (2026-10-16): "fps arms (rigged only)" de OpenGameArt (CC0, hecho con
# MakeHuman; original en Fuentes/Brazos_OGA). Tomi: "agregale la ropa, pero necesito que la ropa se vea impecable".
# Uso: blender -b -Y -P Fuentes/Scripts_Manos/brazos_rig.py -- <rig .blend> <carpeta del FBX> <carpeta de fotos>
#
# Qué hace:
# - Deja el rig limpio y en metros: sin restricciones de Blender (en el juego los huesos los mueve el código), la mano colgada
#   del antebrazo (brazo -> antebrazo -> mano) y sin los huesos de control del IK.
# - Viste cada brazo según el traje (decisión de Tomi), con ropa hecha a partir de la propia malla del brazo: así se dobla
#   con los mismos huesos y la piel nunca asoma (la piel no se exporta: siempre está tapada).
#     con el traje:  manga de campera acolchada (MT_LonaMarron) y guante grueso negro (MT_Goma) con puño largo;
#     sin el traje:  manga de buzo de lana (MT_TelaManta) con puño elástico y guante de trabajo de tejido gris
#                    (MT_TelaGris) con los dedos y la palma bañados en goma negra (MT_Goma).
# - Huesos extra en cada mano (no deforman): "agarre" (el centro del mango en el puño), "agarre_pulgar" (3 cm más allá
#   sobre el mango, del lado del pulgar) y "palma" (el centro de la palma, para apoyarla en una caja).
# - Dos poses de los dedos como acciones: "Puno" (cerrada alrededor de un mango de 2,9 cm: cada falange se dobla hasta
#   tocarlo) y "Abierta" (dedos juntos y casi rectos).
import sys, os, math
import bpy, bmesh
from mathutils import Vector, Matrix, Quaternion
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import libreria

args = sys.argv[sys.argv.index('--') + 1:]
FUENTE, SALIDA, FOTOS = (os.path.abspath(a) for a in args[:3])
os.makedirs(SALIDA, exist_ok=True); os.makedirs(FOTOS, exist_ok=True)

ESCALA = 0.1          # el modelo viene en decímetros
UV_TAM = 0.25         # metros por vuelta de textura (en Unity el material repite cada 1 de UV)
LADOS = ('R', 'L')


# ------------------------------------------------------------------ Preparar el rig
def modo(o, cual):
    """Entra o sale del modo edición de un objeto (sin ventana: hay que dejarlo activo y elegido)."""
    for x in bpy.context.view_layer.objects:
        x.select_set(x == o)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.mode_set(mode=cual)
    assert o.mode == cual, (o.name, o.mode)


def preparar():
    # Se trae del archivo solo el rig y la malla (escena vacía de fábrica: los operadores de Blender funcionan sin ventana).
    bpy.ops.wm.read_factory_settings(use_empty=True)
    with bpy.data.libraries.load(FUENTE, link=False) as (origen, destino):
        destino.objects = list(origen.objects)
    for o in destino.objects:
        if o is not None and o.type in ('ARMATURE', 'MESH'):
            bpy.context.scene.collection.objects.link(o)
    bpy.context.view_layer.update()              # que los objetos tengan su lugar en el mundo antes de medir
    for o in list(bpy.data.objects):
        if o.type not in ('ARMATURE', 'MESH'):
            bpy.data.objects.remove(o)
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    piel = next(o for o in bpy.data.objects if o.type == 'MESH')
    for pb in arm.pose.bones:
        for c in list(pb.constraints):
            pb.constraints.remove(c)
        pb.rotation_mode = 'QUATERNION'
    if arm.animation_data:
        arm.animation_data_clear()
    for a in list(bpy.data.actions):
        bpy.data.actions.remove(a)
    # A metros y con el objeto del rig en el origen sin girar: la misma matriz (la del mundo, por 0,1) va a los huesos y a
    # los vértices, así siguen calzando.
    antes = [piel.matrix_world @ v.co for v in piel.data.vertices[:3]]
    esc = Matrix.Scale(ESCALA, 4)
    m_arm = esc @ arm.matrix_world
    m_piel = esc @ piel.matrix_world
    piel.data.transform(m_piel)
    arm.matrix_world = Matrix.Identity(4)
    arm.data.transform(m_arm)                    # todos los huesos juntos (de a uno, las articulaciones se moverían dos veces)
    piel.matrix_parent_inverse = Matrix.Identity(4)
    piel.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()
    mano = arm.data.bones['hand.R'].head_local
    cerca = min((piel.matrix_world @ v.co - mano).length for v in piel.data.vertices)
    print('HUESO hand.R', tuple(round(c, 3) for c in mano), 'vértice más cerca a %.3f m' % cerca)
    for n in ('upper_arm.R', 'forearm.R', 'hand.R'):
        print('  HUESO', n, tuple(round(c, 3) for c in arm.data.bones[n].head_local), tuple(round(c, 3) for c in arm.data.bones[n].tail_local))
    despues = [piel.matrix_world @ v.co for v in piel.data.vertices[:3]]
    print('A METROS: antes', [tuple(round(c, 4) for c in v * ESCALA) for v in antes], 'después', [tuple(round(c, 4) for c in v) for v in despues])
    # La mano colgada del antebrazo (sin el hueso de control del IK).
    modo(arm, 'EDIT')
    eb = arm.data.edit_bones
    for s in LADOS:
        h = eb['hand.' + s]
        h.parent = eb['forearm.' + s]
        h.use_connect = False
        eb.remove(eb['hand.' + s + '.control'])
    modo(arm, 'OBJECT')
    arm.name = 'Brazos'; arm.data.name = 'Brazos'
    return arm, piel


def mitad(piel, lado):
    """La piel de un solo brazo (los dos brazos son piezas sueltas: el derecho del lado +X)."""
    o = piel.copy(); o.data = piel.data.copy(); o.name = 'Piel_' + lado
    bpy.context.scene.collection.objects.link(o)
    for m in list(o.modifiers):
        o.modifiers.remove(m)
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if (v.co.x < 0) == (lado == 'R')], context='VERTS')
    bm.to_mesh(o.data); bm.free()
    return o


# ------------------------------------------------------------------ Medidas del brazo (en reposo)
class Brazo:
    def __init__(self, arm, lado):
        b = arm.data.bones
        self.lado = lado
        self.S = b['upper_arm.' + lado].head_local.copy()
        self.E = b['forearm.' + lado].head_local.copy()
        self.W = b['hand.' + lado].head_local.copy()
        self.M = {f: b['palm_%s.%s' % (f, lado)].tail_local.copy() for f in ('index', 'middle', 'ring', 'pinky')}
        self.A = (self.M['middle'] - self.W).normalized()                 # el eje de la mano (de la muñeca a los nudillos)
        self.K = (self.M['index'] - self.M['pinky']).normalized()          # la línea de los nudillos, hacia el índice
        n = self.K.cross(self.A) if lado == 'R' else self.A.cross(self.K)
        self.N = n.normalized()                                            # hacia afuera de la palma
        self.Ml = (self.E - self.W).length
        self.largo_brazo = (self.S - self.E).length

    def a_lo_largo(self, p):
        """Distancia desde la muñeca por el brazo (0 en la muñeca, sube hacia el hombro), radio y ángulo alrededor."""
        mejor = None
        a0 = 0.0
        for p0, p1 in ((self.W, self.E), (self.E, self.S)):
            d = p1 - p0; L = d.length; u = d / L
            t = max(0.0, min(L, (p - p0).dot(u)))
            q = p0 + u * t
            dist = (p - q).length
            if mejor is None or dist < mejor[0]:
                mejor = (dist, a0 + t, u, q)
            a0 += L
        dist, a, u, q = mejor
        s = 1.0 if self.lado == 'R' else -1.0
        ref = (Vector((-s, 0.0, -1.0)) - u * Vector((-s, 0.0, -1.0)).dot(u)).normalized()   # la costura: abajo, hacia adentro
        otro = u.cross(ref)
        r = p - q
        ang = math.atan2(r.dot(otro), r.dot(ref)) % (2 * math.pi)
        return a, dist, ang, u


# ------------------------------------------------------------------ Herramientas de malla
def aplicar(o, tipo, **props):
    m = o.modifiers.new('m', tipo)
    for k, v in props.items():
        setattr(m, k, v)
    with bpy.context.temp_override(object=o, active_object=o, selected_objects=[o], selected_editable_objects=[o]):
        bpy.ops.object.modifier_apply(modifier=m.name)


def recortar(o, punto, normal):
    """Corta con un plano y borra lo que queda del lado hacia donde apunta la normal (corte prolijo: un anillo de aristas)."""
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=punto, plane_no=normal,
                           clear_outer=True, dist=1e-7)
    bm.to_mesh(o.data); bm.free()


def empujar(o, cuanto):
    """Infla la malla: cada vértice sale por su normal (lo que abulta la tela)."""
    aplicar(o, 'DISPLACE', strength=cuanto, mid_level=0.0, direction='NORMAL')


def suavizar(o, veces, factor=0.5, volumen=True):
    aplicar(o, 'LAPLACIANSMOOTH', iterations=veces, lambda_factor=factor, use_volume_preserve=volumen,
            use_x=True, use_y=True, use_z=True)


def borde_con_espesor(o, plano_co, plano_no, espesor, adentro):
    """El borde abierto junto al plano (el puño de la manga o del guante) gana espesor: un labio hacia adentro, para que
    no se vea como una hoja de papel. 'adentro' es cuánto entra el labio (a lo largo del brazo)."""
    bm = bmesh.new(); bm.from_mesh(o.data)
    bm.verts.ensure_lookup_table()
    borde = [e for e in bm.edges if len(e.link_faces) == 1 and all(abs((v.co - plano_co).dot(plano_no)) < 0.004 for v in e.verts)]
    if not borde:
        bm.free(); return
    centro = sum((v.co for e in borde for v in e.verts), Vector()) / (2 * len(borde))
    r1 = bmesh.ops.extrude_edge_only(bm, edges=borde)
    nuevos = [g for g in r1['geom'] if isinstance(g, bmesh.types.BMVert)]
    for v in nuevos:                                   # hacia el eje (el espesor de la tela)
        d = v.co - centro; d = d - plano_no * d.dot(plano_no)
        v.co -= d.normalized() * espesor
    bordes2 = [g for g in r1['geom'] if isinstance(g, bmesh.types.BMEdge) and all(v in nuevos for v in g.verts)]
    r2 = bmesh.ops.extrude_edge_only(bm, edges=bordes2)
    for g in r2['geom']:                               # y para adentro de la manga
        if isinstance(g, bmesh.types.BMVert):
            g.co -= plano_no * adentro
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(o.data); bm.free()


def materiales(o, nombres):
    o.data.materials.clear()
    for n in nombres:
        o.data.materials.append(bpy.data.materials.get(n) or bpy.data.materials.new(n))


def uv_cilindrica(o, br, radio_medio):
    """UV como un tubo a lo largo del brazo: la trama de la tela sigue el brazo y la costura queda abajo, hacia adentro."""
    me = o.data
    if not me.uv_layers:
        me.uv_layers.new(name='UVMap')
    uv = me.uv_layers.active.data
    periodo = 2 * math.pi * radio_medio / UV_TAM
    for p in me.polygons:
        us = []
        for li in p.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            a, dist, ang, _ = br.a_lo_largo(co)
            us.append((li, ang * radio_medio / UV_TAM, a / UV_TAM))
        hi = max(u for _, u, _ in us); lo = min(u for _, u, _ in us)
        for li, u, v in us:
            if hi - lo > periodo / 2 and u < periodo / 2:
                u += periodo
            uv[li].uv = (u, v)


def uv_caja(o, tam):
    """Proyección de caja: cada cara toma la UV del plano al que más mira (como el cube_project de Blender)."""
    me = o.data
    if not me.uv_layers:
        me.uv_layers.new(name='UVMap')
    uv = me.uv_layers.active.data
    for p in me.polygons:
        n = p.normal
        eje = max(range(3), key=lambda i: abs(n[i]))
        a, b = [i for i in range(3) if i != eje]
        for li in p.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            uv[li].uv = (co[a] / tam, co[b] / tam)


def uv_a_escala(o, tam):
    """Las UV originales de la mano (prolijas y parejas) llevadas a tamaño real: una vuelta de textura cada 'tam' metros."""
    me = o.data
    uv = me.uv_layers.active.data
    l3 = luv = 0.0
    for p in me.polygons:
        idx = list(p.loop_indices)
        for a, b in zip(idx, idx[1:] + idx[:1]):
            l3 += (me.vertices[me.loops[a].vertex_index].co - me.vertices[me.loops[b].vertex_index].co).length
            luv += (uv[a].uv - uv[b].uv).length
    k = (l3 / max(luv, 1e-9)) / tam
    for d in uv:
        d.uv = d.uv * k


def rellenar_pesos(o, piel):
    """Los vértices sin peso (los hay en el original) toman los del vértice con peso más cercano: si no, se quedarían
    quietos cuando el brazo se mueve."""
    from mathutils.kdtree import KDTree
    con = [v for v in o.data.vertices if v.groups]
    sin = [v for v in o.data.vertices if not v.groups]
    if not sin:
        return
    kd = KDTree(len(con))
    for i, v in enumerate(con):
        kd.insert(v.co, i)
    kd.balance()
    nombres = {g.index: g.name for g in o.vertex_groups}
    for v in sin:
        _, i, _ = kd.find(v.co)
        for g in con[i].groups:
            o.vertex_groups[nombres[g.group]].add([v.index], g.weight, 'REPLACE')


def suave(o):
    for p in o.data.polygons:
        p.use_smooth = True


def pesos_ok(o):
    sin = sum(1 for v in o.data.vertices if not v.groups)
    return sin


# ------------------------------------------------------------------ Pesos (de qué hueso depende cada vértice)
def copiar_pesos(destino, piel, vecinos=4, max_huesos=4):
    """Cada vértice de la ropa toma los pesos de la piel que tiene debajo (los vértices más cercanos, pesados por cercanía):
    así se dobla igual que el brazo. Se quedan los 4 huesos que más pesan (lo que acepta Unity)."""
    from mathutils.kdtree import KDTree
    pv = piel.data.vertices
    con_peso = [v for v in pv if v.groups]
    kd = KDTree(len(con_peso))
    for i, v in enumerate(con_peso):
        kd.insert(v.co, i)
    kd.balance()
    nombres = {g.index: g.name for g in piel.vertex_groups}
    grupos = {}
    for n in nombres.values():
        grupos[n] = destino.vertex_groups.get(n) or destino.vertex_groups.new(name=n)
    for v in destino.data.vertices:
        acum = {}
        for co, i, d in kd.find_n(v.co, vecinos):
            w = 1.0 / max(d, 1e-4) ** 2
            for g in con_peso[i].groups:
                acum[nombres[g.group]] = acum.get(nombres[g.group], 0.0) + w * g.weight
        top = sorted(acum.items(), key=lambda kv: -kv[1])[:max_huesos]
        total = sum(w for _, w in top) or 1.0
        for n in nombres.values():
            grupos[n].remove([v.index])
        for n, w in top:
            grupos[n].add([v.index], w / total, 'REPLACE')


# ------------------------------------------------------------------ La manga
def centro_del_brazo(br, desde, hasta_extra, paso):
    """El eje del brazo como una curva suave (el codo redondeado), desde 'desde' metros antes de la muñeca hasta el hombro
    (más 'hasta_extra'). Devuelve puntos cada 'paso' metros, con su distancia recorrida."""
    u_mano = (br.W - br.E).normalized()
    u_hombro = (br.S - br.E).normalized()
    pts = [br.W - u_mano * desde, br.E, br.S + u_hombro * hasta_extra]
    for _ in range(4):                                   # esquinas cortadas (Chaikin): el codo queda redondo
        nuevos = [pts[0]]
        for a, b in zip(pts[:-1], pts[1:]):
            nuevos += [a.lerp(b, 0.25), a.lerp(b, 0.75)]
        nuevos.append(pts[-1])
        pts = nuevos
    largo = [0.0]
    for a, b in zip(pts[:-1], pts[1:]):
        largo.append(largo[-1] + (b - a).length)
    salida, s = [], 0.0
    while s <= largo[-1]:
        k = max(i for i in range(len(largo)) if largo[i] <= s)
        k = min(k, len(pts) - 2)
        t = (s - largo[k]) / max(largo[k + 1] - largo[k], 1e-9)
        salida.append((pts[k].lerp(pts[k + 1], t), s))
        s += paso
    return salida


def suavizar_radios(rs, sj, si):
    n, m = len(rs), len(rs[0])
    out = [[0.0] * m for _ in range(n)]
    for i in range(n):
        for j in range(m):
            acum = pesos = 0.0
            for di in range(-2 * si, 2 * si + 1):
                ii = min(max(i + di, 0), n - 1)
                for dj in range(-2 * sj, 2 * sj + 1):
                    w = math.exp(-(di / max(si, 1e-3)) ** 2 / 2 - (dj / max(sj, 1e-3)) ** 2 / 2)
                    acum += w * rs[ii][(j + dj) % m]; pesos += w
            out[i][j] = acum / pesos
    return out


def manga(piel, br, nombre, campera, guante_adentro):
    """La manga como un tubo prolijo que copia la forma del brazo (se mide el contorno de la piel en cada altura y se le suma
    la holgura de la tela), con el puño doblado hacia adentro, la costura abajo del lado del cuerpo y la trama derecha."""
    from mathutils.bvhtree import BVHTree
    bm_piel = bmesh.new(); bm_piel.from_mesh(piel.data)
    arbol = BVHTree.FromBMesh(bm_piel)
    bm_g = bmesh.new(); bm_g.from_mesh(guante_adentro.data)
    arbol_g = BVHTree.FromBMesh(bm_g)                     # el puño del guante que queda adentro de la manga
    corte = 0.032 if campera else 0.030                 # el borde del puño, antes de la muñeca
    holgura = 0.020 if campera else 0.007
    vueltas = 32
    eje = centro_del_brazo(br, corte, 0.03, 0.009)
    s_lado = 1.0 if br.lado == 'R' else -1.0
    # Marcos que viajan por la curva sin retorcerse; ángulo 0 = la costura (abajo, del lado del cuerpo).
    marcos = []
    ref = Vector((-s_lado, 0.0, -1.0))
    for i, (p, s) in enumerate(eje):
        a = eje[max(i - 1, 0)][0]; b = eje[min(i + 1, len(eje) - 1)][0]
        t = (b - a).normalized()                          # del puño hacia el hombro
        ref = (ref - t * ref.dot(t)).normalized()
        marcos.append((p, s, t, ref.copy(), t.cross(ref).normalized()))
    # El contorno de la piel en cada anillo (rayos desde el eje).
    radios, radios_g = [], []
    for p, s, t, r0, r1 in marcos:
        fila, fila_g = [], []
        for j in range(vueltas):
            ang = 2 * math.pi * j / vueltas
            d = r0 * math.cos(ang) + r1 * math.sin(ang)
            hit = arbol.ray_cast(p, d, 0.12)
            fila.append((hit[0] - p).length if hit[0] is not None else None)
            hg = arbol_g.ray_cast(p, d, 0.12)
            fila_g.append((hg[0] - p).length if hg[0] is not None else 0.0)
        radios.append(fila)
        radios_g.append(fila_g)
    bm_piel.free(); bm_g.free()
    # Huecos (rayos que no tocaron piel: el hombro abierto) y suavizado: la tela no copia los músculos.
    for i, fila in enumerate(radios):
        validos = [r for r in fila if r is not None]
        media = sum(validos) / len(validos) if validos else (sum(radios[i - 1]) / vueltas if i else 0.04)
        radios[i] = [r if r is not None else media for r in fila]
    radios = suavizar_radios(radios, 2, 2)
    # Forma final de la tela: holgura, puño, acolchado o pliegues de lana.
    puno_largo = 0.045 if campera else 0.055
    largo_codo = br.Ml - corte
    espesor = 0.004 if campera else 0.003
    adentro = 0.018

    def radio(i, j):
        p, s, t, r0, r1 = marcos[i]
        ang = 2 * math.pi * j / vueltas
        r = radios[i][j] + holgura
        codo = math.exp(-((s - largo_codo) / 0.045) ** 2)
        if campera:
            if s > puno_largo:
                f = abs(math.sin(math.pi * (s - puno_largo) / 0.07)) ** 0.6     # bultos del acolchado entre costuras
                r += 0.007 * f - 0.002
            else:
                r -= 0.006 * (1.0 - s / puno_largo) ** 0.5                      # el puño, más ajustado
            r += 0.003 * codo * math.sin(ang * 3.0 + s * 40.0)                   # arrugas en el pliegue del codo
        else:
            if s < puno_largo:                                                    # puño elástico con canaletas
                r -= 0.006 * min(1.0, (puno_largo - s) / 0.012)
                r += 0.0011 * math.cos(ang * vueltas / 2)
            else:
                r += 0.0022 * math.sin(ang * 2.0 + s * 22.0) * min(1.0, (s - puno_largo) / 0.03)
            r += 0.004 * codo * math.sin(ang * 3.0 + s * 45.0)
        # Nunca más angosta que el guante que tiene adentro (con el espesor de la tela y un poco de aire).
        return max(r, radios_g[i][j] + espesor + 0.0015)

    bm = bmesh.new()
    uv_capa = bm.loops.layers.uv.new('UVMap')
    perimetro = sum(2 * math.pi * sum(f) / len(f) for f in radios) / len(radios)
    # Anillos: primero el labio de adentro (vuelve 1,8 cm hacia el codo), el canto redondeado, y después la manga.
    anillos = []                                         # (vértices, v de la textura, es de adentro)
    p0, s0, t0, r00, r10 = marcos[0]
    for despl, menos, dentro in ((adentro, espesor, True), (0.003, espesor, True), (-0.0012, espesor * 0.5, False)):
        fila = []
        for j in range(vueltas):
            ang = 2 * math.pi * j / vueltas
            d = r00 * math.cos(ang) + r10 * math.sin(ang)
            fila.append(bm.verts.new(p0 + t0 * despl + d * (radio(0, j) - menos)))
        anillos.append((fila, -(adentro - despl) / UV_TAM - 0.03 if dentro else -0.012, dentro))
    for i, (p, s, t, r0, r1) in enumerate(marcos):
        fila = []
        for j in range(vueltas):
            ang = 2 * math.pi * j / vueltas
            d = r0 * math.cos(ang) + r1 * math.sin(ang)
            fila.append(bm.verts.new(p + d * radio(i, j)))
        anillos.append((fila, s / UV_TAM, False))
    for k in range(len(anillos) - 1):
        a, va, dentro = anillos[k]; b, vb, _ = anillos[k + 1]
        for j in range(vueltas):
            j2 = (j + 1) % vueltas
            f = bm.faces.new((a[j], a[j2], b[j2], b[j]))
            f.material_index = 1 if (dentro and campera) else 0
            f.smooth = True
            u1, u2 = j / vueltas * perimetro / UV_TAM, (j + 1) / vueltas * perimetro / UV_TAM
            for loop, uv in zip(f.loops, ((u1, va), (u2, va), (u2, vb), (u1, vb))):
                loop[uv_capa].uv = uv
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(nombre)
    for n in (['MT_LonaMarron', 'MT_TelaGris'] if campera else ['MT_TelaManta']):   # antes de la malla: así quedan los índices
        me.materials.append(bpy.data.materials.get(n) or bpy.data.materials.new(n))
    bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(nombre, me); bpy.context.scene.collection.objects.link(o)
    copiar_pesos(o, piel)
    return o


# ------------------------------------------------------------------ El guante
def guante(piel, br, nombre, grueso):
    o = piel.copy(); o.data = piel.data.copy(); o.name = nombre
    bpy.context.scene.collection.objects.link(o)
    u_codo = (br.E - br.W).normalized()
    largo_puno = 0.075 if grueso else 0.050               # hasta dónde sube el puño del guante (queda adentro de la manga)
    p_corte = br.W + u_codo * largo_puno
    recortar(o, p_corte, u_codo)
    suavizar(o, 3, 0.35)                                   # sin uñas ni arrugas de piel
    empujar(o, 0.0045 if grueso else 0.0022)
    if grueso:
        suavizar(o, 3, 0.3)
    bm = bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table(); bm.normal_update()
    for v in bm.verts:                                     # el puño: se abre un poco hacia el antebrazo
        a = (v.co - br.W).dot(u_codo)
        if a > 0.0:
            t = min(1.0, a / largo_puno)
            d = v.co - br.W; d = d - u_codo * d.dot(u_codo)
            if grueso:
                v.co += d.normalized() * (0.010 * t * t + 0.002)
            else:
                v.co += d.normalized() * 0.0015 + v.normal * 0.0008 * math.cos(math.atan2(d.dot(br.N), d.dot(br.K)) * 30.0) * t
    bm.to_mesh(o.data); bm.free()
    # Materiales cortados por planos (bordes rectos): la goma o el cuero en los dedos (de los nudillos para adelante) y en la
    # palma; el resto (dorso y puño) en tejido.
    nudillos = sum(br.M.values(), Vector()) / 4 - br.A * 0.006
    materiales(o, ['MT_Goma'] if grueso else ['MT_Goma', 'MT_TelaGris'])   # antes de asignar: vaciar la lista borra lo asignado
    bm = bmesh.new(); bm.from_mesh(o.data)
    for co, no in ((nudillos, br.A), (br.W, br.N), (br.W + u_codo * 0.012, u_codo)):
        bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=co, plane_no=no, dist=1e-7)
    for f in bm.faces:
        c = f.calc_center_median()
        en_puno = (c - br.W).dot(u_codo) > 0.012
        dedos = (c - nudillos).dot(br.A) > 0
        palma = (c - br.W).dot(br.N) > 0
        if grueso:
            f.material_index = 0
        else:
            f.material_index = 0 if (not en_puno and (dedos or palma)) else 1
        f.smooth = True
    bm.to_mesh(o.data); bm.free()
    # El borde del puño del guante queda adentro de la manga: no necesita labio.
    uv_a_escala(o, 0.15)
    rellenar_pesos(o, piel)
    suave(o)
    return o


def unir_al_rig(o, arm):
    o.parent = arm
    o.matrix_parent_inverse = arm.matrix_world.inverted()
    m = o.modifiers.new('Armature', 'ARMATURE'); m.object = arm


def tris(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)


# ------------------------------------------------------------------ Poses de los dedos
DEDOS = ('index', 'middle', 'ring', 'pinky')
MANGO_R = 0.0145                 # el mango que agarra el puño (la cinta de la barreta, el mango del martillo)
NUDILLO_MIN = 60.0             # los nudillos, por lo menos así de doblados: si no, la mano queda como un gancho y no como un puño
PALMA_ESPESOR = 0.009          # de los huesos de la palma a la piel: el mango no se mete en la palma
DIAGONAL = 14.0                # cuánto más en diagonal que la línea de los nudillos (grados)
GROSOR = {'01': 0.0095, '02': 0.0087, '03': 0.0078}       # radio de cada falange (sin guante)
FLEX_MAX = {'01': 95.0, '02': 110.0, '03': 85.0}


def punto(arm, nombre, cola=False):
    pb = arm.pose.bones[nombre]
    return (pb.tail if cola else pb.head).copy()


def girar(arm, nombre, eje, grados):
    """Gira un hueso (en la pose) alrededor de un eje del mundo que pasa por su cabeza."""
    pb = arm.pose.bones[nombre]
    h = pb.head.copy()
    pb.matrix = Matrix.Translation(h) @ Matrix.Rotation(math.radians(grados), 4, eje) @ Matrix.Translation(-h) @ pb.matrix
    bpy.context.view_layer.update()


def dist_segmentos(p1, q1, p2, q2):
    """Distancia mínima entre dos segmentos."""
    d1, d2, r = q1 - p1, q2 - p2, p1 - p2
    a, e, f = d1.dot(d1), d2.dot(d2), d2.dot(r)
    if a < 1e-12 and e < 1e-12:
        return (p1 - p2).length
    if a < 1e-12:
        s, t = 0.0, max(0.0, min(1.0, f / e))
    else:
        c = d1.dot(r)
        if e < 1e-12:
            t, s = 0.0, max(0.0, min(1.0, -c / a))
        else:
            b = d1.dot(d2); den = a * e - b * b
            s = max(0.0, min(1.0, (b * f - c * e) / den)) if den > 1e-12 else 0.0
            t = (b * s + f) / e
            if t < 0.0:
                t, s = 0.0, max(0.0, min(1.0, -c / a))
            elif t > 1.0:
                t, s = 1.0, max(0.0, min(1.0, (b - c) / a))
    return ((p1 + d1 * s) - (p2 + d2 * t)).length


def eje_flexion(arm, nombre, N):
    """El eje alrededor del que se dobla una falange: perpendicular al hueso y a la palma; positivo = cerrar hacia la palma."""
    p, q = punto(arm, nombre), punto(arm, nombre, True)
    d = (q - p).normalized()
    eje = d.cross(N).normalized()
    # Comprobar el sentido con un giro chico: la punta tiene que ir hacia la palma.
    antes = (q - p).dot(N)
    rot = Matrix.Rotation(math.radians(5), 3, eje)
    if (rot @ (q - p)).dot(N) < antes:
        eje = -eje
    return eje


class Mango:
    """El mango que agarra el puño: un cilindro de radio MANGO_R (más el espesor del guante), en diagonal sobre la palma."""

    def __init__(self, br, guante_mm):
        self.g = guante_mm / 1000.0
        h = (br.K - br.N * br.K.dot(br.N)).normalized()      # paralelo a la palma, hacia el índice (el lado del pulgar)
        a = (br.A - br.N * br.A.dot(br.N) - h * br.A.dot(h)).normalized()
        # En diagonal, como en una mano de verdad: del índice (en la base de los dedos) hacia la carne del meñique.
        self.H = (h + a * math.tan(math.radians(DIAGONAL))).normalized()
        self.poner(br.M['middle'] + br.N * 0.03)

    def poner(self, C):
        self.C = C.copy()
        self.a = self.C - self.H * 0.09; self.b = self.C + self.H * 0.09

    def choca(self, p, q, radio):
        return dist_segmentos(p, q, self.a, self.b) < MANGO_R + radio + self.g


class Dedo:
    """La cadena de un dedo (nudillo, falanges y punta) en reposo, con su eje de flexión: para probar cierres sin mover huesos."""

    def __init__(self, arm, br, dedo):
        s = br.lado
        self.nombres = ['f_%s.%s.%s' % (dedo, k, s) for k in ('01', '02', '03')]
        self.P = [punto(arm, self.nombres[0]), punto(arm, self.nombres[1]), punto(arm, self.nombres[2]), punto(arm, self.nombres[2], True)]
        self.eje = eje_flexion(arm, self.nombres[0], br.N)
        self.palma = (punto(arm, 'palm_%s.%s' % (dedo, s)), punto(arm, 'palm_%s.%s' % (dedo, s), True))
        self.dedo = dedo

    def cadena(self, t1, t2, t3):
        q = [p.copy() for p in self.P]
        for j, t in enumerate((t1, t2, t3)):
            rot = Matrix.Rotation(math.radians(t), 3, self.eje)
            for k in range(j + 1, 4):
                q[k] = q[j] + rot @ (q[k] - q[j])
        return q

    def costo(self, mango, t1, t2, t3):
        """Que las tres falanges apoyen en el mango (a la distancia de contacto) sin atravesarlo; entre parecidos, el más cerrado."""
        q = self.cadena(t1, t2, t3)
        c = 0.0
        for k, g in enumerate(('01', '02', '03')):
            falta = dist_segmentos(q[k], q[k + 1], mango.a, mango.b) - (MANGO_R + GROSOR[g] + mango.g)
            c += 400.0 * falta * falta + 0.02 * -falta if falta < 0 else (1.0, 1.0, 0.6)[k] * falta * falta
        return c * 1e4 - 0.00002 * (t1 + t2 + t3)

    def mejor(self, mango, paso=5, t1_min=NUDILLO_MIN):
        mejor = None
        for t1 in range(int(t1_min), int(FLEX_MAX['01']) + 1, paso):
            for t2 in range(0, int(FLEX_MAX['02']) + 1, paso):
                for t3 in (t2 * 0.55, t2 * 0.75, t2 * 0.95):
                    if t3 > FLEX_MAX['03']:
                        continue
                    c = self.costo(mango, t1, t2, t3)
                    if mejor is None or c < mejor[0]:
                        mejor = (c, t1, t2, t3)
        return mejor


def ubicar_mango(arm, br, mango):
    """Busca dónde cruza el mango la mano: el lugar (delante de la palma, apenas antes de los nudillos) en que los cuatro dedos lo rodean mejor con los
    nudillos bien doblados, sin que el mango se meta en la palma."""
    dedos = [Dedo(arm, br, d) for d in DEDOS]
    m = br.M['middle']
    mejor = None
    for da in [x * 0.0025 for x in range(-20, 5)]:           # de 5 cm antes a 1 cm después de los nudillos (delante de la palma)
        for dn in [x * 0.0025 for x in range(2, 17)]:        # de 0,5 a 4 cm delante de la palma
            mango.poner(m + br.A * da + br.N * dn)
            c = 0.0
            for d in dedos:
                c += d.mejor(mango, paso=10)[0]
                p0, p1 = d.palma
                falta = dist_segmentos(p0, p1, mango.a, mango.b) - (MANGO_R + mango.g + PALMA_ESPESOR)
                if falta < 0:
                    c += 4e6 * falta * falta
            if mejor is None or c < mejor[0]:
                mejor = (c, da, dn)
    _, da, dn = mejor
    mango.poner(m + br.A * da + br.N * dn)
    print('    mango: %.1f cm más allá de los nudillos, %.1f cm delante de la palma' % (da * 100, dn * 100))
    return dedos


def cerrar_dedo(arm, d, mango):
    """Cierra un dedo alrededor del mango (busca los tres ángulos juntos, después afina) y lo aplica a los huesos."""
    c, t1, t2, t3 = d.mejor(mango, paso=3)
    for paso in (1.0, 0.5):
        cambio = True
        while cambio:
            cambio = False
            for d1, d2, d3 in ((paso, 0, 0), (-paso, 0, 0), (0, paso, 0), (0, -paso, 0), (0, 0, paso), (0, 0, -paso)):
                n = (t1 + d1, t2 + d2, t3 + d3)
                if not (NUDILLO_MIN <= n[0] <= FLEX_MAX['01'] and 0 <= n[1] <= FLEX_MAX['02'] and 0 <= n[2] <= FLEX_MAX['03']):
                    continue
                if not (0.5 * n[1] - 1 <= n[2] <= n[1] + 1):     # la última falange se dobla con la del medio (como en una mano)
                    continue
                cn = d.costo(mango, *n)
                if cn < c:
                    c, (t1, t2, t3), cambio = cn, n, True
    q = d.cadena(t1, t2, t3)
    huecos = [dist_segmentos(q[k], q[k + 1], mango.a, mango.b) - (MANGO_R + GROSOR[g] + mango.g) for k, g in enumerate(('01', '02', '03'))]
    for nombre, t in zip(d.nombres, (t1, t2, t3)):
        girar(arm, nombre, d.eje, t)
    print('    %s: %.0f° %.0f° %.0f°; separación del mango %s mm' % (d.dedo, t1, t2, t3, ', '.join('%.1f' % (h * 1000) for h in huecos)))


def cerrar_pulgar(arm, br, mango):
    """El pulgar rodea el mango y apoya sobre la falange del medio del índice (CCD: se acerca la punta al objetivo)."""
    s = br.lado
    medio = (punto(arm, 'f_index.02.' + s) + punto(arm, 'f_index.02.' + s, True)) / 2
    radial = medio - mango.C; radial = (radial - mango.H * radial.dot(mango.H)).normalized()
    objetivo = medio + radial * (GROSOR['02'] + 0.0085 + 2 * mango.g) - mango.H * 0.004
    cadena = ['thumb.03.' + s, 'thumb.02.' + s, 'thumb.01.' + s]
    for _ in range(30):
        for nombre in cadena:
            j = punto(arm, nombre)
            punta = punto(arm, 'thumb.03.' + s, True)
            a = (punta - j); b = (objetivo - j)
            if a.length < 1e-6 or b.length < 1e-6:
                continue
            eje = a.cross(b)
            if eje.length < 1e-8:
                continue
            ang = min(math.degrees(a.angle(b)), 8.0)
            girar(arm, nombre, eje.normalized(), ang * 0.5)
    print('    pulgar: la punta quedó a %.1f mm del objetivo' % ((punto(arm, 'thumb.03.' + s, True) - objetivo).length * 1000))
    # Si atraviesa el mango, se aleja un poco.
    for nombre in cadena[::-1]:
        p, q = punto(arm, nombre), punto(arm, nombre, True)
        if mango.choca(p, q, 0.0085):
            print('    AVISO: el pulgar toca el mango en', nombre)


def abrir_mano(arm, br):
    """Dedos juntos y casi rectos, el pulgar al costado del índice (la palma apoyada contra una caja)."""
    s = br.lado
    for dedo in DEDOS:
        for k, cuanto in (('01', 0.8), ('02', 0.85), ('03', 0.85)):
            nombre = 'f_%s.%s.%s' % (dedo, k, s)
            padre = arm.pose.bones[nombre].parent
            d_padre = (padre.tail - padre.head).normalized()
            p, q = punto(arm, nombre), punto(arm, nombre, True)
            d = (q - p).normalized()
            ang = math.degrees(d.angle(d_padre))
            eje = d.cross(d_padre)
            if eje.length > 1e-6:
                girar(arm, nombre, eje.normalized(), ang * cuanto)
    # los dedos juntos: cada uno gira un poco hacia el mayor
    centro = (punto(arm, 'f_middle.01.' + s) + punto(arm, 'f_middle.03.' + s, True)) / 2
    for dedo in ('index', 'ring', 'pinky'):
        nombre = 'f_%s.01.%s' % (dedo, s)
        p = punto(arm, nombre); q = punto(arm, 'f_%s.03.%s' % (dedo, s), True)
        hacia = (centro - q); hacia = hacia - br.N * hacia.dot(br.N)
        d = (q - p)
        eje = d.cross(hacia)
        if eje.length > 1e-8:
            girar(arm, nombre, eje.normalized(), min(6.0, math.degrees(d.angle(d + hacia * 0.5))))


# Cada pose va en su cuadro de una sola animación "Poses" (el exportador de FBX de Blender 5 deja vacías las acciones
# sueltas): en Unity se lee cada pose en su cuadro.
CUADROS = {'Reposo': 1, 'Puno': 11, 'Puno_Grueso': 21, 'Abierta': 31}


def guardar_pose(arm, nombre):
    """La pose actual, en su cuadro de la animación de las poses."""
    if not arm.animation_data:
        arm.animation_data_create()
    act = bpy.data.actions.get('Poses') or bpy.data.actions.new('Poses')
    act.use_fake_user = True
    arm.animation_data.action = act
    for pb in arm.pose.bones:
        pb.keyframe_insert('rotation_quaternion', frame=CUADROS[nombre])
        pb.keyframe_insert('location', frame=CUADROS[nombre])
    arm.animation_data.action = None      # mientras se posa, la animación no tiene que pisar los huesos


def a_reposo(arm):
    for pb in arm.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()


def huesos_de_agarre(arm, brazos, mangos):
    """Huesos extra que no deforman, hijos de la mano: el centro del mango, un punto hacia el pulgar y el centro de la palma."""
    modo(arm, 'EDIT')
    eb = arm.data.edit_bones
    for s, br in brazos.items():
        m = mangos[s]
        mano = eb['hand.' + s]
        for nombre, cabeza, cola in (('agarre.' + s, m.C, m.C + m.H * 0.03), ('agarre_pulgar.' + s, m.C + m.H * 0.03, m.C + m.H * 0.05),
                                     ('palma.' + s, m.palma, m.palma + br.N * 0.02)):
            b = eb.new(nombre); b.head = cabeza; b.tail = cola; b.parent = mano; b.use_deform = False
    modo(arm, 'OBJECT')


def punto_de_palma(br, piel):
    from mathutils.bvhtree import BVHTree
    bm = bmesh.new(); bm.from_mesh(piel.data); arbol = BVHTree.FromBMesh(bm)
    base = br.W + (br.M['middle'] - br.W) * 0.5
    hit = arbol.ray_cast(base, br.N, 0.1); bm.free()
    return (hit[0] if hit[0] is not None else base + br.N * 0.015) + br.N * 0.004


# ------------------------------------------------------------------ Armar
arm, piel = preparar()
brazos = {s: Brazo(arm, s) for s in LADOS}
pieles = {s: mitad(piel, s) for s in LADOS}
ropa = []
for s in LADOS:
    br, pl = brazos[s], pieles[s]
    trabajo = guante(pl, br, 'Guante_Trabajo_' + s, False)
    grueso = guante(pl, br, 'Guante_Grueso_' + s, True)
    for o in (manga(pl, br, 'Manga_Buzo_' + s, False, trabajo), manga(pl, br, 'Manga_Campera_' + s, True, grueso), trabajo, grueso):
        unir_al_rig(o, arm)
        ropa.append(o)
        print('ROPA', o.name, 'tris', tris(o), 'vértices sin peso', pesos_ok(o))
print('MEDIDAS R: brazo %.3f m, antebrazo %.3f m' % (brazos['R'].largo_brazo, brazos['R'].Ml))

# Dónde cruza el mango cada mano (en reposo) y el centro de la palma. El mango es el mismo con los dos guantes: con el
# grueso los dedos cierran un poco menos.
a_reposo(arm)
mangos, mangos_gruesos = {}, {}
for s, br in brazos.items():
    print('MANGO', s)
    mangos[s] = Mango(br, 2.5)
    ubicar_mango(arm, br, mangos[s])
    mangos[s].palma = punto_de_palma(br, pieles[s])
    mangos_gruesos[s] = Mango(br, 5.0)
    mangos_gruesos[s].poner(mangos[s].C)
huesos_de_agarre(arm, brazos, mangos)

# Las poses: puño (guante de trabajo), puño (guante grueso), abierta y reposo.
for nombre, juego in (('Puno', mangos), ('Puno_Grueso', mangos_gruesos)):
    a_reposo(arm)
    for s, br in brazos.items():
        print('POSE', nombre, s)
        for d in [Dedo(arm, br, x) for x in DEDOS]:
            cerrar_dedo(arm, d, juego[s])
        cerrar_pulgar(arm, br, juego[s])
    guardar_pose(arm, nombre)
a_reposo(arm)
for s, br in brazos.items():
    abrir_mano(arm, br)
guardar_pose(arm, 'Abierta')
a_reposo(arm)
guardar_pose(arm, 'Reposo')

for s in LADOS:
    bpy.data.objects.remove(pieles[s])
bpy.data.objects.remove(piel)

# Exportar: el rig, las 8 piezas de ropa y las 4 poses (sin la piel: siempre queda tapada).
arm.animation_data.action = bpy.data.actions['Poses']
bpy.context.scene.frame_start, bpy.context.scene.frame_end = 1, max(CUADROS.values())
for o in bpy.data.objects:
    o.select_set(o == arm or o in ropa)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(filepath=os.path.join(SALIDA, 'BrazosFPS.fbx'), use_selection=True, object_types={'ARMATURE', 'MESH'},
                         apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False,
                         axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE', use_mesh_modifiers=False,
                         add_leaf_bones=False, use_armature_deform_only=False, primary_bone_axis='Y', secondary_bone_axis='X',
                         bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                         bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(FOTOS, 'brazos_rig.blend'))

# Fotos de revisión: cada puño con un mango, de varios lados.
libreria.conectar_texturas()
cam = libreria.camara_y_luz()
palo_me = bpy.data.meshes.new('Palo'); bmp = bmesh.new()
bmesh.ops.create_cone(bmp, cap_ends=True, segments=24, radius1=MANGO_R, radius2=MANGO_R, depth=0.3)
bmp.to_mesh(palo_me); bmp.free()
palo = bpy.data.objects.new('Palo', palo_me); bpy.context.scene.collection.objects.link(palo)
rojo = bpy.data.materials.new('Palo'); rojo.use_nodes = True
rojo.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.42, 0.04, 0.03, 1)
palo_me.materials.append(rojo)
br = brazos['R']
for pose, variante, nombres, juego in (('Puno', 'buzo', ('Manga_Buzo_R', 'Guante_Trabajo_R', 'Palo'), mangos),
                                       ('Puno_Grueso', 'campera', ('Manga_Campera_R', 'Guante_Grueso_R', 'Palo'), mangos_gruesos)):
    arm.animation_data.action = bpy.data.actions['Poses']
    bpy.context.scene.frame_set(CUADROS[pose])
    m = juego['R']
    palo.location = m.C
    palo.rotation_mode = 'QUATERNION'; palo.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(m.H)
    for vista, desde in (('frente', m.C + br.N * 0.22 + br.A * 0.05 + Vector((0, 0, 0.05))),
                         ('arriba', m.C - br.A * 0.2 + br.N * 0.05 + Vector((0, 0, 0.1))),
                         ('pulgar', m.C + m.H * 0.25 + br.N * 0.04),
                         ('dorso', m.C - br.N * 0.22 + Vector((0, 0, 0.06)))):
        libreria.foto(cam, os.path.join(FOTOS, 'puno_%s_%s.png' % (variante, vista)), m.C, desde, 50, nombres)
arm.animation_data.action = bpy.data.actions['Poses']; bpy.context.scene.frame_set(CUADROS['Abierta'])
libreria.foto(cam, os.path.join(FOTOS, 'abierta_buzo.png'), br.W + br.A * 0.08, br.W + br.A * 0.08 + br.N * 0.25 + Vector((0, 0, 0.08)), 50, ('Manga_Buzo_R', 'Guante_Trabajo_R'))
print('LISTO')
