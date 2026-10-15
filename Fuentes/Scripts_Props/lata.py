import sys, math; sys.path.insert(0, '/home/claude/skill_new/scripts'); from kit import *
from mathutils import Vector

def toro(bm, z, R, r, seg=32, sseg=5, mat=0):
    """Pestaña enrollada de la lata (anillo cerrado)."""
    an = []
    for i in range(seg):
        a = 2 * math.pi * i / seg; c = Vector((math.cos(a), math.sin(a), 0))
        an.append([bm.verts.new(c * (R + r * math.cos(2 * math.pi * k / sseg)) + Vector((0, 0, z + r * math.sin(2 * math.pi * k / sseg)))) for k in range(sseg)])
    for i in range(seg):
        A, B = an[i], an[(i + 1) % seg]
        for k in range(sseg):
            bm.faces.new([A[k], B[k], B[(k + 1) % sseg], A[(k + 1) % sseg]]).material_index = mat

def arandela(bm, centro, rx, ry, ix, iy, z0, esp, seg=10, mat=0):
    """Anillo plano (elipse con agujero) apoyado en z0: el anillo para abrir."""
    cx, cy = centro; anillos = []
    for z in (z0, z0 + esp):
        o = [bm.verts.new((cx + rx * math.cos(2 * math.pi * k / seg), cy + ry * math.sin(2 * math.pi * k / seg), z)) for k in range(seg)]
        i = [bm.verts.new((cx + ix * math.cos(2 * math.pi * k / seg), cy + iy * math.sin(2 * math.pi * k / seg), z)) for k in range(seg)]
        anillos.append((o, i))
    (ob, ib), (ot, it) = anillos
    for k in range(seg):
        n = (k + 1) % seg
        bm.faces.new([ot[k], ot[n], it[n], it[k]]).material_index = mat          # cara de arriba
        bm.faces.new([ob[k], ib[k], ib[n], ob[n]]).material_index = mat          # cara de abajo
        bm.faces.new([ob[k], ob[n], ot[n], ot[k]]).material_index = mat          # canto exterior
        bm.faces.new([ib[k], it[k], it[n], ib[n]]).material_index = mat          # canto interior

reset()
bm = nuevo_bm()
H = 0.110; Rc = 0.0415; rr = 0.0021
cilindro(bm, (0, 0, 0.0040), (0, 0, H - 0.0045), Rc, seg=32, mat=0)          # cuerpo, tapas hundidas 3 mm
toro(bm, H - rr - 0.0002, Rc + 0.0006, rr, mat=0)                             # borde superior
toro(bm, rr, Rc + 0.0006, rr, sseg=4, mat=0)                                          # borde inferior (apoya)
zt = H - 0.0045 + 0.0002                                                      # anillo para abrir, acostado en la tapa
n0 = len(bm.verts)
arandela(bm, (0, -0.0135), 0.0135, 0.0105, 0.0090, 0.0060, zt, 0.0010, seg=9)
bm.verts.ensure_lookup_table()
for v in bm.verts[n0:]:                                                       # levantado 8° desde la lengüeta: se lee de lejos
    if v.co.y < -0.004:
        v.co.z += (-0.004 - v.co.y) * math.tan(math.radians(8))
caja(bm, (0, 0.0002, zt + 0.0005), (0.0085, 0.0140, 0.0010), mat=0)         # lengüeta hasta el remache
cilindro(bm, (0, 0.0055, zt), (0, 0.0055, zt + 0.0018), 0.0030, seg=6, mat=0) # remache
cilindro(bm, (0, 0, 0.0115), (0, 0, 0.0985), Rc + 0.0007, seg=32, mat=1)     # etiqueta
cilindro(bm, (0, 0, 0.044), (0, 0, 0.066), Rc + 0.0010, seg=32, mat=2)       # franja impresa
# abolladura chica en el borde de arriba (golpe de una caída): se hunde hacia adentro y un poco hacia abajo
t0 = math.radians(-60); ancho = math.radians(38)
for v in bm.verts:
    r = math.hypot(v.co.x, v.co.y)
    if r < 0.02:
        continue
    da = math.atan2(math.sin(math.atan2(v.co.y, v.co.x) - t0), math.cos(math.atan2(v.co.y, v.co.x) - t0))
    if abs(da) > ancho:
        continue
    f = (0.5 + 0.5 * math.cos(math.pi * da / ancho)) * min(1, max(0, (v.co.z - 0.0115) / 0.087))   # lineal en altura: etiqueta y franja se hunden juntas
    k = 1 - 0.0032 * f / r
    v.co.x *= k; v.co.y *= k; v.co.z -= 0.0012 * f
lata = terminar('Lata_Conserva_LOD0', bm, ['hojalata', 'etiqueta_lata_acanalada', 'franja_lata_acanalada'],
                bisel=0, angulo=65, apoyado=False, bisel_min=0.008)
l1 = bpy.data.objects['Lata_Conserva_LOD1']                                   # LOD1 a la mitad (no hay piezas chicas que sacar)
d = l1.modifiers.new('dec', 'DECIMATE'); d.ratio = 0.5; aplicar_mods(l1); uv_caja(l1.data)
pivote(lata, 'centro')
raiz = agrupar('Lata_Conserva', [lata])
r = reporte([lata], categoria='A')
preview([lata], '/home/claude/props/prev/lata.png', vistas=('34', 'arriba'))
chk = []
for i, n in enumerate(('Lata_Conserva_LOD1', 'Lata_Conserva_LOD2')):
    c = bpy.data.objects[n].copy(); c.data = bpy.data.objects[n].data.copy(); c.name = f'chk{i}'; c.parent = None
    c.location = lata.matrix_world.translation + Vector((0.11 * (i + 1), 0, 0)); bpy.context.collection.objects.link(c); chk.append(c)
preview([lata] + chk, '/home/claude/props/prev/lods.png', vistas=('frente',), muestras=12)
for c in chk: bpy.data.objects.remove(c, do_unlink=True)
exportar([lata], 'Lata_Conserva', '/mnt/user-data/outputs/Lata_Conserva', estatico=False)
