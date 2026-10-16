# Brazos y manos en primera persona (Claude, 2026-10-16; provisorios hasta el arte final del chat de arte).
# Uso: blender -b -P Fuentes/Scripts_Manos/manos.py -- <carpeta_salida_fbx> <carpeta_fotos>
# Brazo DERECHO (el izquierdo es el mismo espejado en Unity). Piezas rígidas que el juego acomoda solo (sin esqueleto):
#   Brazo_*      : del hombro (origen) al codo, a lo largo de +Z, 0,29 m.
#   Antebrazo_*  : del codo (origen) a la muñeca, a lo largo de +Z, 0,26 m.
#   Mano_*       : la muñeca en el origen, los dedos hacia +Z, el dorso hacia +Y y la palma hacia -Y; el pulgar del lado -X.
#                  "Puno": cerrada alrededor de un mango que cruza la palma a lo largo de X (vacío "Agarre" en su centro).
#                  "Abierta": para agarrar cajas (vacío "Agarre" en el centro de la palma).
# Variantes (decisión de Tomi del 2026-10-16: los brazos según el traje): con el traje, campera de trabajo y guante grueso;
# sin el traje, buzo de lana y guante de trabajo.
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kit_manos import *

args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else ['.', '.']
salida, fotos = args[0], args[1]
os.makedirs(salida, exist_ok=True)
os.makedirs(fotos, exist_ok=True)

COLORES = {'MT_LonaMarron': (0.30, 0.20, 0.11, 1), 'MT_TelaManta': (0.32, 0.30, 0.27, 1), 'MT_TelaGris': (0.22, 0.22, 0.23, 1),
           'MT_Goma': (0.06, 0.06, 0.07, 1), 'MT_Cuero': (0.48, 0.33, 0.18, 1), 'MT_FranjasSeguridad': (0.8, 0.6, 0.1, 1),
           'MT_MetalGastado': (0.42, 0.42, 0.42, 1), 'MT_PinturaRoja': (0.45, 0.06, 0.04, 1)}

LARGO_BRAZO, LARGO_ANTEBRAZO = 0.29, 0.26
MANGO_R = 0.0135   # radio del mango que agarra el puño (la barreta, el martillo)


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


def punta(pts, radios):
    """Redondea la punta de un dedo: sigue un poco en la misma dirección y se cierra."""
    a, b = Vector(pts[-2]), Vector(pts[-1])
    d = (b - a).normalized()
    r = radios[-1]
    for avance, escala in ((0.45, 0.85), (0.8, 0.55), (0.98, 0.2)):
        pts.append(tuple(b + d * r * avance))
        radios.append(r * escala)


def mano(nombre, mat_guante, mat_puno, grueso, cerrada):
    bm = nuevo_bm()
    g = 1.18 if grueso else 1.0                         # el guante grueso abulta
    # Puño del guante (sobre la muñeca) y la palma.
    puno = [(0, 0, -0.07 if grueso else -0.035), (0, 0, -0.01), (0, 0, 0.0)]
    barrido(bm, puno, [0.047 * g if grueso else 0.037, 0.04 * g, 0.036 * g], lados=14, mat=1,
            escala_x=[1.0] * 3, escala_y=[0.78] * 3, normal_inicial=(1, 0, 0))
    palma = [(0, 0, 0.0), (0, -0.002, 0.03), (0, -0.004, 0.065), (0, -0.004, 0.092)]
    barrido(bm, palma, [0.036 * g, 0.043 * g, 0.046 * g, 0.044 * g], lados=14, mat=0,
            escala_x=[1.0] * 4, escala_y=[0.45, 0.42, 0.40, 0.38], normal_inicial=(1, 0, 0))
    rd = 0.0095 * g                                     # radio de un dedo con guante
    G = Vector((0, -0.036 * g, 0.096))                  # el centro del mango que agarra el puño
    rc = MANGO_R + rd                                   # los dedos van alrededor del mango
    dedos = [(-0.031, 1.0), (-0.0105, 1.05), (0.0105, 1.0), (0.031, 0.82)]   # índice (lado del pulgar) a meñique; largo relativo
    for x, largo in dedos:
        x *= g
        pts = [(x, -0.004 * g, 0.088)]
        if cerrada:
            for k in range(12):
                fi = math.radians(-5 + 235 * largo * k / 11)
                pts.append((x, G.y + rc * math.cos(fi), G.z + rc * math.sin(fi)))
        else:
            for k in range(1, 9):
                u = k / 8
                curva = math.radians(22) * u * u
                pts.append((x, -0.004 * g - 0.075 * largo * math.sin(curva) * u, 0.092 + 0.075 * largo * u))
        r = [rd * (1.0 - 0.18 * i / (len(pts) - 1)) for i in range(len(pts))]
        punta(pts, r)
        barrido(bm, pts, r, lados=8, mat=0, normal_inicial=(1, 0, 0))
    # Pulgar: sale del costado de la palma y cruza por delante de los dedos (cerrada) o va al costado (abierta).
    if cerrada:
        pulgar = [(-0.04 * g, -0.012, 0.025), (-0.046 * g, -0.034 * g, 0.05), (-0.03 * g, G.y - rc * 0.55, G.z - 0.008),
                  (-0.006 * g, G.y - rc * 0.85, G.z + 0.006), (0.012 * g, G.y - rc * 0.8, G.z + 0.012)]
    else:
        pulgar = [(-0.04 * g, -0.012, 0.025), (-0.055 * g, -0.03 * g, 0.05), (-0.062 * g, -0.04 * g, 0.075), (-0.064 * g, -0.044 * g, 0.095)]
    rp = [0.0125 * g, 0.011 * g, 0.0105 * g, 0.0095 * g, 0.009 * g][:len(pulgar)]
    punta(pulgar, rp)
    barrido(bm, pulgar, rp, lados=8, mat=0, normal_inicial=(1, 0, 0))
    ob = terminar(nombre, bm, [mat_guante, mat_puno], angulo=55, uv_tam=0.15)
    vacio('Agarre', tuple(G) if cerrada else (0, -0.035 * g, 0.075), ob)
    return ob


