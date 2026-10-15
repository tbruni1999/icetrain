# Pulido de mallas (producción P4, pedido de Tomi: "mega pulido de los assets, que la malla quede impecable").
# Abre un .blend, limpia cada malla y exporta un FBX pulido (y guarda el .blend pulido aparte). Los originales no se tocan.
#   blender.exe -b <original.blend> --python pulir.py -- <salida.fbx> <salida.blend> <informe.txt>
# Qué hace en cada malla:
# - suelda los vértices que están en el mismo lugar (las caras venían sueltas, cada una con sus vértices: quedaban uniones
#   abiertas por donde se cuela la luz y bordes que no se tocan);
# - saca las caras sin área y los vértices sueltos;
# No cambia el sombreado del artista: las normales hechas a mano se conservan, los bordes que el artista marcó duros siguen
# duros, y los bordes que estaban separados (y por eso se veían duros) se marcan duros antes de soldar, así se ven igual.
# (La primera versión ponía sombreado por ángulo de 30° y facetaba los carámbanos y la costra de nieve, que son lisos a propósito.)
# No da vuelta normales en automático (el interior del vagón mira hacia adentro a propósito).
import sys
import bpy
import bmesh

args = sys.argv[sys.argv.index("--") + 1:]
out_fbx, out_blend, out_report = args[0], args[1], args[2]
MERGE = 0.0001        # 0,1 mm

lines = ["objeto | vértices antes → después | bordes abiertos antes → después | caras sin área quitadas"]
for obj in sorted([o for o in bpy.data.objects if o.type == 'MESH'], key=lambda o: o.name):
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    v0 = len(bm.verts)
    b0 = sum(1 for e in bm.edges if e.is_boundary)
    # Lo que estaba separado se veía duro: queda duro después de soldar.
    for e in bm.edges:
        if e.is_boundary:
            e.smooth = False
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=MERGE)
    degenerate = [f for f in bm.faces if f.calc_area() < 1e-10]
    removed = len(degenerate)
    if degenerate:
        bmesh.ops.delete(bm, geom=degenerate, context='FACES_ONLY')
    loose = [v for v in bm.verts if not v.link_faces]
    if loose:
        bmesh.ops.delete(bm, geom=loose, context='VERTS')
    v1 = len(bm.verts)
    b1 = sum(1 for e in bm.edges if e.is_boundary)
    bm.to_mesh(me)
    bm.free()
    me.update()
    lines.append(f"{obj.name} | {v0} → {v1} | {b0} → {b1} | {removed}")

bpy.ops.wm.save_as_mainfile(filepath=out_blend, copy=True)
# Ejes y escala horneados en la malla: en Unity cada pieza queda sin giro y con escala 1, como el FBX original (las palancas,
# agujas, puertas y ruedas se animan girando su pieza; con el giro de 90° y la escala 100 de la exportación por defecto se rompían).
bpy.ops.export_scene.fbx(
    filepath=out_fbx,
    use_selection=False,
    object_types={'MESH', 'EMPTY'},
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL',
    bake_space_transform=True,
    axis_forward='-Z',
    axis_up='Y',
    use_mesh_modifiers=True,
    mesh_smooth_type='OFF',
    use_tspace=False,
    add_leaf_bones=False,
    bake_anim=False,
    path_mode='AUTO',
)
with open(out_report, "w", encoding="utf-8") as f:
    f.write("\n".join(lines))
print("[pulir] listo:", out_fbx)
