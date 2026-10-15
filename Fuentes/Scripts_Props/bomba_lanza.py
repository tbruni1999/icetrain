import sys, math; sys.path.insert(0, '/home/claude/skill_new/scripts'); from kit import *
from mathutils import Vector
reset()
MATS = ['acero_oscuro', 'pintura_ocre', 'pintura_gris_maquinaria', 'goma']
bm = nuevo_bm()
# Bomba eléctrica de lanza: la lanza se clava en el suelo (z=0 = nivel del suelo) hasta el depósito o la napa
tubo(bm, [(0, 0, -0.62), (0, 0, -0.50)], [0.003, 0.022], seg=10, mat=0)          # punta de la lanza
cilindro(bm, (0, 0, -0.505), (0, 0, 0.572), 0.022, seg=10, mat=0)                   # lanza (caño de succión)
for z in (-0.20, 0.10):                                                           # uniones roscadas de los tramos
    cilindro(bm, (0, 0, z), (0, 0, z + 0.035), 0.027, seg=10, mat=0)
cilindro(bm, (0, 0, 0.30), (0, 0, 0.36), 0.040, seg=12, mat=1)                    # collar del trípode
for k in range(3):                                                                # trípode con estacas
    a = math.radians(90 + 120 * k); c = Vector((math.cos(a), math.sin(a), 0))
    viga(bm, c * 0.035 + Vector((0, 0, 0.33)), c * 0.36 + Vector((0, 0, 0.025)), 0.026, mat=1)
    caja(bm, c * 0.37 + Vector((0, 0, 0.012)), (0.07, 0.07, 0.012), mat=1)        # pie
    tubo(bm, [c * 0.37 + Vector((0, 0, -0.16)), c * 0.37 + Vector((0, 0, 0.03))], [0.0035, 0.009], seg=6, mat=0)  # estaca
# placa y unidad motor-bomba arriba de la lanza
caja(bm, (0.02, 0, 0.575), (0.30, 0.13, 0.02), mat=1)                              # placa de montaje
cilindro(bm, (-0.11, 0, 0.66), (0.10, 0, 0.66), 0.062, seg=16, mat=2)             # motor eléctrico
for x in (-0.085, -0.055, -0.025, 0.005):                                         # aletas de enfriamiento
    cilindro(bm, (x, 0, 0.66), (x + 0.008, 0, 0.66), 0.069, seg=16, mat=2)
cilindro(bm, (-0.125, 0, 0.66), (-0.105, 0, 0.66), 0.050, seg=16, mat=0)           # tapa trasera del motor
cilindro(bm, (0.095, 0, 0.65), (0.165, 0, 0.65), 0.080, seg=16, mat=2)             # carcasa de la bomba (voluta)
caja(bm, (-0.01, 0, 0.60), (0.16, 0.10, 0.04), mat=2)                              # base del motor
tubo(bm, [(0.13, 0, 0.73), (0.13, 0, 0.80), (0.20, 0, 0.83), (0.27, 0, 0.83)], 0.020, seg=10, mat=0)   # salida
cilindro(bm, (0.27, 0, 0.83), (0.33, 0, 0.83), 0.024, seg=10, mat=3)              # espiga de la manguera
tubo(bm, [(-0.15, -0.075, 0.60), (-0.15, -0.075, 0.76), (0.14, -0.075, 0.76), (0.14, -0.075, 0.60)], 0.010, seg=8, mat=0)  # manija
# batería colgada del trípode y cable al motor
caja(bm, (-0.05, 0.155, 0.43), (0.20, 0.11, 0.15), mat=0)                          # batería
for x in (-0.11, 0.01):
    cilindro(bm, (x, 0.155, 0.505), (x, 0.155, 0.525), 0.010, seg=8, mat=3)       # bornes
tubo(bm, [(0.01, 0.155, 0.52), (0.02, 0.16, 0.58), (0.02, 0.085, 0.665), (0.0, 0.03, 0.665)], 0.006, seg=6, mat=3)  # cable
caja(bm, (-0.05, 0.10, 0.43), (0.21, 0.01, 0.03), mat=1)                           # abrazadera de la batería
bomba = terminar('Bomba_Lanza_LOD0', bm, MATS, bisel=0, apoyado=False, angulo=50)
pivote(bomba, 'punto', (0, 0, 0))                                                 # pivote = donde la lanza entra al suelo
agrupar('Bomba_Lanza', [bomba])
reporte([bomba], categoria='B')
preview([bomba], '/home/claude/props/prev/lanza.png', vistas=('34', 'cerca'), suelo_nieve=False)
exportar([bomba], 'Bomba_Lanza', '/mnt/user-data/outputs/Bomba_Lanza', estatico=False)
