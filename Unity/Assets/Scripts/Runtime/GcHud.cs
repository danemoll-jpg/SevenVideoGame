using UnityEngine;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>Round 20: one player's HUD on that player's camera (Screen Space - Camera: it fills that camera's viewport, so split
    /// screen gets one per half): position, lap, race time, speed, the held pickup with its button, the countdown, the finish card.
    /// Monochrome, every text on a soft dark panel with a shadow (ChapterUI's rule). Controller buttons only.</summary>
    public class GcHud : MonoBehaviour
    {
        public GcGame Game; public GcRacer R; Text _pos, _lap, _time, _speed, _item, _center, _note, _subs; Image _itemBox; int _players;
        float _noteUntil; Font _font;
        public static GcHud Make(Camera cam, GcGame game, GcRacer r, int players)
        {
            var go = new GameObject("Hud_" + cam.name);
            var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 0.5f;
            var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1280, 720); sc.matchWidthOrHeight = players > 1 ? 0.5f : 1f;
            var h = go.AddComponent<GcHud>(); h.Game = game; h.R = r; h._players = players; h.Build();
            r.Event += h.OnEvent;
            return h;
        }
        Text T(string name, Vector2 anchor, Vector2 pos, int size, TextAnchor al, Vector2 box, bool panel = true)
        {
            var root = new GameObject(name, typeof(RectTransform)); root.transform.SetParent(transform, false);
            var rt = (RectTransform)root.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = pos; rt.sizeDelta = box;
            if (panel) { var im = root.AddComponent<Image>(); im.color = new Color(0f, 0f, 0f, 0.42f); im.raycastTarget = false; }
            var tg = new GameObject("Text", typeof(RectTransform)); tg.transform.SetParent(root.transform, false); var tr = (RectTransform)tg.transform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(10, 2); tr.offsetMax = new Vector2(-10, -2);
            var t = tg.AddComponent<Text>(); t.font = _font; t.fontSize = size; t.alignment = al; t.color = new Color(0.96f, 0.96f, 0.96f); t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var sh = tg.AddComponent<Shadow>(); sh.effectColor = new Color(0f, 0f, 0f, 0.8f); sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }
        void Build()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _pos = T("Position", new Vector2(0f, 1f), new Vector2(18f, -14f), 46, TextAnchor.MiddleLeft, new Vector2(190f, 64f));
            _lap = T("Lap", new Vector2(1f, 1f), new Vector2(-18f, -14f), 28, TextAnchor.MiddleRight, new Vector2(220f, 44f));
            _time = T("Time", new Vector2(1f, 1f), new Vector2(-18f, -64f), 22, TextAnchor.MiddleRight, new Vector2(220f, 34f));
            _speed = T("Speed", new Vector2(1f, 0f), new Vector2(-18f, 16f), 30, TextAnchor.MiddleRight, new Vector2(200f, 46f));
            _item = T("Item", new Vector2(0f, 0f), new Vector2(18f, 16f), 24, TextAnchor.MiddleLeft, new Vector2(250f, 46f));
            _itemBox = _item.transform.parent.GetComponent<Image>();
            _center = T("Center", new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), 72, TextAnchor.MiddleCenter, new Vector2(520f, 110f));
            _subs = T("Subtitle", new Vector2(0.5f, 0f), new Vector2(0f, 76f), 22, TextAnchor.MiddleCenter, new Vector2(640f, 38f));
            _note = T("Note", new Vector2(0.5f, 1f), new Vector2(0f, -16f), 22, TextAnchor.MiddleCenter, new Vector2(460f, 38f));
        }
        void OnEvent(GcRacer r, string e)
        {
            if (e == "lap" && r.Lap > 1 && r.Lap <= Game.Laps) Note(r.Lap == Game.Laps ? "Final lap" : $"Lap {r.Lap}   {GcGame.Fmt(r.LastLap)}", 2.5f);
            else if (e == "shortcut") Note("Shortcut", 1.6f);
            else if (e == "pushed") Note("Pushed!", 1.2f);
            else if (e == "shielded") Note("Shield held", 1.2f);
            else if (e.StartsWith("fall:")) Note("Back on the track", 1.6f);
        }
        void Note(string s, float t) { _note.text = s; _noteUntil = Time.time + t; }
        static string Ord(int n) => n + (n % 100 >= 11 && n % 100 <= 13 ? "th" : n % 10 == 1 ? "st" : n % 10 == 2 ? "nd" : n % 10 == 3 ? "rd" : "th");
        void LateUpdate()
        {
            if (R == null || Game == null) return;
            bool trial = Game.Racers.Count == 1;
            _pos.text = trial ? "TIME TRIAL" : $"{Ord(R.Place)} / {Game.Racers.Count}"; _pos.fontSize = trial ? 26 : 46;
            int lap = Mathf.Clamp(R.Lap, 1, Game.Laps);
            _lap.text = R.Finished ? "Finished" : $"Lap {lap} / {Game.Laps}";
            float t = R.Finished ? R.FinishTime : Game.Running ? Time.time - Game.RaceT0 : 0f;
            _time.text = GcGame.Fmt(t) + (R.BestLap > 0f ? $"   best {GcGame.Fmt(R.BestLap)}" : "");
            _speed.text = $"{Mathf.RoundToInt(R.V * 3.6f)} km/h";
            bool has = R.Item != GcPickup.None;
            _item.text = has ? $"{R.Item.ToString().ToUpperInvariant()}   X" : "";
            _itemBox.enabled = has;
            string c = Game.Countdown > 0 ? Game.Countdown.ToString() : Game.Countdown == 0 ? "GO" : R.Finished && !trial ? $"{Ord(R.Place)} place" : R.Finished ? GcGame.Fmt(R.FinishTime) : R.RespawnT > 0f ? "" : "";
            _center.text = c; _center.transform.parent.gameObject.SetActive(c.Length > 0);
            bool sub = Time.time < GcVoice.SubtitleUntil && GcVoice.LastSubtitle != null; _subs.text = sub ? GcVoice.LastSubtitle : ""; _subs.transform.parent.gameObject.SetActive(sub);
            bool note = Time.time < _noteUntil; _note.transform.parent.gameObject.SetActive(note);
        }
    }
}
