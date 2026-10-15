import os,sys,glob,json,numpy as np
from PIL import Image
sys.path.insert(0,'/mnt/skills/plugins/props-3d-invierno/scripts')
import texturas as T
from mapa import M
N=2048
def cargar(p,gray=False):
    im=Image.open(p)
    if gray:
        if im.mode in('I;16','I;16B','I'): a=np.asarray(im,np.float32)/65535.0
        else: a=np.asarray(im.convert('L'),np.float32)/255.0
    else: a=np.asarray(im.convert('RGB'),np.float32)/255.0
    return a
def a_n(a):
    """Redimensiona a NxN respetando el wrap (sin romper el tileo)."""
    h,w=a.shape[:2]
    if (h,w)==(N,N): return a
    t=np.concatenate([a[-8:],a,a[:8]],0); t=np.concatenate([t[:,-8:],t,t[:,:8]],1)
    sy=N/h; sx=N/w; mode='F'
    chans=[t] if t.ndim==2 else [t[...,c] for c in range(t.shape[2])]
    out=[]
    for c in chans:
        r=Image.fromarray(c.astype(np.float32),'F').resize((round(t.shape[1]*sx),round(t.shape[0]*sy)),Image.BICUBIC)
        r=np.asarray(r,np.float32); oy=round(8*sy); ox=round(8*sx); out.append(r[oy:oy+N,ox:ox+N])
    return out[0] if t.ndim==2 else np.stack(out,-1)
def fuente(src,i):
    d=f'raw/{i}'
    if src=='ph':
        f=lambda k:(glob.glob(f'{d}/{k}.*') or [None])[0]
        return dict(col=f('Diffuse'),nor=f('nor_gl'),rough=f('Rough'),ao=f('AO'),metal=f('Metal'),disp=f('Displacement'))
    f=lambda k:(glob.glob(f'{d}/*_{k}.jpg') or [None])[0]
    return dict(col=f('Color'),nor=f('NormalGL'),rough=f('Roughness'),ao=f('AmbientOcclusion'),metal=f('Metalness'),disp=f('Displacement'))
# ---- color
def s2l(c): return np.where(c<=0.04045,c/12.92,((c+0.055)/1.055)**2.4)
def l2s(c): c=np.clip(c,0,1); return np.where(c<=0.0031308,c*12.92,1.055*c**(1/2.4)-0.055)
Mx=np.array([[0.4124,0.3576,0.1805],[0.2126,0.7152,0.0722],[0.0193,0.1192,0.9505]],np.float32); Wp=np.array([0.95047,1,1.08883],np.float32)
def rgb2lab(c):
    x=(s2l(c)@Mx.T)/Wp; f=np.where(x>0.008856,np.cbrt(x),7.787*x+16/116)
    return np.stack([116*f[...,1]-16,500*(f[...,0]-f[...,1]),200*(f[...,1]-f[...,2])],-1)
def lab2rgb(l):
    fy=(l[...,0]+16)/116; fx=fy+l[...,1]/500; fz=fy-l[...,2]/200; f=np.stack([fx,fy,fz],-1)
    x=np.where(f**3>0.008856,f**3,(f-16/116)/7.787)*Wp
    return l2s(x@np.linalg.inv(Mx).T)
def med(lab,mask=None):
    v=lab.reshape(-1,3) if mask is None else lab[mask>0.5]
    return np.median(v[::7],0)
def recolor(alb,modo,target,ref_lab=None):
    lab=rgb2lab(alb); tgt=rgb2lab(T.hexrgb(target)[None,None])[0,0] if isinstance(target,str) else target
    if modo=='nada': return alb,None
    if modo=='pintura':
        m0=med(lab); d=np.sqrt(((lab[...,1:]-m0[1:])**2).sum(-1))
        mask=np.clip(1-(d-10)/14,0,1).astype(np.float32); mask=T.blur(mask,2)
        mp=med(lab,mask)
        nl=lab.copy(); nl[...,0]=lab[...,0]+(tgt[0]-mp[0])*(1-0.25*np.clip((lab[...,0]-mp[0])/40,-1,1))
        nl[...,1:]=tgt[1:]+(lab[...,1:]-mp[1:])*0.6
        out=lab*(1-mask[...,None])+nl*mask[...,None]; return lab2rgb(out),mask
    kL,kab=(1.0,1.0) if modo=='full' else (0.85,0.75)
    m=med(lab); out=lab.copy()
    out[...,0]=lab[...,0]+(tgt[0]-m[0])*kL
    out[...,1:]=lab[...,1:]+(tgt[1:]-m[1:])*kab
    if modo=='full': out[...,1:]=tgt[1:]+(out[...,1:]-tgt[1:])*0.8
    return lab2rgb(out),None
def rot_normal(nrm,k):
    if not k: return nrm
    x=nrm[...,0]*2-1; y=nrm[...,1]*2-1; z=nrm[...,2]
    for _ in range(k): x,y=-y,x
    out=np.stack([x*0.5+0.5,y*0.5+0.5,z],-1)
    return np.rot90(out,k,(0,1))
