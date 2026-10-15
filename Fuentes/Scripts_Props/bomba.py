import sys, math; sys.path.insert(0, '/home/claude/skill_new/scripts'); from kit import *
from mathutils import Vector
reset()
MATS = ['pintura_roja', 'acero_galvanizado', 'hierro_fundido', 'goma']
# --- cuerpo: bomba manual de palanca para tambores de 200 l (el caño entra por la boca del tambor)
bm = nuevo_bm()
cilindro(bm, (0, 0, 0.0), (0, 0, 0.80), 0.016, seg=10, mat=1)                   # caño de succión
cilindro(bm, (0, 0, 0.000), (0, 0, 0.012), 0.019, seg=10, mat=1)               # boquilla de abajo
cilindro(bm, (0, 0, 0.655), (0, 0, 0.685), 0.034, seg=6, mat=2)                 # tuerca hexagonal (rosca al tambor)
cilindro(bm, (0, 0, 0.685), (0, 0, 0.715), 0.027, seg=12, mat=2)                # rosca
cilindro(bm, (0, 0, 0.790), (0, 0, 0.808), 0.054, seg=16, mat=2)                # brida inferior
cilindro(bm, (0, 0, 0.805), (0, 0, 0.930), 0.048, seg=16, mat=0)                # cuerpo de la bomba
cilindro(bm, (0, 0, 0.927), (0, 0, 0.945), 0.054, seg=16, mat=2)                # tapa
caja(bm, (0.030, 0, 0.958), (0.034, 0.040, 0.028), mat=2)                       # soporte del eje de la palanca
tubo(bm, [(-0.040, 0, 0.885), (-0.085, 0, 0.885), (-0.110, 0, 0.865), (-0.118, 0, 0.815)], 0.012, seg=8, mat=1)  # pico de salida
cilindro(bm, (-0.118, 0, 0.815), (-0.118, 0, 0.785), 0.0145, seg=8, mat=2)    # espiga para la manguera
cuerpo = terminar('Bomba_Manual_LOD0', bm, MATS, bisel=0, apoyado=False, angulo=50)
pivote(cuerpo, 'punto', (0, 0, 0.86))                                           # agarre: el cuerpo
# --- palanca: objeto aparte, pivote en el eje para animarla en Unity
bm = nuevo_bm()
cilindro(bm, (0.030, -0.026, 0.962), (0.030, 0.026, 0.962), 0.006, seg=8, mat=2)  # eje
viga(bm, (0.022, 0, 0.962), (0.300, 0, 0.995), 0.012, 0.020, mat=2)             # brazo
cilindro(bm, (0.205, 0, 0.984), (0.315, 0, 0.997), 0.015, seg=10, mat=3)        # puño de goma
pal = terminar('Bomba_Manual_Palanca_LOD0', bm, MATS, bisel=0, apoyado=False, angulo=50)
pivote(pal, 'punto', (0.030, 0, 0.962))
agrupar('Bomba_Manual', [cuerpo, pal])
reporte([cuerpo, pal], categoria='A')
preview([cuerpo, pal], '/home/claude/props/prev/bomba.png', vistas=('34', 'cerca'))
exportar([cuerpo, pal], 'Bomba_Manual', '/mnt/user-data/outputs/Bomba_Manual', estatico=False)
