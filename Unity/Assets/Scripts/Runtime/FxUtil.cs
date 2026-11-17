using UnityEngine;

namespace Seven
{
    /// <summary>Small procedural textures for white-only effects.</summary>
    public static class FxUtil
    {
        static Texture2D _dot;

        /// <summary>Soft round dot (gaussian), white on black.</summary>
        public static Texture2D Dot(int n)
        {
            if (_dot != null) return _dot;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float r2 = dx * dx + dy * dy;
                    float v = Mathf.Exp(-r2 * 5f) * Mathf.Clamp01(1f - r2);
                    t.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            t.Apply();
            return _dot = t;
        }
    }
}
