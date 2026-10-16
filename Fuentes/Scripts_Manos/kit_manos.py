# Funciones de modelado para las manos y las herramientas en primera persona (Claude, 2026-10-16).
# Se usan desde Blender sin ventana:  blender -b -P <script>.py
# Convención: metros; en Blender Z es "a lo largo de la herramienta" (en Unity queda como el eje Y del modelo).
import math
import bpy
import bmesh
from mathutils import Vector, Matrix, Quaternion


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def nuevo_bm():
    return bmesh.new()


def _perpendicular(t):
    a = Vector((1, 0, 0)) if abs(t.x) < 0.9 else Vector((0, 1, 0))
    n = t.cross(a).normalized()
    return n


def barrido(bm, puntos, radios, lados=12, mat=0, escala_x=None, escala_y=None, tapas=True, giro=0.0, normal_inicial=None):
    """Barre un perfil (polígono regular de 'lados', o elipse con escala_x/escala_y por punto) a lo largo de una línea.
    radios: un número o uno por punto. escala_x / escala_y: listas (una por punto) que aplastan el perfil."""
    pts = [Vector(p) for p in puntos]
    n = len(pts)
    rad = radios if isinstance(radios, (list, tuple)) else [radios] * n
    sx = escala_x or [1.0] * n
    sy = escala_y or [1.0] * n
    # Marco que viaja con la línea sin retorcerse (transporte paralelo).
    tangentes = []
    for i in range(n):
        a = pts[max(i - 1, 0)]
        b = pts[min(i + 1, n - 1)]
        tangentes.append((b - a).normalized())
    normal = Vector(normal_inicial).normalized() if normal_inicial else _perpendicular(tangentes[0])
    anillos = []
    prev_t = tangentes[0]
    for i in range(n):
        t = tangentes[i]
        q = prev_t.rotation_difference(t)
        normal = (q @ normal).normalized()
        prev_t = t
        binormal = t.cross(normal).normalized()
        anillo = []
        for k in range(lados):
            ang = 2 * math.pi * k / lados + giro
            off = normal * (math.cos(ang) * rad[i] * sx[i]) + binormal * (math.sin(ang) * rad[i] * sy[i])
            anillo.append(bm.verts.new(pts[i] + off))
        anillos.append(anillo)
    for i in range(n - 1):
        for k in range(lados):
            a, b = anillos[i][k], anillos[i][(k + 1) % lados]
            c, d = anillos[i + 1][(k + 1) % lados], anillos[i + 1][k]
            f = bm.faces.new((a, b, c, d))
            f.material_index = mat if not callable(mat) else mat(i)
    if tapas:
        for anillo, inv in ((anillos[0], True), (anillos[-1], False)):
            f = bm.faces.new(list(reversed(anillo)) if inv else anillo)
            f.material_index = mat if not callable(mat) else mat(0 if inv else n - 2)
    return anillos


def caja(bm, centro, tam, mat=0, rot=None):
    cx, cy, cz = centro
    hx, hy, hz = tam[0] / 2, tam[1] / 2, tam[2] / 2
    m = rot or Matrix.Identity(3)
    vs = []
    for x, y, z in [(-hx, -hy, -hz), (hx, -hy, -hz), (hx, hy, -hz), (-hx, hy, -hz),
                    (-hx, -hy, hz), (hx, -hy, hz), (hx, hy, hz), (-hx, hy, hz)]:
        vs.append(bm.verts.new(Vector((cx, cy, cz)) + m @ Vector((x, y, z))))
    caras = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    for c in caras:
        f = bm.faces.new([vs[i] for i in c])
        f.material_index = mat
    return vs


