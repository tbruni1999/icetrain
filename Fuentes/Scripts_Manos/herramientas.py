# Barreta, martillo de herrero y llave combinada para la primera persona (Claude, 2026-10-16; provisorios hasta el arte final).
# Uso: blender -b -P Fuentes/Scripts_Manos/herramientas.py -- <carpeta_salida_fbx> <carpeta_fotos>
# Cada herramienta: el origen es donde la agarra la mano derecha; a lo largo de Z (en Unity, el eje Y del modelo); los vacíos
# "Agarre_Derecha" / "Agarre_Izquierda" marcan dónde van las manos.
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kit_manos import *
import bmesh as _bm

args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else ['.', '.']
salida, fotos = args[0], args[1]
os.makedirs(salida, exist_ok=True)
os.makedirs(fotos, exist_ok=True)

COLORES = {'MT_PinturaRoja': (0.45, 0.06, 0.04, 1), 'MT_MetalGastado': (0.42, 0.42, 0.42, 1),
           'MT_AceroOscuro': (0.12, 0.12, 0.13, 1), 'MT_Madera': (0.42, 0.27, 0.14, 1), 'MT_AceroGalvanizado': (0.6, 0.62, 0.64, 1)}


def triangular_ngons(bm):
    caras = [f for f in bm.faces if len(f.verts) > 4]
    if caras:
        _bm.ops.triangulate(bm, faces=caras, quad_method='BEAUTY', ngon_method='EAR_CLIP')


# ------------------------------------------------------------------ Barreta (75 cm, hierro hexagonal, cuello de ganso, uña)
def barreta():
    reset()
    bm = nuevo_bm()
    agarre = 0.08                                       # la mano derecha, cerca de la punta recta
    pts, sx, sy = [], [], []
    largo_recto = 0.62
    n = 32
    for i in range(n + 1):                              # tramo recto, con la punta de cincel aplastada
        z = largo_recto * i / n
        pts.append((0, 0, z))
        k = min(1.0, z / 0.045)
        sx.append(0.55 + 0.75 * k if z < 0.045 else 1.0 + 0.3 * max(0.0, (0.045 - z) / 0.045))
        sy.append(0.28 + 0.72 * k)
    R = 0.055
    for i in range(1, 15):                              # el cuello de ganso: 115° hacia +Y
        th = math.radians(115) * i / 14
        pts.append((0, R - R * math.cos(th), largo_recto + R * math.sin(th)))
        sx.append(1.0); sy.append(1.0)
    fin = Vector(pts[-1]); t = Vector((0, math.sin(math.radians(115)), math.cos(math.radians(115))))
    for i in range(1, 7):                               # la uña: se ensancha y se aplana
        u = i / 6
        pts.append(tuple(fin + t * (0.045 * u)))
        sx.append(1.0 + 0.6 * u); sy.append(1.0 - 0.62 * u)
    pintado = [0.10 <= p[2] <= 0.56 and p[1] < 0.001 for p in pts]
    barrido(bm, pts, 0.0125, lados=6, mat=lambda i: 0 if pintado[i] else 1, escala_x=sx, escala_y=sy,
            giro=math.radians(30), normal_inicial=(1, 0, 0))
    # Las dos puntas de la uña, abiertas en V.
    base = fin + t * 0.045
    for lado in (-1, 1):
        prong = []
        for i in range(6):
            u = i / 5
            prong.append(tuple(base + Vector((lado * (0.006 + 0.004 * u), 0, 0)) + t * (0.028 * u) + Vector((0, 0, -0.006 * u))))
        barrido(bm, prong, [0.0085 * (1 - 0.55 * i / 5) for i in range(6)], lados=6, mat=1,
                escala_x=[0.75] * 6, escala_y=[0.38] * 6, normal_inicial=(1, 0, 0))
    bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, -agarre))
    ob = terminar('Barreta', bm, ['MT_PinturaRoja', 'MT_MetalGastado'], angulo=35, uv_tam=0.3)
    vacio('Agarre_Derecha', (0, 0, 0), ob)
    vacio('Agarre_Izquierda', (0, 0, 0.24), ob)
    vacio('Punta', tuple(base + t * 0.028 - Vector((0, 0, agarre))), ob)
    exportar(os.path.join(salida, 'Barreta.fbx'))
    foto(os.path.join(fotos, 'barreta.png'), (0, 0.03, 0.28), (1.05, -0.75, 0.62), lente=40, colores=COLORES)
    foto(os.path.join(fotos, 'barreta_una.png'), (0, 0.06, 0.6), (0.22, -0.16, 0.66), lente=40, colores=COLORES)


