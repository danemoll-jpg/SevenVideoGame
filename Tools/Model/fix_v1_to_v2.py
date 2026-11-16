import json,struct,io,sys,numpy as np
from PIL import Image, ImageDraw, ImageFilter
src,dst,maskout=sys.argv[1],sys.argv[2],sys.argv[3]
f=open(src,'rb').read()
L=struct.unpack('<I',f[12:16])[0]; j=json.loads(f[20:20+L]); bin0=20+L+8
def bv(i):
    v=j['bufferViews'][i]; o=bin0+v.get('byteOffset',0); return f[o:o+v['byteLength']]
P=np.frombuffer(bv(1),np.float32).reshape(-1,3); UV=np.frombuffer(bv(3),np.float32).reshape(-1,2)
I=np.frombuffer(bv(4),np.uint32).reshape(-1,3)
tex=Image.open(io.BytesIO(bv(0))).convert('RGB'); T=np.asarray(tex).astype(float); h,w,_=T.shape
lumv=T[(UV[:,1]*(h-1)).astype(int),(UV[:,0]*(w-1)).astype(int)].mean(1)
X,Y,Z=P.T; ax=np.abs(X)
# --- pigtail vertices ---
vm=((ax>0.062)&(Y>0.83)) | ((ax>0.062)&(Y>0.79)&(lumv>75)) | ((ax>0.035)&(Y>0.945)&(Z<-0.015))
tm=vm[I].sum(1)>=2
print('pigtail tris',tm.sum(),'of',len(I))
scalp=(ax<0.03)&(Y>0.95)
k=np.median(lumv[scalp])/np.median(lumv[vm]); k=float(np.clip(k,0.45,1.0)); print('darken factor',round(k,3))
# rasterize UV triangles into texture mask
mimg=Image.new('L',(w,h),0); d=ImageDraw.Draw(mimg)
uvp=np.stack([UV[:,0]*(w-1),UV[:,1]*(h-1)],1)
for t in I[tm]:
    d.polygon([tuple(uvp[t[0]]),tuple(uvp[t[1]]),tuple(uvp[t[2]])],fill=255)
mimg=mimg.filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.GaussianBlur(1.5))
mimg.save(maskout)
M=np.asarray(mimg).astype(float)[...,None]/255
T2=T*(1-M)+T*k*M
newtex=Image.fromarray(T2.clip(0,255).astype(np.uint8)); bio=io.BytesIO(); newtex.save(bio,'JPEG',quality=95); jpg=bio.getvalue()
# --- orb hair ties ---
def sphere(c,r,nu=32,nv=20):
    pts=[];nrm=[]
    for a in range(nv+1):
        th=np.pi*a/nv
        for b in range(nu+1):
            ph=2*np.pi*b/nu; n=np.array([np.sin(th)*np.cos(ph),np.cos(th),np.sin(th)*np.sin(ph)])
            nrm.append(n); pts.append(c+r*n)
    idx=[]
    for a in range(nv):
        for b in range(nu):
            p0=a*(nu+1)+b; p1=p0+nu+1
            idx+= [p0,p1,p0+1, p0+1,p1,p1+1]
    return np.array(pts,np.float32),np.array(nrm,np.float32),np.array(idx,np.uint32)
R=0.017
sp=[sphere(np.array([s*0.042,0.960,-0.039]),R) for s in (-1,1)]
SP=np.concatenate([s[0] for s in sp]); SN=np.concatenate([s[1] for s in sp])
SI=np.concatenate([sp[0][2], sp[1][2]+len(sp[0][0])])
# rebuild buffer
chunks=[jpg,bv(1),bv(2),bv(3),bv(4),SP.tobytes(),SN.tobytes(),SI.tobytes()]
targets=[None,34962,34962,34962,34963,34962,34962,34963]
binb=b''; views=[]
for c,tg in zip(chunks,targets):
    while len(binb)%4: binb+=b'\0'
    v={'buffer':0,'byteOffset':len(binb),'byteLength':len(c)}
    if tg: v['target']=tg
    views.append(v); binb+=c
while len(binb)%4: binb+=b'\0'
j['bufferViews']=views; j['buffers']=[{'byteLength':len(binb)}]
na=len(j['accessors'])
j['accessors']+= [
 {'bufferView':5,'componentType':5126,'count':len(SP),'type':'VEC3','min':SP.min(0).tolist(),'max':SP.max(0).tolist()},
 {'bufferView':6,'componentType':5126,'count':len(SN),'type':'VEC3'},
 {'bufferView':7,'componentType':5125,'count':len(SI),'type':'SCALAR'}]
j['materials'].append({'name':'7_hairtie_glow','pbrMetallicRoughness':{'baseColorFactor':[0.92,0.92,0.92,1],'metallicFactor':0.0,'roughnessFactor':0.3},
  'emissiveFactor':[1.0,1.0,1.0],'extensions':{'KHR_materials_emissive_strength':{'emissiveStrength':2.0}}})
j['extensionsUsed']=list(j['extensionsUsed'])+['KHR_materials_emissive_strength']
j['meshes'].append({'name':'7_hairtie_orbs','primitives':[{'mode':4,'material':len(j['materials'])-1,'indices':na+2,'attributes':{'POSITION':na,'NORMAL':na+1}}]})
j['nodes'][0]['name']='7_body'
j['nodes'].append({'name':'7_hairtie_orbs','mesh':len(j['meshes'])-1})
j['scenes'][0]['nodes']=[0,1]
js=json.dumps(j,separators=(',',':')).encode()
while len(js)%4: js+=b' '
out=struct.pack('<III',0x46546C67,2,12+8+len(js)+8+len(binb))+struct.pack('<II',len(js),0x4E4F534A)+js+struct.pack('<II',len(binb),0x004E4942)+binb
open(dst,'wb').write(out); print('wrote',len(out))
