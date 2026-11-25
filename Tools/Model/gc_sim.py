"""Round 20 (Grand Circuit): a quick Python model of the tracks (GcTrack.cs pieces, parsed) and the handling + AI (GcRacer / GcAI),
for tuning without a Unity build. Usage (repo root): python Tools/Model/gc_sim.py  -> per track / vehicle: lap time, wall hits, falls,
slowest speed on a loop, fall-throughs, jump assists. Keep it in step with GcRacer.Step / GcAI.Drive when they change."""
import numpy as np, math, re, sys
from scipy.spatial.transform import Rotation as Rr
import os
src=open(os.environ.get('GC_TRACK_SRC') or os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','..','Unity','Assets','Scripts','Runtime','GcTrack.cs'),encoding='utf-8').read()
def pieces(fn):
    body=src[src.index('public static GcTrack '+fn+'()'):]
    body=body[:body.index('Make("')]
    out=[]
    for m in re.finditer(r'new Piece\(([^)]*)\)',body):
        a=[x.strip() for x in m.group(1).split(',')]
        def f(x): return float(x.replace('f','').replace('W','8')) if re.match(r'^-?[\dW.]+f?$',x) else None
        L=f(a[0]); yaw=f(a[1]) if len(a)>1 else 0; pit=f(a[2]) if len(a)>2 else 0; rol=f(a[3]) if len(a)>3 else 0
        hw=f(a[4]) if len(a)>4 else 7; flags=a[5] if len(a)>5 else 'F.Walls'; shift=f(a[7]) if len(a)>7 else 0
        out.append((L,yaw,pit,rol,hw,flags,shift))
    return out
def rot(y,p,r):
    # Unity: Euler(0,y,0) * AngleAxis(-p, right) * AngleAxis(r, forward); left-handed... emulate with matrices in unity convention
    def ry(a):
        a=math.radians(a); return np.array([[math.cos(a),0,math.sin(a)],[0,1,0],[-math.sin(a),0,math.cos(a)]])
    def rx(a):
        a=math.radians(a); return np.array([[1,0,0],[0,math.cos(a),-math.sin(a)],[0,math.sin(a),math.cos(a)]])
    def rz(a):
        a=math.radians(a); return np.array([[math.cos(a),-math.sin(a),0],[math.sin(a),math.cos(a),0],[0,0,1]])
    # unity left-handed rotations: positive about Y turns +Z toward +X: ry above maps z->(sin,0,cos) ok. about X positive turns +Y toward +Z? Unity: Euler(x) positive pitches nose down (+Z toward -Y).
    return ry(y)@rx(-p)@rz(r)
def make(fn,start=(0,1.2,0)):
    ps=[];ups=[];flat=[];fl=[]
    yaw=pitch=roll=0; p=np.array(start,float)
    for (L,dy,dp,dr,hw,flags,shift) in pieces(fn):
        n=max(1,round(L)); cork='Cork' in flags
        up0=rot(yaw,pitch,roll)@[0,1,0]
        isflat=not any(k in flags for k in ['Loop','Cork','Gap']) and abs(dp)<0.01
        for i in range(n):
            q=rot(yaw+dy/n*.5,pitch+dp/n*.5,roll+dr/n*.5)
            off=(up0-rot(yaw,pitch,roll)@[0,1,0])*8 if cork else 0
            ps.append(p+off); ups.append(q@[0,1,0]); flat.append(isflat); fl.append(flags)
            yaw+=dy/n; pitch+=dp/n; roll+=dr/n
            p=p+q@[0,0,1]
            if shift: p=p+rot(yaw,0,0)@[1,0,0]*shift/n
    err=np.array(start)-p
    ps=np.array(ps); w=np.cumsum([0]+[1 if f else 0 for f in flat])[:-1]/sum(flat)
    ps=ps+np.outer(w,err)
    return ps,np.array(ups),err,fl

def make_full(fn):
    ps=[];qs=[];flat=[];fl=[];hw=[]
    yaw=pitch=roll=0; p=np.array((0,1.2,0),float)
    for (L,dy,dp,dr,h,flags,shift) in pieces(fn):
        n=max(1,round(L)); cork='Cork' in flags
        up0=rot(yaw,pitch,roll)@[0,1,0]
        isflat=not any(k in flags for k in ['Loop','Cork','Gap']) and abs(dp)<0.01
        for i in range(n):
            q=rot(yaw+dy/n*.5,pitch+dp/n*.5,roll+dr/n*.5)
            off=(up0-rot(yaw,pitch,roll)@[0,1,0])*8 if cork else 0
            ps.append(p+off); qs.append(q); flat.append(isflat); fl.append(flags); hw.append(h)
            yaw+=dy/n; pitch+=dp/n; roll+=dr/n
            p=p+q@[0,0,1]
            if shift: p=p+rot(yaw,0,0)@[1,0,0]*shift/n
    err=np.array((0,1.2,0))-p
    ps=np.array(ps); w=np.cumsum([0]+[1 if f else 0 for f in flat])[:-1]/sum(flat)
    ps=ps+np.outer(w,err)
    # chord-based frames
    R=[]
    n=len(ps)
    for i in range(n):
        f=ps[(i+1)%n]-ps[i]; f/=np.linalg.norm(f); up=qs[i]@[0,1,0]
        r=np.cross(up,f); r/=np.linalg.norm(r); u=np.cross(f,r)   # unity left-handed: right = up x fwd
        R.append(np.stack([r,u,f],1))
    return ps,np.array(R),fl,np.array(hw)
def curv(R,s,n):
    i0=int(s-2)%n; i1=int(s+2)%n
    f1=R[i0].T@R[i1][:,2]
    return math.degrees(math.atan2(f1[0],f1[2]))/4
STATS={'7':(50,15,1.0,1.0),'Duchess':(51.5,17,0.9,0.82),'Tunneler':(52,11,0.85,1.1)}
def run(fn,who,laps=1,skill=0.85,dt=0.02,human=False,pace=1.0,seed=1):
    # Round 21: human=True = the human-like test driver (GcAI.HumanLike: reacts 0.16 s late, a shorter look-ahead and a timid line,
    # lifts in the tight bends, a little steering noise); pace = the AI's top-speed factor (GcAI.Pace)
    import random; rnd=random.Random(seed); lagq=[0.0]*max(1,int(0.16/dt)); noise=0; noiseT=0
    ps,R,fl,hw=make_full(fn); n=len(ps)
    top,acc,hand,grip=STATS[who]; top*=pace
    S=n-12.0; boostT=0; X=0; Psi=0; V=0; t=0; walls=0; air=False; falls=0; minloop=99; lap=0; prevS=S; airp=None; airv=None; through=0; assist=0
    G=16
    while lap<laps and t<400:
        t+=dt
        i=int(S)%n; f=fl[i]
        if air:
            airv=airv+np.array([0,-G,0])*dt; nxt=airp+airv*dt
            # nearest sample near gap end
            best=min(range(gapend-30,gapend+90),key=lambda k:np.sum((ps[k%n]-nxt)**2)); k=best%n
            loc=R[k].T@(nxt-ps[k])
            over=('Gap' not in fl[k]) and abs(loc[0])<hw[k]+0.3 and abs(loc[2])<1.5
            if over and loc[1]<=0.55:
                if not wasabove or loc[1]<-2.5: through+=1
                S=float(k); X=max(-(hw[k]-1),min(hw[k]-1,loc[0])); lv=R[k].T@airv; V=math.hypot(lv[0],lv[2])*0.96; Psi=max(-40,min(40,math.degrees(math.atan2(lv[0],lv[2])))); air=False
            else:
                if over: wasabove=loc[1]>0.55
                airp=nxt
                if airp[1]<-80: falls+=1; air=False; S=float(gapend-200); V=18
            continue
        # AI
        v=max(V,8); look=min(max(v*0.7,18),40)*(0.6 if human else 1)
        ca=(curv(R,S+look*0.6,n)+curv(R,S+look,n))/2
        lineX=max(-1,min(1,ca*90))*(hw[i]-2.2)*(0.55 if human else 1)
        rate=62*hand*(1-0.35*min(1,V/60))
        need=curv(R,S+v*0.12,n)*V
        aim=math.degrees(math.atan2(lineX-X,max(10,v*0.9)))
        want=need+(aim-Psi)*3.2
        airR=want>rate*0.95; airL=want<-rate*0.95
        steer=max(-1,min(1,(want-(48*hand if airR else 0)+(48*hand if airL else 0))/rate))
        thr=1; brk=0
        if abs(need)>(rate+48*hand)*1.08: thr=0.2; brk=0.5
        if human:
            noiseT-=dt
            if noiseT<=0: noiseT=0.4+rnd.random()*0.6; noise=(rnd.random()-0.5)*0.35
            lagq.append(steer+noise); steer=max(-1,min(1,lagq.pop(0)))
            if abs(need)>rate*0.85: thr=0.55
            if airL or airR:   # (a person reaches for the air-brakes late: only well past the steering's limit)
                if abs(want)<rate*1.3: airL=airR=False
        # physics
        boostT=max(0,boostT-dt)
        if 'Boost' in f and abs(X)<2.6: boostT=max(boostT,1.1)
        T=top+(16 if boostT>0 else 0)
        a=acc*(1.25 if V<T*0.5 else 1)
        if V<T: V=min(T,V+a*thr*dt)
        elif V>T: V=max(T,V-9*dt)
        if brk: V=max(0,V-30*brk*dt)
        V-=(0.012*V)*dt
        if airL or airR: V-=3.5*dt
        if 'Assist' in f and V<26: V=min(26,V+40*dt)
        if 'Assist' in f: minloop=min(minloop,V)
        turn=steer*rate+(48*hand if airR else 0)-(48*hand if airL else 0)
        c=curv(R,S,n); ds=V*math.cos(math.radians(Psi))*dt
        Psi+=turn*dt-c*ds
        if abs(steer)<0.1 and not airL and not airR: Psi+= -math.copysign(min(abs(Psi),6*grip*dt),Psi)
        Psi=max(-80,min(80,Psi))
        X+=V*math.sin(math.radians(Psi))*dt
        S+=ds
        if S>=n: S-=n; lap+=1
        i=int(S)%n
        if 'Gap' in fl[i]:
            air=True; wasabove=True; q=R[i]; launch=q@[math.sin(math.radians(Psi*0.5)),0,math.cos(math.radians(Psi*0.5))]
            gapend=i
            while 'Gap' in fl[gapend%n]: gapend+=1
            land=ps[gapend%n]+R[gapend%n][:,0]*X+R[gapend%n][:,1]*0.55; start=ps[i]+q[:,0]*X+q[:,1]*0.55
            fl2=land-start; dh=fl2[1]; fl2[1]=0; D=np.linalg.norm(fl2)
            th=math.asin(max(-0.9,min(0.9,launch[1]))); cs=math.cos(th); tn=math.tan(th)
            need=math.sqrt(G*D*D/(2*cs*cs*(D*tn-dh))) if D*tn-dh>0.5 else 40
            land2=ps[(gapend+35)%n]+R[(gapend+35)%n][:,0]*X+R[(gapend+35)%n][:,1]*0.55; f3=land2-start; dh2=f3[1]; f3[1]=0; D2=np.linalg.norm(f3)
            most=math.sqrt(G*D2*D2/(2*cs*cs*(D2*tn-dh2))) if D2*tn-dh2>0.5 else 999
            vv=min(max(V,need*1.04),max(need*1.08,most*0.97)); assist+= V<need
            airp=start; airv=launch*vv
            continue
        lim=hw[i]-0.9
        if abs(X)>lim:
            if 'Walls' in fl[i] or 'Loop' in fl[i] or 'Cork' in fl[i]:
                sg=math.copysign(1,X); X=sg*lim
                if Psi*sg>0: h=math.sin(math.radians(abs(Psi))); V*=1-min(1,h*0.9)*0.5; Psi=-Psi*0.25; walls+=1
            elif abs(X)>hw[i]+0.4: falls+=1; X=0; V=18
    return t,walls,falls,minloop,through,assist
def geom(fn):
    """Round 21: the layout check - closing error before it is spread, the total turn, the length, and the self-clearance (the
    nearest approach of two parts of the ribbon more than 80 m apart along it: in plan, and how far apart in height there)."""
    ps,ups,err,fl=make(fn); n=len(ps)
    turn=sum(p[1] for p in pieces(fn))
    worst=(1e9,0,0,0)
    sub=ps[::3]; idx=np.arange(0,n,3)
    for a in range(len(sub)):
        d=sub-sub[a]; plan=np.hypot(d[:,0],d[:,2]); dy=np.abs(d[:,1])
        sep=np.abs(idx-idx[a]); sep=np.minimum(sep,n-sep)
        bad=(sep>80)&(dy<9.0)
        if bad.any():
            k=np.argmin(np.where(bad,plan,1e9))
            if plan[k]<worst[0]: worst=(plan[k],idx[a],idx[k],dy[k])
    print(f'{fn:10s} length {n} m, turn {turn:.0f} deg, closing error {np.linalg.norm(err):.1f} m ({err[0]:.1f}, {err[1]:.1f}, {err[2]:.1f}), '
          f'height {ps[:,1].min():.0f}..{ps[:,1].max():.0f}, nearest other part (height gap < 9 m) {worst[0]:.1f} m (s {worst[1]} vs {worst[2]}, dy {worst[3]:.1f})')
    return err
if __name__=='__main__':
    names=sys.argv[2:] if len(sys.argv)>2 else re.findall(r'public static GcTrack (\w+)\(\)\s*\{[^}]*?var L = new List<Piece>',src)
    if len(sys.argv)>1 and sys.argv[1]=='geom':
        for fn in names: geom(fn)
    else:
        names=sys.argv[1:] or names
        for fn in names:
            for who in ['7','Duchess','Tunneler']:
                t,w,f,ml,th,a=run(fn,who,laps=2)   # (the grid sits just behind the line: laps=2 = one full lap)
                print(f'{fn:9s} {who:9s} 1 lap {t:6.1f}s  walls {w:3d} falls {f} minloop {ml:5.1f} fallthrough {th} jumpassist {a}')
