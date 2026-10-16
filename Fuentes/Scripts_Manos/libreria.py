# Texturas de la librería MR-04 conectadas a los materiales de Blender, solo para las fotos de revisión (en Unity las conecta
# ArtImport). Una vuelta de textura cada 1 unidad de UV, igual que en Unity.
import os, glob, json, math
import bpy
from mathutils import Vector

PROYECTO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
LIBRERIA = os.path.join(PROYECTO, 'Assets', '_Project', 'Art', 'Libreria')
CATALOGO = {e.get('material'): e for e in json.load(open(os.path.join(LIBRERIA, 'catalogo.json'), encoding='utf-8'))}


def conectar_texturas(fuerza_relieve=0.8):
    for m in bpy.data.materials:
        if m.name not in CATALOGO:
            continue
        m.use_nodes = True
        nt = m.node_tree
        b = nt.nodes.get('Principled BSDF') or nt.nodes.new('ShaderNodeBsdfPrincipled')
        for n in [n for n in nt.nodes if n.type in ('TEX_IMAGE', 'NORMAL_MAP', 'SEPARATE_COLOR', 'MATH')]:
            nt.nodes.remove(n)

        def tex(suf, color=True):
            f = [x for x in glob.glob(os.path.join(LIBRERIA, '**', m.name + '_' + suf + '.*'), recursive=True) if not x.endswith('.meta')]
            if not f:
                return None
            t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(f[0], check_existing=True)
            if not color:
                t.image.colorspace_settings.name = 'Non-Color'
            return t
        t = tex('BaseColor')
        if t: nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
        t = tex('Normal', False)
        if t:
            n = nt.nodes.new('ShaderNodeNormalMap'); n.inputs['Strength'].default_value = fuerza_relieve
            nt.links.new(t.outputs['Color'], n.inputs['Color']); nt.links.new(n.outputs['Normal'], b.inputs['Normal'])
        t = tex('MetalSmooth', False)
        if t:                                           # R = metal, A = suavidad (formato de Unity)
            sep = nt.nodes.new('ShaderNodeSeparateColor'); nt.links.new(t.outputs['Color'], sep.inputs['Color'])
            nt.links.new(sep.outputs['Red'], b.inputs['Metallic'])
            inv = nt.nodes.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1.0
            nt.links.new(t.outputs['Alpha'], inv.inputs[1]); nt.links.new(inv.outputs[0], b.inputs['Roughness'])


def camara_y_luz(ancho=900, alto=600, fondo=(0.55, 0.6, 0.66)):
    sc = bpy.context.scene
    w = bpy.data.worlds.new('Fondo'); sc.world = w; w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (*fondo, 1)
    w.node_tree.nodes['Background'].inputs[1].default_value = 0.9
    sol = bpy.data.objects.new('Sol', bpy.data.lights.new('Sol', 'SUN')); sc.collection.objects.link(sol)
    sol.data.energy = 3.0; sol.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
    cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.clip_start = 0.005; cam.data.lens = 50
    sc.render.engine = 'BLENDER_EEVEE'; sc.render.resolution_x = ancho; sc.render.resolution_y = alto
    return cam


def foto(cam, ruta, objetivo, desde, lente=50, visibles=None):
    if visibles is not None:
        for o in bpy.data.objects:
            if o.type == 'MESH':
                o.hide_render = o.name not in visibles
    cam.data.lens = lente
    cam.location = desde
    cam.rotation_euler = (Vector(objetivo) - Vector(desde)).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.render.filepath = ruta
    bpy.ops.render.render(write_still=True)
