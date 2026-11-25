using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// Round 20: the Grand Circuit's own screens, over a camera flying the selected track: the mode (single race / time trial / the
    /// split-screen layout / back to the main menu), "Press A to join" for player 2, racer select per player (7, Duchess, Tunneler with
    /// their stats; each player picks on their own pad), track select (best times shown), then the race; the results after it; the
    /// pause page (Start) during it. Controller buttons only on screen; the keyboard still works for player 1.
    /// </summary>
    public class GcMenu : MonoBehaviour
    {
        public GcGame Game;
        public enum Page { Off, Title, Join, Racer, Track, Results, Pause, Records }
        public Page Current = Page.Off;
        Camera _cam; GameObject _canvas; Font _font; Text _title, _hint, _sub; readonly List<Text> _rows = new List<Text>(); readonly List<Image> _bars = new List<Image>();
        readonly List<(string label, System.Action act, System.Action<int> adjust)> _items = new List<(string, System.Action, System.Action<int>)>();
        int _sel; float _stickReady; float _flyS; string _showTrack = "cattalus";
        public int Players = 1; public readonly Gamepad[] Pads = new Gamepad[2]; public readonly int[] Pick = { 0, 1 }; readonly bool[] _locked = new bool[2];
        GameObject _racerPanel; readonly Text[] _racerText = new Text[2];
        public string PageName => Current.ToString();
        public bool Busy => Current != Page.Off;

        Camera MenuCam()
        {
            if (_cam != null) return _cam;
            var go = Instantiate(Game.CamTemplate.gameObject, transform); go.name = "MenuCam"; go.SetActive(true);
            _cam = go.GetComponent<Camera>(); _cam.rect = new Rect(0, 0, 1, 1); _cam.fieldOfView = 55f; _cam.depth = 10f; go.tag = "MainCamera";
            if (go.GetComponent<AudioListener>() == null) go.AddComponent<AudioListener>();
            return _cam;
        }

        public void Open()
        {
            GcSave.Load(); Pick[0] = GcSave.LastRacer; _showTrack = GcTrack.Ids[Mathf.Clamp(GcSave.LastTrack, 0, GcTrack.Ids.Length - 1)];
            if (Game.Track == null || Game.Track.Id != _showTrack) Game.BuildTrack(_showTrack);
            MenuCam().gameObject.SetActive(true);
            MusicSystem.Play(GcTrack.MenuSong(), 1.5f);   // QF36: the Grand Circuit menu song on every page
            Show(Page.Title);
        }

        // ------------------------------------------------------------------------------------------ the pages
        public void Show(Page p)
        {
            Current = p; _sel = 0; _items.Clear();
            switch (p)
            {
                case Page.Title:
                    _items.Add(("Single race", () => { GcGame.NextMode = GcGame.Mode.Race; GcSave.LastMode = 0; Players = 1; Pads[1] = null; Show(Page.Join); }, null));
                    _items.Add(("Time trial", () => { GcGame.NextMode = GcGame.Mode.TimeTrial; GcSave.LastMode = 1; Players = 1; Pads[1] = null; Show(Page.Racer); }, null));
                    _items.Add((SplitLabel, () => { GcGame.SideBySide = !GcGame.SideBySide; Relabel(2, SplitLabel); }, d => { GcGame.SideBySide = !GcGame.SideBySide; Relabel(2, SplitLabel); }));
                    _items.Add(("Best times", () => Show(Page.Records), null));
                    _items.Add(("Back to the main menu", () => { GcSave.Save(); SceneManager.LoadScene(DemoMenu.SceneName); }, null));
                    break;
                case Page.Join:
                    _items.Add(("Race", () => { Show(Page.Racer); }, null));
                    _items.Add(("Back", () => Show(Page.Title), null));
                    break;
                case Page.Racer:
                    _locked[0] = _locked[1] = false;
                    _items.Add(("Ready", () => { if (AllLocked()) Show(Page.Track); else { for (int i = 0; i < Players; i++) _locked[i] = true; Show(Page.Track); } }, null));
                    _items.Add(("Back", () => Show(GcGame.NextMode == GcGame.Mode.Race ? Page.Join : Page.Title), null));
                    break;
                case Page.Track:
                    for (int i = 0; i < GcTrack.Ids.Length; i++) { int k = i; _items.Add((TrackLabel(k), () => StartRace(k), null)); }
                    _items.Add(("Back", () => Show(Page.Racer), null));
                    _sel = Mathf.Clamp(GcSave.LastTrack, 0, GcTrack.Ids.Length - 1);
                    break;
                case Page.Results:
                    _items.Add(("Race again", () => { Close(); StartCoroutine(Restart()); }, null));
                    _items.Add(("Grand Circuit menu", () => { Game.Quit(); Open(); }, null));
                    break;
                case Page.Pause:
                    _items.Add(("Resume", () => { Close(); Game.Paused = false; Time.timeScale = GcBoot.Speed; }, null));
                    _items.Add(("Restart the race", () => { Close(); Time.timeScale = GcBoot.Speed; StartCoroutine(Restart()); }, null));
                    _items.Add(("Quit to the Grand Circuit menu", () => { Time.timeScale = GcBoot.Speed; Game.Quit(); Open(); }, null));
                    break;
                case Page.Records:
                    _items.Add(("Back", () => Show(Page.Title), null));
                    break;
            }
            if (p == Page.Track) { _flyS = 0f; }
            Draw();
            AudioKit.Play("menu_move", null, 0.4f);
        }
        static string SplitLabel => "Split screen:  " + (GcGame.SideBySide ? "side by side" : "top / bottom");
        static string TrackLabel(int k) { var id = GcTrack.Ids[k]; float b = GcSave.Best(id, GcGame.NextMode == GcGame.Mode.TimeTrial); return GcTrack.Names[k] + (b > 0f ? "    best " + GcGame.Fmt(b) : ""); }
        bool AllLocked() { for (int i = 0; i < Players; i++) if (!_locked[i]) return false; return true; }
        void Relabel(int i, string s) { if (i < _items.Count) { var it = _items[i]; _items[i] = (s, it.act, it.adjust); } Draw(); }
        public void Close() { Current = Page.Off; if (_canvas != null) Destroy(_canvas); if (_cam != null) _cam.gameObject.SetActive(false); }

        void StartRace(int track)
        {
            GcSave.LastTrack = track; GcSave.LastRacer = Pick[0]; GcSave.Save();
            GcGame.NextTrack = GcTrack.Ids[track];
            GcGame.NextRacers = Enumerable.Range(0, Players).Select(i => Pick[i]).ToArray();
            GcGame.NextPads = Enumerable.Range(0, Players).ToArray();
            GcPads.Device[0] = Pads[0]; GcPads.Device[1] = Pads[1];
            AudioKit.Play("menu_confirm", null, 0.6f);
            Close();
            StartCoroutine(RaceThenResults());
        }
        IEnumerator Restart() { Game.Quit(); yield return null; yield return RaceThenResults(); }
        IEnumerator RaceThenResults()
        {
            yield return Game.RaceRun(TestControls);
            Show(Page.Results);
        }
        /// <summary>The test bot's hook: a run that drives the human slots through virtual pads sets this.</summary>
        public System.Func<GcRacer, int, GcControls> TestControls;

        // ------------------------------------------------------------------------------------------ drawing
        void Draw()
        {
            if (_canvas != null) Destroy(_canvas);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var cam = MenuCam(); cam.gameObject.SetActive(true); { var al = cam.GetComponent<AudioListener>(); if (al != null) al.enabled = Game.Cams.Count == 0; }
            _canvas = new GameObject("GcMenuCanvas"); var c = _canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 0.6f;
            var sc = _canvas.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1280, 720); sc.matchWidthOrHeight = 1f;
            var dim = Img(_canvas.transform, Vector2.zero, new Vector2(4000, 4000), new Color(0f, 0f, 0f, Current == Page.Pause || Current == Page.Results ? 0.6f : 0.48f));
            var white = new Color(0.95f, 0.95f, 0.95f);
            Img(_canvas.transform, new Vector2(0, 290), new Vector2(620, 58), new Color(0f, 0f, 0f, 0.5f));   // the title on its own soft dark panel
            _title = Txt(_canvas.transform, Current == Page.Title ? "GRAND CIRCUIT" : Current == Page.Join ? "PLAYERS" : Current == Page.Racer ? "CHOOSE YOUR RACER" : Current == Page.Track ? "CHOOSE THE TRACK" : Current == Page.Results ? "RESULTS" : Current == Page.Records ? "BEST TIMES" : "PAUSED", new Vector2(0, 290), 40, white);
            _title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
            if (Current == Page.Title) { Img(_canvas.transform, new Vector2(0, 248), new Vector2(460, 30), new Color(0f, 0f, 0f, 0.5f)); _sub = Txt(_canvas.transform, "an anti-grav race through the worlds", new Vector2(0, 248), 20, new Color(0.85f, 0.85f, 0.85f)); }
            if (Current == Page.Join) JoinPanel();
            if (Current == Page.Racer) RacerPanel();
            if (Current == Page.Results) ResultsPanel();
            if (Current == Page.Records) RecordsPanel();
            _rows.Clear(); _bars.Clear();
            float top = Current == Page.Title ? 120f : Current == Page.Track ? 80f : -170f; if (Current == Page.Results) top = -200f; if (Current == Page.Records) top = -250f;
            for (int i = 0; i < _items.Count; i++)
            {
                float y = top - i * 50f; if (Current == Page.Racer || Current == Page.Join || Current == Page.Results) y = top - i * 46f; if (Current == Page.Track) y = top + 60f - i * 46f;
                var bar = Img(_canvas.transform, new Vector2(0, y), new Vector2(560, 42), new Color(0f, 0f, 0f, 0.45f)); _bars.Add(bar);
                var t = Txt(bar.transform, _items[i].label, Vector2.zero, 26, white); _rows.Add(t);
            }
            Img(_canvas.transform, new Vector2(0, -320), new Vector2(980, 28), new Color(0f, 0f, 0f, 0.5f));
            _hint = Txt(_canvas.transform, Hint, new Vector2(0, -320), 17, new Color(0.85f, 0.85f, 0.85f));
            Refresh();
        }
        string Hint => Current == Page.Join ? "Player 2: press A on your controller to join  ·  B on that controller to leave  ·  A = continue  ·  B = back"
                     : Current == Page.Racer ? (Players > 1 ? "Each player: left / right to choose, A to lock in  ·  B = back" : "Left / right to choose  ·  A = ready  ·  B = back")
                     : Current == Page.Title ? "Up / down + A  ·  left / right changes the split-screen layout" : "Up / down + A  ·  B = back";
        void Refresh()
        {
            for (int i = 0; i < _rows.Count; i++) { bool s = i == _sel; _bars[i].color = s ? new Color(0.92f, 0.92f, 0.92f, 0.92f) : new Color(0f, 0f, 0f, 0.45f); _rows[i].color = s ? new Color(0.06f, 0.06f, 0.06f) : new Color(0.95f, 0.95f, 0.95f); }
            if (Current == Page.Racer) RacerText();
            if (Current == Page.Join && _racerText[1] != null) { _racerText[0].text = "PLAYER 1\n\nready\n" + PadName(Pads[0], true); _racerText[1].text = Pads[1] != null ? "PLAYER 2\n\nready\n" + PadName(Pads[1], false) : "PLAYER 2\n\npress A to join"; }
        }
        static string PadName(Gamepad g, bool first) => g != null || first ? "(controller)" : "";

        void JoinPanel()
        {
            for (int i = 0; i < 2; i++)
            {
                var p = Img(_canvas.transform, new Vector2(i == 0 ? -220 : 220, 60), new Vector2(380, 240), new Color(0f, 0f, 0f, 0.5f));
                _racerText[i] = Txt(p.transform, "", Vector2.zero, 26, Color.white); ((RectTransform)_racerText[i].transform).sizeDelta = new Vector2(360, 220);
            }
        }
        void RacerPanel()
        {
            for (int i = 0; i < Players; i++)
            {
                float x = Players == 1 ? 0f : (i == 0 ? -250f : 250f);
                var p = Img(_canvas.transform, new Vector2(x, 70), new Vector2(Players == 1 ? 560 : 460, 300), new Color(0f, 0f, 0f, 0.5f));
                _racerText[i] = Txt(p.transform, "", new Vector2(0, 0), 22, Color.white); ((RectTransform)_racerText[i].transform).sizeDelta = new Vector2(440, 290);
                _racerText[i].alignment = TextAnchor.MiddleCenter;
            }
        }
        /// <summary>Round 21 (one of each racer per race): is this racer locked in by the other player?</summary>
        bool TakenByOther(int player, int racer) { int o = 1 - player; return Players > 1 && o >= 0 && o < Players && _locked[o] && Pick[o] == racer; }
        /// <summary>The next racer in that direction the other player has not locked in.</summary>
        int NextFree(int player, int from, int dir) { int n = GcStats.All.Length, k = from; for (int i = 0; i < n; i++) { k = (k + dir + n) % n; if (!TakenByOther(player, k)) return k; } return from; }
        static string Bar(float v) { int n = Mathf.Clamp(Mathf.RoundToInt(v * 10f), 1, 10); return new string('|', n * 2) + new string('.', (10 - n) * 2); }
        void RacerText()
        {
            for (int i = 0; i < Players; i++)
            {
                if (_racerText[i] == null) continue; var s = GcStats.Of(Pick[i]);
                bool taken = TakenByOther(i, Pick[i]);
                _racerText[i].text = (Players > 1 ? $"PLAYER {i + 1}\n" : "") + $"<   {s.Racer.ToUpperInvariant()}   >\n{s.Vehicle}\n\n" +
                    $"Top speed  {Bar((s.Top - 44f) / 10f)}\nAcceleration  {Bar((s.Accel - 8f) / 10f)}\nHandling  {Bar((s.Handling - 0.6f) / 0.5f)}\nWeight  {Bar((s.Weight - 0.4f) / 1.4f)}\n\n" + (_locked[i] ? "LOCKED IN" : taken ? $"TAKEN BY PLAYER {2 - i}" : "A to lock in");
                _racerText[i].color = taken && !_locked[i] ? new Color(0.5f, 0.5f, 0.5f) : Color.white;   // Round 21: a racer the other player has is greyed out
            }
        }
        void ResultsPanel()
        {
            var res = Game.Results ?? new List<GcRacer>();
            var lines = res.Select(r => $"{r.Place}.   {r.Label}{(r.Human ? "  (Player " + (Game.Humans.IndexOf(r) + 1) + ")" : "")}      {GcGame.Fmt(r.FinishTime)}{(r.Finished ? "" : "*")}      best lap {GcGame.Fmt(r.BestLap)}");
            var p = Img(_canvas.transform, new Vector2(0, 40), new Vector2(760, 420), new Color(0f, 0f, 0f, 0.55f));   // Round 22: seven rows
            var t = Txt(p.transform, string.Join("\n", lines) + (Game.Track != null ? $"\n\n{Game.Track.Name}   ·   best race {GcGame.Fmt(GcSave.Best(Game.Track.Id, GcGame.NextMode == GcGame.Mode.TimeTrial))}   ·   best lap {GcGame.Fmt(GcSave.BestLap(Game.Track.Id))}" : ""), Vector2.zero, 22, Color.white);
            ((RectTransform)t.transform).sizeDelta = new Vector2(740, 410);
        }
        void RecordsPanel()
        {
            var sb = new System.Text.StringBuilder();
            for (int k = 0; k < GcTrack.Ids.Length; k++)
            {
                var id = GcTrack.Ids[k];
                sb.AppendLine($"{GcTrack.Names[k].ToUpperInvariant()}   race {GcGame.Fmt(GcSave.Best(id, false))}  ·  time trial {GcGame.Fmt(GcSave.Best(id, true))}  ·  lap {GcGame.Fmt(GcSave.BestLap(id))}");
                // Round 22: seven racers - two lines (the first four, then the rest)
                sb.AppendLine(string.Join("   ·   ", GcStats.All.Take(4).Select(s => $"{s.Racer} {GcGame.Fmt(GcSave.BestBy(id, s.Racer))}")));
                sb.AppendLine(string.Join("   ·   ", GcStats.All.Skip(4).Select(s => $"{s.Racer} {GcGame.Fmt(GcSave.BestBy(id, s.Racer))}")));
            }
            var p = Img(_canvas.transform, new Vector2(0, 30), new Vector2(1000, 470), new Color(0f, 0f, 0f, 0.55f));
            var t = Txt(p.transform, sb.ToString(), Vector2.zero, 16, Color.white); ((RectTransform)t.transform).sizeDelta = new Vector2(980, 460);
        }

        Image Img(Transform parent, Vector2 pos, Vector2 size, Color c)
        {
            var go = new GameObject("Panel", typeof(RectTransform)); go.transform.SetParent(parent, false); var rt = (RectTransform)go.transform; rt.anchoredPosition = pos; rt.sizeDelta = size;
            var im = go.AddComponent<Image>(); im.color = c; im.raycastTarget = false; return im;
        }
        Text Txt(Transform parent, string s, Vector2 pos, int size, Color c)
        {
            var go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(parent, false); var rt = (RectTransform)go.transform; rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(1100, size * 1.6f);
            var t = go.AddComponent<Text>(); t.font = _font; t.fontSize = size; t.text = s; t.color = c; t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false; t.supportRichText = false;
            return t;
        }

        // ------------------------------------------------------------------------------------------ input
        void Update()
        {
            // the pause page during a race
            if (Current == Page.Off && Game.Running && !Game.Paused && GcPads.PausePressed(Game.Humans.Count)) { Game.Paused = true; Time.timeScale = 0f; Show(Page.Pause); return; }
            if (Current == Page.Off) return;
            FlyCam();
            if (Current == Page.Join) { JoinInput(); }
            if (Current == Page.Racer) { RacerInput(); if (Current != Page.Racer) return; }   // (the last lock-in opened the track page: not also a press there)
            int dir = 0; bool ok = false, back = false; int lr = 0;
            foreach (var p in Gamepad.all)
            {
                if (Current == Page.Racer && Players > 1) break;   // each player's own pad on the racer page (RacerInput)
                if (p.dpad.up.wasPressedThisFrame) dir = -1; if (p.dpad.down.wasPressedThisFrame) dir = 1;
                if (p.dpad.left.wasPressedThisFrame) lr = -1; if (p.dpad.right.wasPressedThisFrame) lr = 1;
                float y = p.leftStick.y.ReadValue(), x = p.leftStick.x.ReadValue();
                if (Time.unscaledTime > _stickReady) { if (y > 0.6f) { dir = -1; _stickReady = Time.unscaledTime + 0.25f; } else if (y < -0.6f) { dir = 1; _stickReady = Time.unscaledTime + 0.25f; } else if (x > 0.6f) { lr = 1; _stickReady = Time.unscaledTime + 0.25f; } else if (x < -0.6f) { lr = -1; _stickReady = Time.unscaledTime + 0.25f; } }
                if (p.buttonSouth.wasPressedThisFrame && (Current != Page.Join || p == Pads[0] || Pads[0] == null)) { ok = true; if (Current == Page.Title) Pads[0] = p; }
                if (p.buttonEast.wasPressedThisFrame && (Current != Page.Join || p != Pads[1])) back = true;
            }
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) dir = -1; if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) dir = 1;
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) lr = -1; if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) lr = 1;
                if (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) { ok = true; if (Current == Page.Title) Pads[0] = null; }
                if (k.escapeKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame) back = true;
            }
            if (Current == Page.Racer && Players == 1 && lr != 0) { int n = GcStats.All.Length; Pick[0] = (Pick[0] + lr + n) % n; AudioKit.Play("menu_move", null, 0.4f); Refresh(); lr = 0; }
            if (dir != 0 && _items.Count > 0) { _sel = (_sel + dir + _items.Count) % _items.Count; AudioKit.Play("menu_move", null, 0.35f); Refresh(); }
            if (lr != 0 && _sel < _items.Count && _items[_sel].adjust != null) { _items[_sel].adjust(lr); }
            if (ok && _sel < _items.Count) { AudioKit.Play("menu_confirm", null, 0.5f); _items[_sel].act(); }
            else if (back && Current != Page.Title && Current != Page.Results)
            {
                var b = _items.FindIndex(i => i.label == "Back"); if (b >= 0) _items[b].act(); else if (Current == Page.Pause) _items[0].act();
            }
        }
        void JoinInput()
        {
            foreach (var p in Gamepad.all)
            {
                if (p == Pads[0]) continue;
                if (p.buttonSouth.wasPressedThisFrame && Pads[1] == null) { Pads[1] = p; Players = 2; AudioKit.Play("menu_confirm", null, 0.6f); Refresh(); GcGame.Log("GC JOIN (R20): player 2 joined (a second controller pressed A)"); }
                else if (p.buttonEast.wasPressedThisFrame && Pads[1] == p) { Pads[1] = null; Players = 1; Refresh(); }
            }
        }
        /// <summary>Test hook: player 2 joins from the given pad (as if it pressed A).</summary>
        public void Join(Gamepad p) { Pads[1] = p; Players = 2; Refresh(); GcGame.Log("GC JOIN (R20): player 2 joined (a second controller pressed A)"); }
        void RacerInput()
        {
            if (Players < 2) { if (Gamepad.all.Any(p => p.buttonSouth.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)) _locked[0] = true; return; }
            for (int i = 0; i < 2; i++)
            {
                var p = Pads[i]; if (p == null && i == 1) continue;
                bool l = p != null ? p.dpad.left.wasPressedThisFrame : Keyboard.current != null && Keyboard.current.leftArrowKey.wasPressedThisFrame;
                bool r = p != null ? p.dpad.right.wasPressedThisFrame : Keyboard.current != null && Keyboard.current.rightArrowKey.wasPressedThisFrame;
                bool a = p != null ? p.buttonSouth.wasPressedThisFrame : Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;
                if (!_locked[i] && (l || r)) { Pick[i] = NextFree(i, Pick[i], r ? 1 : -1); AudioKit.Play("menu_move", null, 0.4f); Refresh(); }
                if (a && !_locked[i])
                {
                    if (TakenByOther(i, Pick[i])) { AudioKit.Play("menu_move", null, 0.5f); continue; }   // taken: never two of the same racer
                    _locked[i] = true;
                    int o = 1 - i; if (o < Players && !_locked[o] && Pick[o] == Pick[i]) Pick[o] = NextFree(o, Pick[o], 1);   // the other player moves off it
                    Refresh(); AudioKit.Play("menu_confirm", null, 0.5f); if (AllLocked()) Show(Page.Track);
                }
            }
        }
        /// <summary>Test hook: a player's racer choice.</summary>
        public void Choose(int player, int racer) { if (TakenByOther(player, racer)) racer = NextFree(player, racer, 1); Pick[player] = racer; _locked[player] = true; Refresh(); }
        /// <summary>Test hook (Round 21): highlight a row (the track page's tracks are rows 0..6).</summary>
        public void Select(int i) { _sel = Mathf.Clamp(i, 0, Mathf.Max(0, _items.Count - 1)); Refresh(); }
        /// <summary>Test hook (Round 21): is that racer shown as taken for that player right now?</summary>
        public bool IsTakenFor(int player, int racer) => TakenByOther(player, racer);

        void FlyCam()
        {
            if (_cam == null || !_cam.gameObject.activeSelf || Game.Track == null) return;
            if (Current == Page.Track) { var id = GcTrack.Ids[Mathf.Clamp(_sel, 0, GcTrack.Ids.Length - 1)]; if (id != Game.Track.Id) { Game.BuildTrack(id); _flyS = 0f; } }
            _flyS += Time.unscaledDeltaTime * 22f;
            var t = Game.Track; t.Frame(_flyS, out var p, out var r, out _);
            t.Frame(_flyS + 40f, out var p2, out _, out _);
            var up = Vector3.up; var side = r * Vector3.right; side.y = 0f; side.Normalize(); var pos = p + up * 24f - side * 30f;   // high and wide: clear of the sets
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, pos, 0.05f);
            _cam.transform.rotation = Quaternion.Slerp(_cam.transform.rotation, Quaternion.LookRotation(p2 - _cam.transform.position, Vector3.up), 0.05f);
        }
    }
}
