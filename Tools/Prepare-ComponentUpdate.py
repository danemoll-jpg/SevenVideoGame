"""7: The Observer: prepare the NEXT game release locally, checked against the previous published one (its signed catalog and manifest
verify with the pinned key, and the build number must rise). Same output as Prepare-LauncherRelease.py (assets/, runtime/, INVENTORY.json;
tag game-<build>). Never uploads.
  python Tools/Prepare-ComponentUpdate.py game --previous-catalog Builds/LauncherRelease-800/assets/update-catalog.json
      --previous-manifest Builds/LauncherRelease-800/assets/game-manifest.json --out Builds/LauncherRelease-900 --version 0.9.0 --build 900 --notes "..."
(If the previous release folder was cleaned up, download its update-catalog.json and game-manifest.json from the seven-releases release.)
Only the 'game' component exists for 7 (the music ships inside the game)."""
import argparse,base64,importlib.util,json,pathlib
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.asymmetric import padding
ROOT=pathlib.Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('release',ROOT/'Tools/Prepare-LauncherRelease.py');r=importlib.util.module_from_spec(spec);spec.loader.exec_module(r)
def verified(path,key):
    e=json.loads(path.read_text());raw=base64.b64decode(e['payload'],validate=True)
    if e['keyId']!=json.loads((ROOT/'Launcher/pinned-key.json').read_text())['keyId']:raise ValueError('Unknown publisher key')
    key.public_key().verify(base64.b64decode(e['signature'],validate=True),raw,padding.PKCS1v15(),hashes.SHA256())
    return json.loads(raw)
def main():
    p=argparse.ArgumentParser(description=__doc__,formatter_class=argparse.RawDescriptionHelpFormatter);p.add_argument('component',choices=['game'])
    p.add_argument('--previous-catalog',type=pathlib.Path,required=True);p.add_argument('--previous-manifest',type=pathlib.Path,required=True)
    r.common_args(p);a=p.parse_args()
    key=r.load_key(a.key);catalog=verified(a.previous_catalog,key);previous=verified(a.previous_manifest,key)
    if catalog['type']!='catalog' or previous['type']!='game' or a.build<=previous['build']:raise ValueError('Correct previous catalog/manifest and an increasing build required')
    if a.out.exists():raise ValueError('Use a fresh immutable output directory')
    r.check_version(a.game,a.version)
    # QF26 (2): never package a build whose image-quality gate is not clean (Tools/Run-ImageGate.ps1 writes the report)
    gate=pathlib.Path(__file__).resolve().parent.parent/'Builds'/'ImageGate'/f'gate_{a.version}.txt'
    if not gate.exists(): raise SystemExit(f'IMAGE GATE (QF26 2): no report for {a.version} ({gate}) - run Tools/Run-ImageGate.ps1 on this build first')
    txt=gate.read_text(encoding='utf-8-sig')
    if 'IMAGE GATE RELEASE (QF26 2): PASS' not in txt: raise SystemExit('IMAGE GATE (QF26 2) not clean for '+a.version+' - see '+str(gate))
    print(f'IMAGE GATE (QF26 2): release gate PASS for {a.version}')
    tag=f'game-{a.build}';base=a.base_url or f'https://github.com/{r.REPO}/releases/download/{tag}'
    a.out.mkdir(parents=True);assets=a.out/'assets';assets.mkdir()
    runtime=a.out/'runtime';files=r.stage_runtime(a.game,runtime,a.launcher)
    gm=r.package(runtime,files,assets,base,a)
    r.write(assets/'game-manifest.json',r.sign(gm,key));catalog['game']=base+'/game-manifest.json'
    r.write(assets/'update-catalog.json',r.sign(catalog,key));inventory=r.inventory(assets)
    if any(x['bytes']>=2*1024**3 for x in inventory):raise ValueError('Asset exceeds GitHub size limit')
    old={f['path']:f['sha256'] for f in previous['files']};new={f['path']:f['sha256'] for f in files}
    changed=dict(added=len(new.keys()-old.keys()),removed=len(old.keys()-new.keys()),changed=sum(1 for k in new.keys()&old.keys() if new[k]!=old[k]))
    r.write(a.out/'INVENTORY.json',dict(tag=tag,title=f'7: The Observer {a.version}',latest=True,game=dict(build=a.build,version=a.version,notes=a.notes),
                                        component='game',assets=inventory,publication='LOCAL DRAFT ONLY'))
    print(json.dumps(dict(component='game',tag=tag,previousBuild=previous['build'],files=len(files),**changed,assets=len(inventory),uploaded=False)))
if __name__=='__main__':main()
