# Hacha y pico improvisados para la primera persona (G1, Claude, 2026-10-17; provisorios hasta el arte final): una piedra atada
# a un palo con tiras de tela, como las primeras herramientas de 7 Days (nivel 1 de la lista del grindeo, sección 4).
# Uso: blender -b -P Fuentes/Scripts_Manos/hacha_pico.py -- <carpeta_salida_fbx> <carpeta_fotos>
# Convención (la misma que herramientas.py): el origen es donde la agarra la mano derecha; el mango va a lo largo de Z (en
# Unity, el eje Y del modelo); lo que golpea mira hacia +Y; el vacío "Agarre_Derecha" marca la mano.
import sys, os, math, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kit_manos import *

args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else ['.', '.']
salida, fotos = args[0], args[1]
os.makedirs(salida, exist_ok=True)
os.makedirs(fotos, exist_ok=True)

COLORES = {'MT_MaderaIntemperie': (0.36, 0.3, 0.22, 1), 'MT_RocaGris': (0.42, 0.42, 0.41, 1), 'MT_Lona': (0.48, 0.42, 0.32, 1)}
MATERIALES = ['MT_MaderaIntemperie', 'MT_RocaGris', 'MT_Lona']


def palo(bm, largo, radio, semilla):
    """Un palo apenas torcido y desparejo (no torneado): la madera de la herramienta improvisada."""
    rnd = random.Random(semilla)
    pts, rad = [], []
    for i in range(25):
        z = largo * i / 24
        pts.append((0.005 * math.sin(z * 5.1 + semilla), 0.004 * math.sin(z * 3.7 + 1.3 * semilla), z))
        r = radio * (1.0 + rnd.uniform(-0.07, 0.07))
        if z < 0.015: r *= 0.9                          # la punta de abajo, gastada
        rad.append(r)
    barrido(bm, pts, rad, lados=9, mat=0, normal_inicial=(1, 0, 0))


def atadura(bm, z, radio, alto=0.012):
    """Una vuelta de tira de tela alrededor del palo."""
    barrido(bm, [(0, 0, z - alto / 2), (0, 0, z + alto / 2)], radio, lados=10, mat=2, normal_inicial=(1, 0, 0))


def cruces(bm, z, ancho, alto):
    """Las tiras cruzadas que sujetan la piedra al palo (dos lazos en X, por delante y por detrás)."""
    for s in (-1, 1):
        caja(bm, (0, s * 0.012, z), (ancho, 0.006, alto), mat=2, rot=Matrix.Rotation(math.radians(s * 32), 3, 'Y'))


# ------------------------------------------------------------------ Hacha improvisada (55 cm, piedra en cuña atada)
def hacha():
    reset()
    bm = nuevo_bm()
    agarre = 0.08
    palo(bm, 0.5, 0.016, 1)
    hz = 0.465
    # La piedra: una cuña chata que se afila hacia +Y (el filo) y se ensancha hacia arriba y abajo.
    filo = [(0, y, hz) for y in [-0.04, -0.015, 0.015, 0.045, 0.075, 0.1, 0.112]]
    barrido(bm, filo, [0.022, 0.028, 0.034, 0.037, 0.036, 0.032, 0.028], lados=7, mat=1,
            escala_x=[0.62, 0.55, 0.45, 0.34, 0.24, 0.14, 0.07], escala_y=[0.8, 0.95, 1.15, 1.4, 1.65, 1.85, 1.9], normal_inicial=(1, 0, 0))
    for z in (hz - 0.042, hz - 0.028, hz + 0.03, hz + 0.044):
        atadura(bm, z, 0.0185)
    cruces(bm, hz, 0.036, 0.075)
    bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, -agarre))
    ob = terminar('Hacha', bm, MATERIALES, angulo=40, uv_tam=0.25)
    vacio('Agarre_Derecha', (0, 0, 0), ob)
    exportar(os.path.join(salida, 'Hacha.fbx'))
    foto(os.path.join(fotos, "hacha.png"), (0, 0.02, 0.18), (0.8, -0.55, 0.45), lente=40, colores=COLORES)
    foto(os.path.join(fotos, "hacha_lado.png"), (0, 0.03, 0.36), (0.75, 0.03, 0.4), lente=60, colores=COLORES)
    foto(os.path.join(fotos, "hacha_arriba.png"), (0, 0.03, 0.38), (0.02, 0.03, 0.95), lente=60, colores=COLORES)


# ------------------------------------------------------------------ Pico improvisado (60 cm, piedra alargada con punta)
def pico():
    reset()
    bm = nuevo_bm()
    agarre = 0.08
    palo(bm, 0.565, 0.017, 2)
    hz = 0.53
    # La piedra: alargada, atravesada al palo, apenas curva hacia abajo en las puntas; la de +Y es la que pega (más afilada).
    ys = [-0.13, -0.09, -0.05, -0.015, 0.02, 0.06, 0.11, 0.155]
    cabeza = [(0, y, hz - 0.022 * (y / 0.15) ** 2) for y in ys]
    barrido(bm, cabeza, [0.007, 0.014, 0.02, 0.024, 0.024, 0.02, 0.012, 0.004], lados=7, mat=1,
            escala_x=[0.8] * 8, escala_y=[0.85] * 8, normal_inicial=(1, 0, 0))
    for z in (hz - 0.045, hz - 0.031, hz + 0.031, hz + 0.045):
        atadura(bm, z, 0.0195)
    cruces(bm, hz, 0.04, 0.08)
    bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, -agarre))
    ob = terminar('Pico', bm, MATERIALES, angulo=40, uv_tam=0.25)
    vacio('Agarre_Derecha', (0, 0, 0), ob)
    exportar(os.path.join(salida, 'Pico.fbx'))
    foto(os.path.join(fotos, "pico.png"), (0, 0.02, 0.2), (0.85, -0.58, 0.5), lente=40, colores=COLORES)


hacha()
pico()
print('HACHA Y PICO LISTOS')
