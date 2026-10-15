import json,re,sys
C=json.load(open('cands_all.json'))
Q={
'pintura_painted':(['painted metal','paint','metal plate','shutter','green metal','blue metal','container'],['metal','paint','painted','rust','steel']),
'acero_oscuro':(['metal plate','steel','iron','dark metal','metal'],['metal','steel','iron']),
'metal_gastado':(['worn metal','scratched','metal','steel'],['metal','steel']),
'hojalata':(['tin','sheet metal','metal','aluminium','zinc'],['metal','tin','zinc','sheet']),
'acero_galvanizado':(['galvanized','zinc','sheet metal','metal'],['metal','zinc','galvan']),
'aluminio':(['aluminium','aluminum','brushed'],['alumin','brushed']),
'hierro_fundido':(['cast iron','iron','rust','rough metal'],['iron','rust','metal']),
'acero_oxidado':(['rust','rusty','corroded'],['rust','corro']),
'cobre':(['copper','patina','bronze','brass'],['copper','patina','bronze','brass']),
'chapa_corrugada':(['corrugated'],['corrugat']),
'chapa_diamante':(['diamond plate','tread','checker','floor plate'],['diamond','tread','checker']),
'rejilla':(['grate','grating','walkway'],['grate','grating','walkway']),
'madera':(['wood','fine wood','wood table','oak'],['wood']),
'tablas_madera':(['planks','wood floor','wood planks'],['plank','floor']),
'madera_intemperie':(['weathered wood','old wood','wood rough','grey wood'],['weather','old','wood']),
'tablas_intemperie':(['weathered planks','old planks','wood siding','barn'],['plank','siding','barn']),
'madera_pintada':(['painted wood','painted planks','green wood'],['paint']),
'terciado':(['plywood','osb','chipboard'],['plywood','osb','chipboard']),
'durmiente':(['sleeper','railway','tar','dark wood','creosote','rails'],['wood','rail','sleeper']),
'corteza':(['bark'],['bark']),
'hormigon':(['concrete'],['concrete']),
'ladrillo':(['brick'],['brick']),
'azulejo':(['tiles','tile'],['tile']),
'linoleo':(['linoleum','vinyl','floor'],['lino','vinyl']),
'balasto':(['gravel','ballast','rocks ground','crushed'],['gravel','ballast','rock']),
'tierra_congelada':(['frozen','ground','dirt','soil','mud'],['ground','dirt','soil','frozen','mud']),
'nieve':(['snow'],['snow']),
'hielo':(['ice','frozen'],['ice','frozen']),
'lona':(['canvas','fabric','tarp'],['fabric','canvas','tarp']),
'tela':(['wool','fabric','felt','knit'],['fabric','wool','felt']),
'cuero':(['leather'],['leather']),
'goma':(['rubber','tire'],['rubber']),
'carton':(['cardboard'],['cardboard','carton']),
'papel':(['paper'],['paper']),
'plastico':(['plastic'],['plastic']),
'cuerda':(['rope'],['rope']),
}
def buscar(k,n=16):
    frases,req=Q[k]; res=[]
    for c in C:
        t=c['txt']
        if not any(r in t for r in req): continue
        s=sum(3 if f in t else 0 for f in frases)+ (2 if c['src']=='ph' else 0)
        res.append((s,c['pop'],c))
    res.sort(key=lambda x:(-x[0],-x[1]))
    return [r[2] for r in res[:n]]
if __name__=='__main__':
    out={k:[(c['src'],c['id'],c['dim']) for c in buscar(k,int(sys.argv[1]) if len(sys.argv)>1 else 16)] for k in Q}
    json.dump(out,open('cands.json','w'),indent=0)
    for k,v in out.items(): print(k,len(v),[x[1] for x in v])
