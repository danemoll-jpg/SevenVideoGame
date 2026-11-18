using UnityEngine;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// Round 10 (Dan): the game logo - "7" + THE OBSERVER in Arial Bold, as an image (Resources/Logo/
    /// logo_7_observer_white.png, made by Tools/make_logo.py; Arial's font file is never shipped). UI text keeps
    /// Unity's built-in LegacyRuntime font (Arial-metric); Liberation Sans Bold (OFL) is the fallback if a font must ship.
    /// </summary>
    public static class Logo
    {
        public const string Resource = "Logo/logo_7_observer_white";
        static Texture2D _tex;
        public static Texture2D Texture => _tex != null ? _tex : (_tex = Resources.Load<Texture2D>(Resource));

        /// <summary>Add the logo to a uGUI canvas (reference 1280x720), centred at pos, the given width; null if missing.</summary>
        public static RawImage Add(Transform parent, Vector2 pos, float width, Color tint)
        {
            var tex = Texture;
            if (tex == null) return null;
            var go = new GameObject("Logo", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(width, width * tex.height / tex.width);
            var img = go.AddComponent<RawImage>();
            img.texture = tex; img.color = tint; img.raycastTarget = false;
            return img;
        }
    }
}
