import json,os,sys,urllib.request,concurrent.futures as cf
from PIL import Image,ImageDraw,ImageFont
ph=json.load(open('ph.json')); acg={a['assetId']:a for a in json.load(open('acg.json'))}
os.makedirs('thumbs',exist_ok=True)
F=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',12)
def thumb(i):
    p=f'thumbs/{i}.png'
    if os.path.exists(p): return p
    u=f'https://cdn.polyhaven.com/asset_img/thumbs/{i}.png?width=256&height=256' if i in ph else f'https://acg-media.struffelproductions.com/file/ambientCG-Web/media/thumbnail/256-PNG/{i}.png'
    try:
        r=urllib.request.Request(u,headers={'User-Agent':'Mozilla/5.0 (MR04-libreria)'}); open(p,'wb').write(urllib.request.urlopen(r,timeout=30).read()); return p
    except Exception as e: print('fail',i,e); return None
def dim(i):
    if i in ph: d=ph[i].get('dimensions') or [0,0]; return f'{d[0]/1000:.1f}m'
    a=acg.get(i); return f"{(a['dimensionX'] or 0)/100:.1f}m" if a else '?'
def hoja(filas,out):
    ids=[i for _,l in filas for i in l]
    with cf.ThreadPoolExecutor(16) as ex: list(ex.map(thumb,ids))
    t=180;cols=max(len(l) for _,l in filas);W=150+cols*(t+6);H=len(filas)*(t+22)
    im=Image.new('RGB',(W,H),(30,30,32));d=ImageDraw.Draw(im)
    for r,(nom,l) in enumerate(filas):
        y=r*(t+22); d.text((6,y+t//2),nom,fill=(240,200,120),font=F)
        for c,i in enumerate(l):
            x=150+c*(t+6); p=f'thumbs/{i}.png'
            if os.path.exists(p):
                try: im.paste(Image.open(p).convert('RGB').resize((t,t)),(x,y))
                except: pass
            d.text((x,y+t+3),f'{c}:{i[:20]} {dim(i)}',fill=(220,220,220),font=F)
    im.save(out)
if __name__=='__main__':
    filas=json.load(open(sys.argv[1])); hoja(filas,sys.argv[2])
