# Pulido de mallas (producción P4): mira un .blend sin cambiar nada y escribe un informe por objeto.
# Uso: blender.exe -b <archivo.blend> --python inspeccionar.py -- <salida.txt>
import sys
import bpy
import bmesh

out = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "inspeccion.txt"
lines = [f"Archivo: {bpy.data.filepath}", f"Blender guardado con: {bpy.data.version}",
         f"Unidades: {bpy.context.scene.unit_settings.system}, escala {bpy.context.scene.unit_settings.scale_length}", ""]
lines.append("objeto | tipo | padre | posición | giro | escala | vértices | caras | bordes abiertos | no-manifold | sueltos | dobles(<0,1mm) | caras sin área | modificadores | uv | materiales")
for obj in sorted(bpy.data.objects, key=lambda o: o.name):
    base = f"{obj.name} | {obj.type} | {obj.parent.name if obj.parent else '-'} | {tuple(round(v, 3) for v in obj.location)} | {tuple(round(v, 3) for v in obj.rotation_euler)} | {tuple(round(v, 3) for v in obj.scale)}"
    if obj.type != 'MESH':
        lines.append(base)
        continue
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    boundary = sum(1 for e in bm.edges if e.is_boundary)
    nonmanifold = sum(1 for e in bm.edges if not e.is_manifold and not e.is_boundary)
    loose = sum(1 for v in bm.verts if not v.link_edges)
    degenerate = sum(1 for f in bm.faces if f.calc_area() < 1e-10)
    # dobles: vértices a menos de 0,1 mm de otro (sin soldar)
    size = 0.0001
    cells = {}
    doubles = 0
    for v in bm.verts:
        key = (round(v.co.x / size), round(v.co.y / size), round(v.co.z / size))
        if key in cells:
            doubles += 1
        else:
            cells[key] = v
    bm.free()
    mods = ",".join(m.type for m in obj.modifiers) or "-"
    uvs = ",".join(uv.name for uv in me.uv_layers) or "-"
    mats = ",".join(s.material.name if s.material else "?" for s in obj.material_slots) or "-"
    lines.append(f"{base} | {len(me.vertices)} | {len(me.polygons)} | {boundary} | {nonmanifold} | {loose} | {doubles} | {degenerate} | {mods} | {uvs} | {mats}")

with open(out, "w", encoding="utf-8") as f:
    f.write("\n".join(lines))
print("[inspeccion] listo:", out)
