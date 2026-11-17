# 7: The Observer — launcher (SevenLauncher.exe)

The launcher installs **7: The Observer**, keeps it up to date and starts it. Every round Dan publishes a new
version; the launcher finds it, downloads it, checks it and starts the new version for you. You never need to download
the game again.

## What it does

- **On start:** checks for a newer version (a few seconds), installs it if there is one, then starts the game.
  Press **B / Esc** to skip the update and play the version you have now; press **any other button** to open the menu.
- **Menu** (D-pad / arrow keys to choose, A / Enter to confirm):
  *Play current version* · *Check for updates* · *Update game and play* · *Restore previous version* ·
  *Cancel check or download* · *Exit*.
- **Safe updates:** every update is signed by the publisher; the launcher refuses anything with a wrong signature or a
  file that does not match its checksum. If a download fails or is cancelled, the version you had stays as it was.
- **Saves are backed up** before every update (into `save-backups/` inside the launcher folder). They are never
  restored automatically; the game keeps using its normal save folder.
- **The previous version stays installed**, so *Restore previous version* goes back instantly if an update has a problem.
- Needs the internet only to check and download; without it, *Play current version* always works.

## Install (Windows)

1. Download **`Seven-Starter-<version>.zip`** from the newest starter release at
   <https://github.com/danemoll-jpg/seven-releases/releases> (look for a release with a `Seven-Starter-…zip` asset).
2. Extract the whole **`Seven`** folder to a place you can write to, e.g. `Documents\Games\Seven`.
   (Not `Program Files`: the launcher writes its updates next to itself.) Keep everything in the folder together.
3. Start **`SevenLauncher.exe`**. Make a desktop shortcut to it if you like (right-click → Send to → Desktop).
   Windows SmartScreen may warn on the first start because the exe is not code-signed by Microsoft: *More info* → *Run anyway*.

Folder contents: `SevenLauncher.exe` (start this), `versions/<build>/` (the installed game — don't start
`SevenDemo.exe` there directly if you want updates), `state.json` (which version is selected), `Seven-Icon.png`,
`Seven.ico` and `Seven-Poster.png` (art for shortcuts), `Launcher-Licenses/`.

## Install on Steam Deck (Proton)

1. In **Desktop Mode**, download the starter ZIP (Firefox / browser) and extract the `Seven` folder, e.g. to
   `/home/deck/Games/Seven` (or on the SD card).
2. Steam (desktop) → **Games → Add a Non-Steam Game to My Library… → Browse…** → set the file type filter to *All Files*
   → pick `Seven/SevenLauncher.exe` → *Add Selected Programs*.
3. In the library: right-click the new entry → **Properties**:
   - rename it to **7: The Observer**; set the icon to `Seven/Seven-Icon.png` (or `Seven.ico`);
   - **Compatibility** → tick **Force the use of a specific Steam Play compatibility tool** → choose
     **Proton Experimental** (or the newest Proton).
   - **Launch options:** leave empty. (The launcher starts the game itself; optional for the game window:
     `-screen-fullscreen 1`.)
   - Library art: right-click → *Manage → Set custom artwork* → `Seven-Poster.png`.
4. Return to **Gaming Mode** and start it like any game. The launcher's menu works with the Deck's D-pad and A / B.
   Updates install the same way as on Windows (Proton keeps everything inside the `Seven` folder and its Proton prefix).

## Updating

Nothing to do: start the launcher and it updates by itself. To check by hand: open the menu (any button during the
start), *Check for updates*, then *Update game and play*. The launcher also updates **itself**: a game release carries
the newest launcher, which replaces the old one on the next start.

## If something goes wrong

- *"Startup did not complete"*: the game closed or did not report back within 90 s. Choose *Play current version* to
  retry, or *Restore previous version*.
- *"Update service unavailable"* / no internet: play the installed version; try again later.
- *"Extract the complete starter package before launching"*: the folder is incomplete — extract the whole ZIP again.
- The launcher keeps a log in `recovery.log` next to it; send that file to Dan with any report.

---

## Maintainers (Dan / Code)

Everything runs on Dan's PC from the repo root `C:\Users\danmo\SevenVideoGame`. Heavy steps hidden at Below Normal, never
while Unity or SevenDemo is running.

**One-time (done in Round 8):**
- Signing key: `python Tools/New-LauncherKey.py` → private key `Builds/PublisherPrivate/launcher-signing.pem`
  (git-ignored; **never delete, print, copy or replace it** — every installed launcher trusts only this key), public key
  pinned in `Launcher/pinned-key.json` + `Launcher/pinned_key.hpp`. Re-running it keeps the existing key.
- Art: `python Tools/make_launcher_art.py` → `Art/Launcher/Seven.ico`, `Seven-Icon.png`, `Seven-Cover.png`,
  `Seven-Poster.png` (strictly monochrome) and `Launcher/Seven.ico`.
- Releases repo: `danemoll-jpg/seven-releases` (public; anonymous downloads).

**Build the launcher** (Visual Studio 18 C++, Windows SDK 10.0.28000.0):
`powershell -File Tools/Build-Launcher.ps1 -CoreOnly` → `Builds/Launcher/LauncherChecks.exe` (console checks), then
`powershell -File Tools/Build-Launcher.ps1` → `Builds/Launcher/SevenLauncher.exe`. Raise `LauncherBuild` in
`Launcher/core.hpp` whenever the launcher changes, so installed launchers replace themselves.

**Every round** (version `0.<round>.<fix>`, build `<round>*100+<fix>`; `LauncherBridge.Version` / `.Build` in
`Unity/Assets/Scripts/Runtime/LauncherBridge.cs` must match; the builder writes `version.txt` next to the exe):
1. Final game build (Build everything) → `Unity\Builds\Seven`.
2. `python Tools/Prepare-LauncherRelease.py --version 0.R.0 --build R00 --notes "<what changed>" --out Builds/LauncherRelease-R00`
   (or `Prepare-ComponentUpdate.py game --previous-catalog … --previous-manifest … …` to also check the build rises).
3. `python Tools/Publish-LauncherRelease.py Builds/LauncherRelease-R00` (dry run), then with
   `--publish --confirm-repository danemoll-jpg/seven-releases` → release `game-R00`, Latest.
4. Starter for new players (optional after the first round; old starters update themselves):
   `python Tools/Package-LauncherStarter.py --release Builds/LauncherRelease-R00 --out Builds/LauncherStarter-R00`, then
   `python Tools/Publish-LauncherRelease.py Builds/LauncherStarter-R00 --publish --confirm-repository danemoll-jpg/seven-releases`
   → release `starter-R00` (never Latest).

Game side: `LauncherBridge.cs` (the launcher passes `-sevenLauncherReady <file> -sevenLauncherNonce <text>`; the game
writes the nonce once its first scene runs). A launcher self-check (hidden, muted game):
`SevenLauncher.exe --auto-check --root <install> --test-save <dir> --evidence <dir> [--catalog http://127.0.0.1:<port>/…]`.
