# nombre_libreria: (fuente, id, tile_m, rot90, color_modo, target)
#  color_modo: 'pintura' (recolorea solo la pintura), 'full' (todo), 'suave' (acerca a la paleta), 'nada'
#  target: '#hex' o None (= mediana de la versión procedural)
PINT={'pintura_verde':'#33453a','pintura_verde_baja':'#2f372d','pintura_crema':'#b4ac96','pintura_ocre':'#a17a2b','pintura_roja':'#832c20',
      'pintura_azul_industrial':'#2e4a5e','pintura_gris_maquinaria':'#5d6266','pintura_blanco_sucio':'#c9c6bb','pintura_negra':'#1f1f1e','pintura_naranja_seguridad':'#b5582a'}
M={}
for k,h in PINT.items(): M[k]=('ph','green_metal_rust',1.0,0,'pintura',h)
M.update({
 'acero_oxidado':('ph','rusty_metal_05',1.25,0,'suave',None),
 'chapa_corrugada':('ph','worn_corrugated_iron',1.8,0,'suave',None),
 'chapa_corrugada_verde':('ph','container_side',1.94,0,'pintura','#33453a'),
 'chapa_diamante':('ph','metal_plate',0.5,0,'suave',None),
 'madera':('ph','wood_table_worn',0.55,0,'full','#5b3e25'),
 'tablas_madera':('ph','wood_planks',1.5,0,'full','#5b3e25'),
 'madera_intemperie':('ph','rough_wood',0.5,1,'suave',None),
 'tablas_intemperie':('ph','wood_planks_grey',1.5,1,'suave',None),
 'madera_pintada':('ph','green_rough_planks',1.5,0,'pintura','#33453a'),
 'terciado':('ph','plywood',0.5,0,'suave',None),
 'corteza':('ph','bark_brown_02',1.0,0,'suave',None),
 'hormigon':('ph','concrete_floor_worn_001',3.0,0,'suave',None),
 'hormigon_manchado':('ph','concrete_floor_02',2.0,0,'suave',None),
 'ladrillo':('ph','red_brick_03',1.0,0,'suave',None),
 'azulejo':('acg','Tiles002',0.6,0,'full','#c9c6bb'),
 'balasto':('ph','terrain_red_01',2.0,0,'suave',None),
 'nieve':('ph','snow_02',2.0,0,'full','#e0e5ed'),
 'nieve_compacta':('acg','Snow006',2.5,0,'full','#c4c9d1'),
 'nieve_sucia':('ph','snow_03',2.0,0,'suave',None),
 'nieve_pisada':('ph','snow_01',2.0,0,'full','#d6dbe2'),
 'lona':('ph','hessian_380',0.27,0,'full','#4f5440'),
 'lona_marron':('ph','hessian_380',0.27,0,'full','#6b5a43'),
 'tela_manta':('ph','knitted_fleece',0.27,0,'full','#434e44'),
 'tela_gris':('ph','poly_wool_herringbone',0.27,0,'full','#5c5d5a'),
 'cuero':('ph','brown_leather',0.4,0,'full','#3b2a20'),
})
