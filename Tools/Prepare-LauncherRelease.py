"""7: The Observer: prepare a signed game release LOCALLY (never uploads, never reads GitHub credentials).
  python Tools/Prepare-LauncherRelease.py --version 0.8.0 --build 800 --notes "Round 8 ..." --out Builds/LauncherRelease-800
The game payload is the whole Unity/Builds/Seven folder (SevenDemo.exe, SevenDemo_Data, UnityPlayer.dll, ...) minus Unity's
do-not-ship folders, plus the current launcher (Launcher/SevenLauncher.exe + Launcher/launcher.json {"build": LauncherBuild from
Launcher/core.hpp}) so installed launchers update themselves. Output (a new folder): assets/ = game.zip, game-manifest.json and
update-catalog.json (signed with Builds/PublisherPrivate/launcher-signing.pem), runtime/ = the exact files in the manifest (input for
Package-LauncherStarter.py), INVENTORY.json (what Publish-LauncherRelease.py uploads). Release tag: game-<build>.
The first release uses this script; later rounds may use it too (the catalog only points at the game) or Prepare-ComponentUpdate.py,
which also checks the build number rises over the previous release."""
import argparse, base64, hashlib, json, pathlib, re, shutil, zipfile
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import padding

ROOT = pathlib.Path(__file__).resolve().parents[1]
REPO = 'danemoll-jpg/seven-releases'
GAME_EXE = 'SevenDemo.exe'
LAUNCHER_EXE = 'SevenLauncher.exe'
DEFAULT_GAME = ROOT/'Unity/Builds/Seven'
DEFAULT_LAUNCHER = ROOT/'Builds/Launcher'/LAUNCHER_EXE
DEFAULT_KEY = ROOT/'Builds/PublisherPrivate/launcher-signing.pem'
# Unity's own folders that must never ship (and the launcher folder, which is replaced by the current launcher).
EXCLUDED = {'SevenDemo_BurstDebugInformation_DoNotShip', 'SevenDemo_BackUpThisFolder_ButDontShipItWithYourGame', 'Launcher'}
def digest(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024), b''): h.update(b)
    return h.hexdigest()
def inventory(root):
    result=[]
    for p in sorted(root.rglob('*')):
        if p.is_symlink() or p.is_junction(): raise ValueError(f'Linked input forbidden: {p}')
        if p.is_file(): result.append(dict(path=p.relative_to(root).as_posix(),bytes=p.stat().st_size,sha256=digest(p)))
    return result
def sign(payload,key):
    raw=json.dumps(payload,sort_keys=True,separators=(',',':'),ensure_ascii=False).encode()
    public=json.loads((ROOT/'Launcher/pinned-key.json').read_text())
    return dict(keyId=public['keyId'],payload=base64.b64encode(raw).decode(),signature=base64.b64encode(key.sign(raw,padding.PKCS1v15(),hashes.SHA256())).decode())
def load_key(path):
    key=serialization.load_pem_private_key(path.read_bytes(),password=None)
    pinned=json.loads((ROOT/'Launcher/pinned-key.json').read_text())
    if format(key.public_key().public_numbers().n,'x')!=pinned['modulusHex']: raise ValueError('The private key does not match Launcher/pinned-key.json')
    return key
# GitHub's release asset limit is 2 GiB; stay well under it per file.
FILE_LIMIT=int(1.8*1024**3)
def guard(files,what):
    big=[f for f in files if f['bytes']>=FILE_LIMIT]
    if big: raise ValueError(what+': over the 1.8 GiB per-file limit: '+', '.join(f"{f['path']} ({f['bytes']:,} bytes)" for f in big))
def write(path,obj): path.write_text(json.dumps(obj,indent=2),encoding='utf-8')
def launcher_build():
    m=re.search(r'LauncherBuild=(\d+);',(ROOT/'Launcher/core.hpp').read_text(encoding='utf-8'))
    if not m: raise ValueError('LauncherBuild not found in Launcher/core.hpp')
    return int(m.group(1))
