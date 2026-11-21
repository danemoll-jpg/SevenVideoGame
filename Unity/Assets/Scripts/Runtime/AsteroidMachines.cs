using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    // =====================================================================================================================
    // Round 16 — the Great Machine Age: the asteroid's machinery at runtime. Everything code-built by FightDemoBuilderAsteroid
    // (riveted, steam-age: wheels, pistons, rails), every piece readable and with clear feedback when it works. Strictly
    // monochrome (glow = white emission). Nothing is ever destroyed: machines are disabled, trapped or switched off.
    // =====================================================================================================================
    /// <summary>The city's production rate: 1 = every line running; preservation mode (AST_09) eases it to a stop - only the
    /// machinery marked essential keeps going (the stabilizers, the essential lights).</summary>
    public static class AstProduction
    {
        public static float Rate = 1f;
        public static float Target = 1f;
        public static void Tick(float dt) => Rate = Mathf.MoveTowards(Rate, Target, dt * 0.12f);
        public static float For(bool essential) => essential ? 1f : Rate;
    }
    /// <summary>Emission helper (white only).</summary>
    public static class Glowing
    {
        public static void Set(Renderer r, float v) { if (r == null) return; var m = r.material; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * v); }
    }
}
