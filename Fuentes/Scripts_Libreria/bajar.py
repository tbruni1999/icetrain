import json,os,sys,urllib.request,zipfile,io,concurrent.futures as cf
UA={'User-Agent':'Mozilla/5.0 (MR04-libreria)'}
def get(u):
    return urllib.request.urlopen(urllib.request.Request(u,headers=UA),timeout=120).read()
def ph(i):
    d=f'raw/{i}'; os.makedirs(d,exist_ok=True)
    f=json.loads(get(f'https://api.polyhaven.com/files/{i}'))
    want={'Diffuse':'jpg','nor_gl':'png','Rough':'jpg','AO':'jpg','Metal':'jpg','Displacement':'png'}
    got=[]
    for k,ext in want.items():
        if k not in f or '2k' not in f[k]: continue
        e=ext if ext in f[k]['2k'] else list(f[k]['2k'])[0]
        p=f'{d}/{k}.{e}'
        if not os.path.exists(p): open(p,'wb').write(get(f[k]['2k'][e]['url']))
        got.append(k)
    return i,got
def acg(i):
    d=f'raw/{i}'; os.makedirs(d,exist_ok=True)
    if not any(n.endswith('.jpg') for n in os.listdir(d)):
        z=zipfile.ZipFile(io.BytesIO(get(f'https://ambientcg.com/get?file={i}_2K-JPG.zip')))
        for n in z.namelist():
            if n.lower().endswith(('.jpg','.png')): open(f'{d}/{os.path.basename(n)}','wb').write(z.read(n))
    return i,os.listdir(d)
PH=sys.argv[1].split(','); AC=sys.argv[2].split(',') if len(sys.argv)>2 and sys.argv[2] else []
with cf.ThreadPoolExecutor(8) as ex:
    for r in ex.map(lambda a:a[0](a[1]),[(ph,i) for i in PH]+[(acg,i) for i in AC]): print(r,flush=True)