def check_version(game,version):
    # Unity/Builds/Seven/version.txt is written by FightDemoBuilder at build time (LauncherBridge.Version); it must match --version.
    stamp=game/'version.txt'
    if not stamp.is_file(): print(f'WARNING: {stamp} missing (the builder writes it); version not cross-checked',flush=True); return
    found=stamp.read_text(encoding='utf-8').strip()
    if found!=version: raise ValueError(f'Build is {found!r} (version.txt) but --version is {version!r}: rebuild the game or fix the version')
def stage_runtime(game,runtime,launcher):
    """Copy the game folder (explicit top-level items only) and add the current launcher; returns the signed inventory."""
    if not (game/GAME_EXE).is_file(): raise ValueError(f'Complete game runtime required: {game/GAME_EXE} missing')
    if not launcher.is_file(): raise ValueError(f'Launcher not built: {launcher} (powershell -File Tools/Build-Launcher.ps1)')
    runtime.mkdir(parents=True)
    for item in game.iterdir():
        if item.name in EXCLUDED: continue
        if item.is_symlink() or item.is_junction(): raise ValueError('Linked runtime input')
        if item.is_dir(): shutil.copytree(item,runtime/item.name)
        else: shutil.copy2(item,runtime/item.name)
    (runtime/'Launcher').mkdir()
    shutil.copy2(launcher,runtime/'Launcher'/LAUNCHER_EXE)
    write(runtime/'Launcher/launcher.json',dict(build=launcher_build()))
    files=inventory(runtime); guard(files,'Game file'); return files
def package(runtime,files,assets,base,a):
    archive=assets/'game.zip'
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=5) as z:
        for f in files: z.write(runtime/f['path'],f['path'])
    guard([dict(path='game.zip',bytes=archive.stat().st_size)],'Game package')
    return dict(schema=1,type='game',platform='windows-x64',minLauncher=1,build=a.build,version=a.version,notes=a.notes,saveSchema=1,files=files,
                package=dict(url=base+'/game.zip',bytes=archive.stat().st_size,sha256=digest(archive)))
def common_args(p):
    p.add_argument('--game',type=pathlib.Path,default=DEFAULT_GAME,help='built game folder (default Unity/Builds/Seven)')
    p.add_argument('--out',type=pathlib.Path,required=True,help='new folder, e.g. Builds/LauncherRelease-<build>')
    p.add_argument('--version',required=True,help='e.g. 0.8.0 (0.<round>.<fix>)');p.add_argument('--build',type=int,required=True,help='e.g. 800 (round*100+fix)')
    p.add_argument('--notes',required=True);p.add_argument('--key',type=pathlib.Path,default=DEFAULT_KEY)
    p.add_argument('--launcher',type=pathlib.Path,default=DEFAULT_LAUNCHER)
    p.add_argument('--base-url',help='where the assets will be (default the GitHub release game-<build>; a local check: http://127.0.0.1:<port>/...)')
def main():
    p=argparse.ArgumentParser(description=__doc__,formatter_class=argparse.RawDescriptionHelpFormatter);common_args(p);a=p.parse_args()
    if a.out.exists(): raise ValueError('Output must be a new immutable release directory')
    check_version(a.game,a.version)
    key=load_key(a.key);tag=f'game-{a.build}'
    base=a.base_url or f'https://github.com/{REPO}/releases/download/{tag}'
    a.out.mkdir(parents=True); assets=a.out/'assets';assets.mkdir()
    runtime=a.out/'runtime';files=stage_runtime(a.game,runtime,a.launcher)
    gm=package(runtime,files,assets,base,a)
    write(assets/'game-manifest.json',sign(gm,key))
    write(assets/'update-catalog.json',sign(dict(schema=1,type='catalog',game=base+'/game-manifest.json'),key))
    write(a.out/'INVENTORY.json',dict(tag=tag,title=f'7: The Observer {a.version}',latest=True,game=dict(build=a.build,version=a.version,notes=a.notes),
                                      assets=inventory(assets),publication='LOCAL DRAFT ONLY; no upload authorized by this command'))
    print(json.dumps(dict(output=str(a.out),tag=tag,runtimeFiles=len(files),gameZipBytes=(assets/'game.zip').stat().st_size,launcherBuild=launcher_build(),uploaded=False)))
if __name__=='__main__': main()
