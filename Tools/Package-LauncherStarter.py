"""7: The Observer: assemble the players' starter ZIP (launcher + installed game) from a prepared/published signed game release, verify
the extracted copy with the launcher's own code (LauncherChecks.exe: signature with the pinned key, then 'status' opens the extracted
install and finds its game), and stage it as a publisher draft for Publish-LauncherRelease.py. Never signs, uploads or reads keys.
  python Tools/Package-LauncherStarter.py --release Builds/LauncherRelease-800 --out Builds/LauncherStarter-800
ZIP layout: Seven/ = SevenLauncher.exe, Seven-Cover.png (shown in the launcher), Seven-Icon.png / Seven.ico / Seven-Poster.png (for a
Steam shortcut), README-LAUNCHER.md (Docs/LAUNCHER.md), state.json, game-manifest.json, versions/<build>/ (the game), Launcher-Licenses/.
The starter release (tag starter-<build>) never becomes Latest: Latest is always a game-<build> release, whose update-catalog.json every
installed launcher reads. Publish the game release first."""
import argparse,importlib.util,json,pathlib,shutil,subprocess,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('release',ROOT/'Tools/Prepare-LauncherRelease.py');r=importlib.util.module_from_spec(spec);spec.loader.exec_module(r)
ART=ROOT/'Art/Launcher'
def verify(checks,work,envelope):
    # The launcher's own signature check (pinned key); returns the signed payload.
    p=subprocess.run([str(checks),str(work/'verify-root'),str(work/'verify-saves'),'verify',str(envelope)],capture_output=True,text=True)
    if p.returncode:raise ValueError(f'Signature check failed for {envelope}: {p.stderr.strip()}')
    return json.loads(p.stdout)
def copy_exact(source,target,files):
    for f in files:
        s=source/f['path'];t=target/f['path']
        if s.is_symlink() or s.stat().st_size!=f['bytes'] or r.digest(s)!=f['sha256']:raise ValueError(f'{s} differs from its signed manifest')
        t.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(s,t)
def main():
    p=argparse.ArgumentParser(description=__doc__,formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('--release',type=pathlib.Path,help='a Prepare-LauncherRelease / Prepare-ComponentUpdate output folder (sets --game and --game-manifest)')
    p.add_argument('--game',type=pathlib.Path,help='runtime folder matching --game-manifest (default <release>/runtime)')
    p.add_argument('--game-manifest',type=pathlib.Path,help='signed game-manifest.json (default <release>/assets/game-manifest.json)')
    p.add_argument('--launcher',type=pathlib.Path,default=r.DEFAULT_LAUNCHER)
    p.add_argument('--checks',type=pathlib.Path,default=ROOT/'Builds/Launcher/LauncherChecks.exe')
    p.add_argument('--readme',type=pathlib.Path,default=ROOT/'Docs/LAUNCHER.md')
    p.add_argument('--out',type=pathlib.Path,required=True,help='new draft folder, e.g. Builds/LauncherStarter-<build>')
    p.add_argument('--zip-name');p.add_argument('--tag');p.add_argument('--title');p.add_argument('--notes')
    a=p.parse_args()
    if a.release:a.game=a.game or a.release/'runtime';a.game_manifest=a.game_manifest or a.release/'assets/game-manifest.json'
    if not a.game or not a.game_manifest:raise ValueError('--release, or --game and --game-manifest, required')
    if a.out.exists():raise ValueError('Output must be a new immutable draft folder')
    work=a.out/'work';starter=a.out/'starter/Seven';assets=a.out/'assets'
    for d in (work,starter,assets):d.mkdir(parents=True)
    gm=verify(a.checks,work,a.game_manifest)
    if gm['type']!='game':raise ValueError('Wrong manifest type')
    version,build=gm['version'],gm['build']
    zip_name=a.zip_name or f'Seven-Starter-{version}.zip';tag=a.tag or f'starter-{build}'
    title=a.title or f'7: The Observer {version} (starter for new players)'
    notes=a.notes or f'New players: download {zip_name}, extract the Seven folder, start SevenLauncher.exe (Steam Deck: add it as a non-Steam game with Proton). It installs later updates by itself.'
    g=f"versions/{build}"
    copy_exact(a.game,starter/g,gm['files'])
    shutil.copy2(a.launcher,starter/r.LAUNCHER_EXE);shutil.copy2(ART/'Seven-Cover.png',starter/'Seven-Cover.png')
    for name in ('Seven-Icon.png','Seven.ico','Seven-Poster.png'):shutil.copy2(ART/name,starter/name)
    shutil.copy2(a.readme,starter/'README-LAUNCHER.md')
    shutil.copy2(a.game_manifest,starter/'game-manifest.json')
    licenses=starter/'Launcher-Licenses';licenses.mkdir()
    for name in ('JSON-LICENSE.txt','MINIZ-LICENSE.txt'):shutil.copy2(ROOT/'Launcher/vendor'/name,licenses/name)
    r.write(starter/'state.json',dict(schema=1,game=dict(directory=g,manifest=gm),highestGameBuild=build))
    files=r.inventory(starter);archive=assets/zip_name
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=5) as z:
        for f in files:z.write(starter/f['path'],'Seven/'+f['path'])
    if archive.stat().st_size>=2*1024**3:raise ValueError('Starter exceeds the 2 GiB release asset limit')
    extracted=work/'verified-extraction'
    with zipfile.ZipFile(archive) as z:z.extractall(extracted)
    if r.inventory(extracted/'Seven')!=files:raise ValueError('Extracted starter differs')
    # The launcher's own store opens the extracted starter and finds its game (exact inventory of versions/<build>).
    s=subprocess.run([str(a.checks),str(extracted/'Seven'),str(work/'status-saves'),'status'],capture_output=True,text=True)
    if s.returncode or json.loads(s.stdout)['game']['manifest']['build']!=build:raise ValueError('Launcher cannot open the extracted starter: '+s.stderr)
    shutil.rmtree(extracted);shutil.rmtree(starter.parent)
    inventory=dict(tag=tag,title=title,latest=False,game=dict(version=version,build=build,notes=notes),assets=r.inventory(assets),starterFiles=len(files),publication='LOCAL DRAFT ONLY')
    r.write(a.out/'INVENTORY.json',inventory)
    print(json.dumps(dict(archive=str(archive),tag=tag,files=len(files),bytes=archive.stat().st_size,sha256=r.digest(archive),game=version,launcherStatusCheck='PASS')))
if __name__=='__main__':main()