# ------------------------------------------------------------------ Martillo de herrero (pena cruzada, ~1 kg, 33 cm)
def martillo():
    reset()
    bm = nuevo_bm()
    agarre = 0.06
    pts, rad = [], []
    for i in range(21):
        z = 0.33 * i / 20
        pts.append((0, 0, z))
        r = 0.0135
        if z < 0.03: r = 0.0150 - 0.0015 * z / 0.03       # ensanche del extremo
        elif z > 0.22: r = 0.0135 - 0.0028 * min(1, (z - 0.22) / 0.06)  # cuello que entra en la cabeza
        rad.append(r)
    barrido(bm, pts, rad, lados=12, mat=0, escala_x=[1.0] * 21, escala_y=[0.72] * 21, normal_inicial=(1, 0, 0))
    hz = 0.318
    cara = [(0, y, hz) for y in [0.0, 0.015, 0.03, 0.045, 0.055, 0.062]]
    barrido(bm, cara, [0.0195, 0.0195, 0.0195, 0.0195, 0.0185, 0.0165], lados=8, mat=1, giro=math.radians(22.5),
            normal_inicial=(0, 0, 1))
    pena = [(0, -y, hz) for y in [0.0, 0.015, 0.03, 0.042, 0.052]]
    barrido(bm, pena, [0.0195, 0.0185, 0.017, 0.0155, 0.0145], lados=8, mat=1, giro=math.radians(22.5),
            escala_x=[1.0, 0.85, 0.55, 0.3, 0.14], escala_y=[1.0, 1.0, 1.02, 1.05, 1.08], normal_inicial=(0, 0, 1))
    bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, -agarre))
    ob = terminar('Martillo', bm, ['MT_Madera', 'MT_AceroOscuro'], angulo=40, uv_tam=0.25)
    vacio('Agarre_Derecha', (0, 0, 0), ob)
    exportar(os.path.join(salida, 'Martillo.fbx'))
    foto(os.path.join(fotos, 'martillo.png'), (0, 0, 0.16), (0.55, -0.42, 0.34), lente=40, colores=COLORES)


# ------------------------------------------------------------------ Llave combinada (boca abierta y estrella, 28 cm)
def llave():
    reset()
    bm = nuevo_bm()
    agarre = 0.10
    cuerpo = [(0, 0, 0.035 + 0.21 * i / 12) for i in range(13)]
    barrido(bm, cuerpo, 0.011, lados=12, mat=0, escala_x=[1.0] * 13, escala_y=[0.36] * 13, normal_inicial=(1, 0, 0))
    # Estrella (arriba): un anillo cerrado en el plano X-Z.
    anillo = []
    for i in range(25):
        a = 2 * math.pi * i / 24
        anillo.append((0.018 * math.sin(a), 0, 0.262 + 0.018 * math.cos(a)))
    barrido(bm, anillo, 0.0068, lados=8, mat=0, escala_x=[0.62] * 25, escala_y=[1.0] * 25, tapas=False, normal_inicial=(0, 1, 0))
    # Boca abierta (abajo): un arco grueso abierto hacia la punta (en el plano X-Z), como la estrella pero cortado.
    cz, rm = 0.018, 0.0158
    boca = []
    for i in range(17):
        a = math.radians(48 + 264 * i / 16)
        boca.append((rm * math.sin(a), 0, cz - rm * math.cos(a)))
    barrido(bm, boca, 0.0068, lados=8, mat=0, escala_x=[0.62] * 17, escala_y=[1.0] * 17, normal_inicial=(0, 1, 0))
    triangular_ngons(bm)
    bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, -agarre))
    ob = terminar('Llave', bm, ['MT_AceroGalvanizado'], angulo=40, uv_tam=0.2)
    vacio('Agarre_Derecha', (0, 0, 0), ob)
    exportar(os.path.join(salida, 'Llave.fbx'))
    foto(os.path.join(fotos, 'llave.png'), (0, 0, 0.05), (0.42, -0.32, 0.22), lente=40, colores=COLORES)
    foto(os.path.join(fotos, 'llave_boca.png'), (0, 0, -0.08), (0.05, -0.16, -0.06), lente=40, colores=COLORES)


barreta()
martillo()
llave()
print('HERRAMIENTAS LISTAS')