def contorno_extruido(bm, contorno, eje_a, eje_b, eje_grosor, origen, grosor, mat=0):
    """Un contorno 2D (lista de (a, b)) en el plano de eje_a/eje_b, extruido 'grosor' a lo largo de eje_grosor (centrado)."""
    ea, eb, eg, o = Vector(eje_a), Vector(eje_b), Vector(eje_grosor), Vector(origen)
    abajo = [bm.verts.new(o + ea * a + eb * b - eg * grosor / 2) for a, b in contorno]
    arriba = [bm.verts.new(o + ea * a + eb * b + eg * grosor / 2) for a, b in contorno]
    n = len(contorno)
    f = bm.faces.new(list(reversed(abajo))); f.material_index = mat
    f = bm.faces.new(arriba); f.material_index = mat
    for i in range(n):
        j = (i + 1) % n
        f = bm.faces.new((abajo[i], abajo[j], arriba[j], arriba[i])); f.material_index = mat
    return abajo, arriba


def terminar(nombre, bm, materiales, bisel=0.0, angulo=40, uv_tam=0.5):
    """Objeto con sus materiales (nombres de la librería MT_...), normales prolijas, bisel opcional y UV por proyección."""
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    me = bpy.data.meshes.new(nombre)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(nombre, me)
    bpy.context.scene.collection.objects.link(ob)
    for m in materiales:
        mat = bpy.data.materials.get(m) or bpy.data.materials.new(m)
        me.materials.append(mat)
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    if bisel > 0:
        mod = ob.modifiers.new('Bisel', 'BEVEL')
        mod.width = bisel
        mod.segments = 2
        mod.limit_method = 'ANGLE'
        mod.angle_limit = math.radians(35)
        bpy.ops.object.modifier_apply(modifier=mod.name)
    me.shade_smooth()
    me.set_sharp_from_angle(angle=math.radians(angulo))
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.cube_project(cube_size=uv_tam)
    bpy.ops.object.mode_set(mode='OBJECT')
    ob.select_set(False)
    return ob


def vacio(nombre, pos, padre=None, rot=(0, 0, 0)):
    e = bpy.data.objects.new(nombre, None)
    e.empty_display_size = 0.02
    e.location = pos
    e.rotation_euler = rot
    bpy.context.scene.collection.objects.link(e)
    if padre is not None:
        e.parent = padre
    return e


def exportar(ruta):
    bpy.ops.export_scene.fbx(
        filepath=ruta, use_selection=False, object_types={'MESH', 'EMPTY'},
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
        axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE', use_mesh_modifiers=True, add_leaf_bones=False)


def foto(ruta, objetivo, desde, lente=50, colores=None):
    """Una foto rápida con Eevee para revisar (no es arte final)."""
    sc = bpy.context.scene
    for m in bpy.data.materials:
        m.use_nodes = True
        col = (colores or {}).get(m.name, (0.5, 0.5, 0.5, 1))
        bsdf = m.node_tree.nodes.get('Principled BSDF')
        if bsdf:
            bsdf.inputs['Base Color'].default_value = col
            bsdf.inputs['Roughness'].default_value = 0.55
    cam_data = bpy.data.cameras.new('Cam')
    cam_data.lens = lente
    cam_data.clip_start = 0.005
    cam = bpy.data.objects.new('Cam', cam_data)
    sc.collection.objects.link(cam)
    cam.location = desde
    cam.rotation_euler = (Vector(objetivo) - Vector(desde)).to_track_quat('-Z', 'Y').to_euler()
    sol_data = bpy.data.lights.new('Sol', 'SUN')
    sol_data.energy = 3.5
    sol = bpy.data.objects.new('Sol', sol_data)
    sc.collection.objects.link(sol)
    sol.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
    w = bpy.data.worlds.new('Fondo')
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (0.55, 0.6, 0.66, 1)
    w.node_tree.nodes['Background'].inputs[1].default_value = 0.8
    sc.camera = cam
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x = 900
    sc.render.resolution_y = 700
    sc.render.filepath = ruta
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam, do_unlink=True)
    bpy.data.objects.remove(sol, do_unlink=True)
