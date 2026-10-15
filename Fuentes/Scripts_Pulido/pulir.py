# Pulido de mallas (producción P4, pedido de Tomi: "mega pulido de los assets, que la malla quede impecable").
# Abre un .blend, limpia cada malla y exporta un FBX pulido (y guarda el .blend pulido aparte). Los originales no se tocan.
#   blender.exe -b <original.blend> --python pulir.py -- <salida.fbx> <salida.blend> <informe.txt>
# Qué hace en cada malla:
# - suelda los vértices que están en el mismo lugar (las caras venían sueltas, cada una con sus vértices: quedaban uniones
#   abiertas por donde se cuela la luz y bordes que no se tocan);
# - saca las caras sin área y los vértices sueltos;
# - sombreado por ángulo (30°): los cantos marcados quedan duros y lo curvo se ve liso (antes todo era facetado).
# No da vuelta normales en automático (el interior del vagón mira hacia adentro a propósito).
import sys
import bpy
import bmesh

args = sys.argv[sys.argv.index("--") + 1:]
out_fbx, out_blend, out_report = args[0], args[1], args[2]
MERGE = 0.0001        # 0,1 mm
SMOOTH_ANGLE = 0.5236  # 30°

lines = ["objeto | vértices antes → después | bordes abiertos antes → después | caras sin área quitadas"]
for obj in sorted([o for o in bpy.data.objects if o.type == 'MESH'], key=lambda o: o.name):
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    v0 = len(bm.verts)
    b0 = sum(1 for e in bm.edges if e.is_boundary)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=MERGE)
    degenerate = [f for f in bm.faces if f.calc_area() < 1e-10]
    removed = len(degenerate)
    if degenerate:
        bmesh.ops.delete(bm, geom=degenerate, context='FACES_ONLY')
    loose = [v for v in bm.verts if not v.link_faces]
    if loose:
        bmesh.ops.delete(bm, geom=loose, context='VERTS')
    # Cantos duros por ángulo: bordes entre caras de más de 30° quedan marcados como filosos; el resto, suave.
    for e in bm.edges:
        if len(e.link_faces) == 2:
            e.smooth = e.calc_face_angle(0.0) <= SMOOTH_ANGLE
        else:
            e.smooth = False
    for f in bm.faces:
        f.smooth = True
    v1 = len(bm.verts)
    b1 = sum(1 for e in bm.edges if e.is_boundary)
    bm.to_mesh(me)
    bm.free()
    me.update()
    lines.append(f"{obj.name} | {v0} → {v1} | {b0} → {b1} | {removed}")

bpy.ops.wm.save_as_mainfile(filepath=out_blend, copy=True)
bpy.ops.export_scene.fbx(
    filepath=out_fbx,
    use_selection=False,
    object_types={'MESH', 'EMPTY'},
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_NONE',
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
