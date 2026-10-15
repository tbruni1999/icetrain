# Etiquetas de lata con las nervaduras (vueltitas) en el relieve: papel de la librería + acanalado horizontal.
import sys, os, glob, numpy as np
from PIL import Image
sys.path.insert(0, '/home/claude/skill_new/scripts'); import texturas as T
DEST = '/home/claude/props_texturas/propios'; N = 2048
PASO = 0.009; ANCHO = 0.0035          # una nervadura cada 9 mm, 3,5 mm de ancho (la textura mide 1 m)
for nombre, hexc in [('etiqueta_lata_acanalada', '#832c20'), ('franja_lata_acanalada', '#b4ac96')]:
    T.generar(nombre, '/home/claude/props/tx', N, receta='papel', hexc=hexc)
    base = glob.glob(f'/home/claude/props/tx/**/{T.camel(nombre)}_BaseColor.*', recursive=True)[0].rsplit('_BaseColor', 1)[0]
    v = (np.arange(N, dtype=np.float32) + 0.5) / N
    d = np.abs(((v % PASO) / PASO) - 0.5) * PASO                         # distancia al centro de la nervadura (m)
    perfil = np.clip(1 - (d / (ANCHO / 2)) ** 2, 0, 1)                    # surco redondeado
    alto = np.repeat((-perfil)[:, None], N, 1).astype(np.float32)
    nr = T.normal(alto, 9.0)
    no = np.asarray(Image.open(base + '_Normal.png').convert('RGB'), np.float32) / 255 * 2 - 1
    n = np.stack([no[..., 0] + nr[..., 0], no[..., 1] + nr[..., 1], no[..., 2] * nr[..., 2]], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    os.makedirs(DEST, exist_ok=True); out = os.path.join(DEST, T.camel(nombre))
    Image.fromarray(((n * 0.5 + 0.5) * 255).astype(np.uint8)).save(out + '_Normal.png')
    sombra = (1 - 0.12 * perfil)[:, None]
    bc = np.asarray(Image.open(base + '_BaseColor.jpg').convert('RGB'), np.float32) * sombra[..., None]
    Image.fromarray(np.clip(bc, 0, 255).astype(np.uint8)).save(out + '_BaseColor.jpg', quality=92)
    ms = np.asarray(Image.open(base + '_MetalSmooth.png'), np.float32); ms[..., 1] *= sombra
    Image.fromarray(ms.astype(np.uint8), 'RGBA').save(out + '_MetalSmooth.png')
    os.system(f'cp {base}_MetalRough.jpg {out}_MetalRough.jpg')
    print('ok', out)