def armar():
    reset()
    piezas = []
    # Con el traje: campera de trabajo acolchada, con una franja reflectiva cerca del puño, y guante grueso de goma.
    piezas.append(manga('Brazo_Campera', LARGO_BRAZO, 0.072, 0.066, 'MT_LonaMarron', 'MT_LonaMarron', True, 0.0))
    piezas.append(manga('Antebrazo_Campera', LARGO_ANTEBRAZO, 0.066, 0.056, 'MT_LonaMarron', 'MT_TelaGris', True, 0.035))
    piezas.append(mano('Mano_Grueso_Puno', 'MT_Goma', 'MT_TelaGris', True, True))
    piezas.append(mano('Mano_Grueso_Abierta', 'MT_Goma', 'MT_TelaGris', True, False))
    # Sin el traje: buzo de lana y guante de trabajo de cuero.
    piezas.append(manga('Brazo_Buzo', LARGO_BRAZO, 0.052, 0.047, 'MT_TelaManta', 'MT_TelaManta', False, 0.0))
    piezas.append(manga('Antebrazo_Buzo', LARGO_ANTEBRAZO, 0.047, 0.037, 'MT_TelaManta', 'MT_TelaGris', False, 0.045))
    piezas.append(mano('Mano_Trabajo_Puno', 'MT_Cuero', 'MT_TelaGris', False, True))
    piezas.append(mano('Mano_Trabajo_Abierta', 'MT_Cuero', 'MT_TelaGris', False, False))
    # Separadas en fila para exportar (en Unity cada pieza se usa sola, en su propio origen).
    for i, p in enumerate(piezas):
        p.location = (0, 0, 0)
    exportar(os.path.join(salida, 'Manos.fbx'))

    # Fotos de revisión: un brazo armado agarrando un palo, con cada variante.
    for variante, (brazo, ante, mano_c) in {'campera': ('Brazo_Campera', 'Antebrazo_Campera', 'Mano_Grueso_Puno'),
                                             'buzo': ('Brazo_Buzo', 'Antebrazo_Buzo', 'Mano_Trabajo_Puno')}.items():
        for p in piezas:
            p.hide_render = p.name not in (brazo, ante, mano_c)
        b = bpy.data.objects[brazo]; a = bpy.data.objects[ante]; m = bpy.data.objects[mano_c]
        b.location = (0, 0, 0); b.rotation_euler = (math.radians(-60), 0, 0)
        codo = b.matrix_world @ Vector((0, 0, LARGO_BRAZO))
        bpy.context.view_layer.update()
        codo = bpy.data.objects[brazo].matrix_world @ Vector((0, 0, LARGO_BRAZO))
        a.location = codo; a.rotation_euler = (math.radians(-100), 0, 0)
        bpy.context.view_layer.update()
        muneca = a.matrix_world @ Vector((0, 0, LARGO_ANTEBRAZO))
        m.location = muneca; m.rotation_euler = (math.radians(-100), 0, 0)
        bpy.context.view_layer.update()
        ag = m.matrix_world @ Vector((0, -0.036 * 1.18 if 'Grueso' in mano_c else -0.036, 0.096))
        palo = nuevo_bm()
        eje = m.matrix_world.to_3x3() @ Vector((1, 0, 0))
        barrido(palo, [tuple(ag - eje * 0.25), tuple(ag + eje * 0.25)], MANGO_R, lados=6, mat=0)
        po = terminar('Palo_' + variante, palo, ['MT_PinturaRoja'])
        foto(os.path.join(fotos, f'brazo_{variante}.png'), tuple(muneca), tuple(muneca + Vector((0.45, -0.45, 0.25))), lente=45,
             colores=COLORES)
        bpy.data.objects.remove(po, do_unlink=True)
        foto(os.path.join(fotos, f'mano_{variante}.png'), tuple(ag), tuple(ag + Vector((0.18, -0.2, 0.12))), lente=45,
             colores=COLORES)


armar()
print('MANOS LISTAS')