def costura(a):
    a=a if a.ndim==2 else a.mean(-1)
    dx=np.abs(np.diff(a,axis=1)).mean(); dy=np.abs(np.diff(a,axis=0)).mean()
    ex=np.abs(a[:,0]-a[:,-1]).mean(); ey=np.abs(a[0]-a[-1]).mean()
    r=max(ex/(dx+1e-6),ey/(dy+1e-6)); e=max(ex-dx,ey-dy)
    return r if e>0.02 else min(r,1.0)   # solo cuenta si el salto se ve (>5/255 por encima de lo normal)
def convertir(nombre,out_root,ref_dir):
    src,i,tile,rot,modo,target=M[nombre]; f=fuente(src,i)
    alb=a_n(cargar(f['col'])); nrm=a_n(cargar(f['nor'])); rough=a_n(cargar(f['rough'],True))
    ao=a_n(cargar(f['ao'],True)) if f['ao'] else None
    disp=a_n(cargar(f['disp'],True)) if f['disp'] else None
    if ao is None:
        h=(disp-disp.min())/(np.ptp(disp)+1e-6); ao=T.oclusion(h).astype(np.float32)
    metal=a_n(cargar(f['metal'],True)) if f['metal'] else np.zeros((N,N),np.float32)
    if rot:
        alb=np.rot90(alb,rot,(0,1)); rough=np.rot90(rough,rot); ao=np.rot90(ao,rot); metal=np.rot90(metal,rot)
        if disp is not None: disp=np.rot90(disp,rot)
        nrm=rot_normal(nrm,rot)
    if target is None:   # mediana de la versión procedural (la paleta del vagón)
        p=glob.glob(f'{ref_dir}/**/{T.camel(nombre)}_BaseColor.*',recursive=True)[0]
        ref=np.asarray(Image.open(p).convert('RGB'),np.float32)/255.0
        target=med(rgb2lab(ref)); tgt_ao=True
    else: tgt_ao=False
    # el AO se hornea en el color igual que en la librería (75 % + 25 % AO)
    aof=(0.75+0.25*ao)[...,None]
    if tgt_ao:
        alb2,mask=recolor(alb*aof,modo,target); alb2=np.clip(alb2,0,1)
    else:
        alb2,mask=recolor(alb,modo,target); alb2=np.clip(alb2,0,1)*aof
    if nombre=='chapa_corrugada':   # galvanizado desnudo = metal; óxido y manchas no
        lab=rgb2lab(alb2); sat=np.sqrt(lab[...,1]**2+lab[...,2]**2)
        metal=np.maximum(metal,0.85*np.clip(1-(sat-6)/8,0,1)*np.clip((lab[...,0]-30)/10,0,1)).astype(np.float32)
    m={'alb':alb2,'rough':rough,'metal':metal,'nrm':np.clip(nrm,0,1),'ao':np.clip(ao,0,1)}
    return m,disp
def guardar(m,carpeta,nombre):
    os.makedirs(carpeta,exist_ok=True); base=os.path.join(carpeta,T.camel(nombre))
    u8=lambda a:(np.clip(a,0,1)*255+0.5).astype(np.uint8)
    Image.fromarray(u8(m['alb'])).save(base+'_BaseColor.jpg',quality=92,optimize=True)
    Image.fromarray(u8(m['nrm'])).save(base+'_Normal.png',compress_level=6)
    mr=np.zeros((N,N,3),np.uint8); mr[...,1]=u8(m['rough']); mr[...,2]=u8(m['metal'])
    Image.fromarray(mr).save(base+'_MetalRough.jpg',quality=93)
    ms=np.zeros((N,N,4),np.uint8); ms[...,0]=u8(m['metal']); ms[...,1]=u8(m['ao']); ms[...,2]=255; ms[...,3]=u8(1-m['rough'])
    Image.fromarray(ms,'RGBA').save(base+'_MetalSmooth.png',compress_level=6)
    return base
if __name__=='__main__':
    out=sys.argv[1]; ref=sys.argv[2]; nombres=sys.argv[3].split(',') if len(sys.argv)>3 else list(M)
    res=json.load(open('reporte.json')) if os.path.exists('reporte.json') else {}
    for nom in nombres:
        cat=T.C[nom][0]; m,disp=convertir(nom,out,ref); b=guardar(m,os.path.join(out,cat),nom)
        cs={k:round(float(costura(m[k])),2) for k in ('alb','nrm','rough')}
        # chequeo de la normal rotada: debe acompañar al relieve
        corr=None
        if disp is not None:
            g=np.gradient(T.blur(disp.astype(np.float32),2)); ny=m['nrm'][...,1]*2-1; nx=m['nrm'][...,0]*2-1
            corr=round(float(np.corrcoef(-g[1].ravel()[::13],nx.ravel()[::13])[0,1]),2), round(float(np.corrcoef(g[0].ravel()[::13],ny.ravel()[::13])[0,1]),2)
        res[nom]={'costura':cs,'normal_vs_relieve':corr}
        print(nom,cs,corr,flush=True)
        json.dump(res,open('reporte.json','w'),indent=1)
