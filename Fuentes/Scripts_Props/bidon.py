import sys, math; sys.path.insert(0, '/home/claude/skill_new/scripts'); from kit import *
from mathutils import Vector
COLOR = sys.argv[1] if len(sys.argv) > 1 else 'roja'
reset()
bm = nuevo_bm()
W, D, H = 0.345, 0.165, 0.445                                                    # bidón de 20 l (jerrycan)
caja(bm, (0, 0, H / 2), (W, D, H), mat=0)                                        # cuerpo
for y in (-D / 2, D / 2):                                                        # la X de refuerzo en las dos caras grandes
    viga(bm, (-0.135, y, 0.045), (0.135, y, 0.40), 0.008, 0.026, mat=0)
    viga(bm, (0.135, y, 0.045), (-0.135, y, 0.40), 0.008, 0.026, mat=0)
for x in (-W / 2, W / 2):                                                        # costura soldada del contorno
    viga(bm, (x, 0, 0.012), (x, 0, H - 0.012), 0.014, 0.008, mat=0)
viga(bm, (-W / 2, 0, 0.004), (W / 2, 0, 0.004), 0.014, 0.008, mat=0)
for x in (-0.115, -0.055, 0.005):                                                # tres manijas
    tubo(bm, [(x, -0.062, H - 0.004), (x, -0.050, H + 0.026), (x, 0.050, H + 0.026), (x, 0.062, H - 0.004)], 0.0085, seg=6, mat=2)
cilindro(bm, (0.112, 0, H - 0.01), (0.138, 0, H + 0.035), 0.021, seg=12, mat=1)  # pico
cilindro(bm, (0.138, 0, H + 0.035), (0.146, 0, H + 0.049), 0.026, seg=12, mat=1) # tapa
caja(bm, (0.105, 0, H + 0.045), (0.05, 0.016, 0.008), mat=2)                     # traba de la tapa
bid = terminar('Bidon_Combustible_LOD0', bm, [f'pintura_{COLOR}', 'acero_oscuro', 'metal_gastado'],
               bisel=0.006, bisel_min=0.05, apoyado=False, angulo=50)
pivote(bid, 'punto', (-0.055, 0, H + 0.026))                                     # agarre: manija del medio
nombre = 'Bidon_Combustible_' + {'roja': 'Rojo', 'verde': 'Verde'}[COLOR]
bid.name = nombre + '_LOD0'
for o in list(bpy.data.objects):
    if o.name.startswith('Bidon_Combustible_LOD'):
        o.name = o.name.replace('Bidon_Combustible_', nombre + '_')
bid['lods'] = [n.replace('Bidon_Combustible_', nombre + '_') for n in bid['lods']]
agrupar(nombre, [bid])
reporte([bid], categoria='A')
preview([bid], f'/home/claude/props/prev/bidon_{COLOR}.png', vistas=('34', 'cerca'))
exportar([bid], nombre, f'/mnt/user-data/outputs/{nombre}', estatico=False)
