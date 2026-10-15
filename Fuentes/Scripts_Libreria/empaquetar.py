import sys,os,glob,json,zipfile
sys.path.insert(0,'/mnt/skills/plugins/props-3d-invierno/scripts'); import texturas as T
from mapa import M
LIB='/home/claude/lib_new'; OUT='/mnt/user-data/outputs/libreria_mr04'; LIM=29.5e6
ph=json.load(open('ph.json'))
cat=[]
for n,(c,fn,esc,desc) in T.C.items():
    b=glob.glob(f'{LIB}/{c}/{T.camel(n)}_BaseColor.*')[0]
    e={'nombre':n,'material':T.camel(n),'categoria':c,'BaseColor':os.path.basename(b),'alfa':b.endswith('.png'),'descripcion':desc}
    if n in M:
        src,i,tile,rot,modo,tg=M[n]
        if src=='ph': e.update(escala_m=tile,fuente='Poly Haven',asset=i,autores=list(ph[i]['authors']),url=f'https://polyhaven.com/a/{i}')
        else: e.update(escala_m=tile,fuente='ambientCG',asset=i,autores=['ambientCG'],url=f'https://ambientcg.com/a/{i}')
        e['licencia']='CC0'; e['tipo']='escaneada'
        if i=='Tiles002': e['escala_estimada']=True
    else: e.update(escala_m=esc,tipo='procedural',fuente='texturas.py')
    cat.append(e)
json.dump(cat,open(f'{LIB}/catalogo.json','w'),ensure_ascii=False,indent=1)
lic=['Texturas escaneadas: licencia CC0 (dominio público). No requieren atribución; se listan por prolijidad.','']
lic+=[f"{e['material']}: {e['fuente']} · {e['asset']} · {', '.join(e['autores'])} · {e['url']}" for e in cat if e['tipo']=='escaneada']
open(f'{LIB}/LICENCIAS.txt','w').write('\n'.join(lic)+'\n')
os.makedirs(OUT,exist_ok=True)
for f in glob.glob(OUT+'/*.zip'): os.remove(f)
cats=sorted({e['categoria'] for e in cat}); res=[]
for c in cats:
    mats=[e for e in cat if e['categoria']==c]; grupos=[]
    for e in mats:
        fs=sorted(glob.glob(f"{LIB}/{c}/{e['material']}_*")); grupos.append((sum(os.path.getsize(f) for f in fs),fs))
    grupos.sort(key=lambda g:-g[0]); bins=[]
    for s_,fs in grupos:
        for b in bins:
            if b[0]+s_<=LIM: b[0]+=s_; b[1].extend(fs); break
        else: bins.append([s_,list(fs)])
    partes=[sorted(b[1]) for b in bins]
    for k,fs in enumerate(partes):
        nom=f'{c}.zip' if len(partes)==1 else f'{c}_{k+1}de{len(partes)}.zip'
        with zipfile.ZipFile(f'{OUT}/{nom}','w',zipfile.ZIP_STORED) as z:
            z.write(f'{LIB}/catalogo.json','libreria_mr04/catalogo.json'); z.write(f'{LIB}/LICENCIAS.txt','libreria_mr04/LICENCIAS.txt')
            for f in fs: z.write(f,f'libreria_mr04/{c}/{os.path.basename(f)}')
        res.append((nom,os.path.getsize(f'{OUT}/{nom}')/1e6))
for r in res: print(f'{r[0]:32s} {r[1]:6.1f} MB')
print('total',round(sum(r[1] for r in res),1),'MB en',len(res),'zips')
