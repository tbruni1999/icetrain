import sys,os,glob,json,math,numpy as np
from PIL import Image,ImageDraw,ImageFont
sys.path.insert(0,'/mnt/skills/plugins/props-3d-invierno/scripts'); import texturas as T
from mapa import M
OLD='/home/claude/lib_old1k'; NEW=sys.argv[1]; OUT=sys.argv[2]
FB=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',15)
F=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',12)
FT=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',26)
ph=json.load(open('ph.json'))
CELL=250
def cargar(root,nom,res=512):
    b=glob.glob(f'{root}/**/{T.camel(nom)}_BaseColor.*',recursive=True)[0]; base=b.rsplit('_BaseColor',1)[0]
    im=Image.open(b); alfa=None
    if im.mode=='RGBA': alfa=np.asarray(im.resize((res,res),Image.LANCZOS),np.float32)[...,3]/255
    alb=np.asarray(im.convert('RGB').resize((res,res),Image.LANCZOS),np.float32)/255
    nn=np.asarray(Image.open(base+'_Normal.png').convert('RGB').resize((res,res),Image.LANCZOS),np.float32)/255*2-1
    return alb,nn,alfa
def sombrear(alb,nn):
    L=np.array([-0.45,0.55,0.7]); L/=np.linalg.norm(L)
    return np.clip(alb*(0.42+0.58*np.clip((nn*L).sum(-1),0,1))[...,None],0,1)
def parche(root,nom,tile,area):
    alb,nn,alfa=cargar(root,nom); sh=sombrear(alb,nn)
    reps=area/tile; n=max(1,math.ceil(reps)+1)
    t=np.tile(sh,(n,n,1)); px=int(round(reps*sh.shape[0]))
    t=t[:px,:px]
    return Image.fromarray((t*255).astype(np.uint8)).resize((CELL,CELL),Image.LANCZOS)
cats={}
for nom,(src,i,tile,rot,modo,tg) in M.items(): cats.setdefault(T.C[nom][0],[]).append(nom)
filas=[]
for c in ['pinturas','metales','maderas','construccion','suelos','nieve_hielo','textiles']:
    for nom in cats.get(c,[]): filas.append(nom)
cols=3; W=cols*(2*CELL+30)+20; rowh=CELL+58
rows=math.ceil(len(filas)/cols); H=90+rows*rowh+20
hoja=Image.new('RGB',(W,H),(28,29,31)); d=ImageDraw.Draw(hoja)
d.text((20,18),'Librería MR-04 · antes (procedural) / después (escaneada CC0)',fill=(240,236,225),font=FT)
d.text((20,56),'Cada par muestra la MISMA superficie real (lado indicado), con la textura repetida a su tamaño real: si hubiera costura, se vería como una línea.',fill=(170,170,165),font=F)
for k,nom in enumerate(filas):
    src,i,tile,rot,modo,tg=M[nom]; told=T.C[nom][2]
    area=round(max(1.5*max(tile,told),0.6),2)
    x=20+(k%cols)*(2*CELL+30); y=90+(k//cols)*rowh
    try:
        a=parche(OLD,nom,told,area); b=parche(NEW,nom,tile,area)
    except Exception as e: print('falta',nom,e); continue
    hoja.paste(a,(x,y+20)); hoja.paste(b,(x+CELL+4,y+20))
    d.text((x,y),nom,fill=(240,200,120),font=FB); d.text((x+CELL*2-60,y+3),f'{area:g} m',fill=(150,150,150),font=F)
    d.text((x+4,y+24),'antes',fill=(255,255,255),font=F); d.text((x+CELL+8,y+24),'después',fill=(255,255,255),font=F)
    fuente=('Poly Haven' if src=='ph' else 'ambientCG')
    d.text((x,y+CELL+24),f'{fuente}: {i}  ·  tile {told:g} m → {tile:g} m',fill=(185,185,180),font=F)
hoja.save(OUT,quality=90); print(OUT,hoja.size)
