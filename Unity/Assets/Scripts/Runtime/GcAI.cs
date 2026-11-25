using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 20: the AI driver (also the test bot that plays a human slot through a virtual pad). It rides a racing line (toward the
    /// inside of the bends ahead, wide on the way in), steers with a feed-forward for the bend plus a correction to its line, uses the
    /// air-brakes in tight bends, takes the shortcut some of the time, uses its pickups (boost on a straight, push when someone is close
    /// ahead, shield when someone is close behind) and avoids the racer just ahead. Mild rubber band: a little faster when far behind the
    /// best human, a little slower when far ahead. Skill 0..1 (a good player beats skill 0.85).
    /// </summary>
    public class GcAI
    {
        public float Skill = 0.85f; public float ShortcutChance = 0.5f;
        readonly System.Random _rnd; bool _takeShortcut; int _shortcutLap = -1; float _itemHeld, _wobbleT, _wobble;
        public GcAI(int seed, float skill) { _rnd = new System.Random(seed); Skill = skill; }

        public GcControls Drive(GcRacer me, List<GcRacer> all, float dt)
        {
            var c = new GcControls { Throttle = 1f };
            if (me.Air) { c.Steer = 0f; return c; }
            var t = me.On; float v = Mathf.Max(me.V, 8f);
            t.Frame(me.S, out _, out _, out float hw);
            // the racing line: toward the inside of the bend 25-45 m ahead
            float look = Mathf.Clamp(v * 0.7f, 18f, 40f);
            float cAhead = (t.Curv(me.S + look * 0.6f) + t.Curv(me.S + look)) * 0.5f;
            float lineX = Mathf.Clamp(cAhead * 90f, -1f, 1f) * (hw - 2.2f);
            // the shortcut: decided once per lap, a little before the fork: ride the fork side
            var b = me.Main.Branch;
            if (b != null && me.On == me.Main)
            {
                if (_shortcutLap != me.Lap) { _shortcutLap = me.Lap; _takeShortcut = _rnd.NextDouble() < ShortcutChance; }
                float toFork = me.Main.Ahead(me.S, b.JoinFrom);
                if (_takeShortcut && toFork < 90f) lineX = b.ForkSide * (me.Main.HalfW[me.Main.Idx(b.JoinFrom)] - 2f);
                else if (!_takeShortcut && toFork < 40f) lineX = -b.ForkSide * 2f;
            }
            // avoid the racer just ahead on the same line
            foreach (var o in all)
            {
                if (o == me || o.On != me.On || o.Air) continue;
                float d = o.S - me.S; if (t.Closed) { float L = t.Length; if (d < -L * 0.5f) d += L; if (d > L * 0.5f) d -= L; }
                if (d > 0f && d < 14f && Mathf.Abs(o.X - lineX) < 2.4f && me.V > o.V - 1f) lineX = o.X + (o.X > 0f ? -2.8f : 2.8f);
            }
            // a little human wobble at lower skill
            _wobbleT -= dt; if (_wobbleT <= 0f) { _wobbleT = 0.6f + (float)_rnd.NextDouble(); _wobble = ((float)_rnd.NextDouble() - 0.5f) * (1f - Skill) * 3f; }
            lineX = Mathf.Clamp(lineX + _wobble, -(hw - 1.4f), hw - 1.4f);
            // steering: feed-forward for the bend under it + a heading toward the line
            float rate = 62f * me.Stats.Handling * (1f - 0.35f * Mathf.Clamp01(me.V / 60f));
            float need = t.Curv(me.S + v * 0.12f) * me.V;                       // deg/s the bend turns the track
            float aim = Mathf.Atan2(lineX - me.X, Mathf.Max(10f, v * 0.9f)) * Mathf.Rad2Deg;
            float psiErr = aim - me.Psi;
            float want = need + psiErr * 3.2f;
            // beyond what the stick alone turns: the air-brake on that side, the stick trimming the rest (continuous, no hunting)
            float air = 48f * me.Stats.Handling;
            if (want > rate * 0.95f) c.AirR = true; else if (want < -rate * 0.95f) c.AirL = true;
            c.Steer = Mathf.Clamp((want - (c.AirR ? air : 0f) + (c.AirL ? air : 0f)) / rate, -1f, 1f);
            // brake only when a bend is far beyond what steering + air-brakes can take at this speed
            float max = rate + 48f * me.Stats.Handling;
            if (Mathf.Abs(need) > max * 1.08f) { c.Throttle = 0.2f; c.Brake = 0.5f; }
            // pickups
            if (me.Item != GcPickup.None)
            {
                _itemHeld += dt;
                bool straight = Mathf.Abs(t.Curv(me.S + 30f)) < 0.15f;
                GcRacer ahead = null, behind = null; float da = 999f, db = 999f;
                foreach (var o in all) { if (o == me) continue; float d = o.Distance - me.Distance; if (d > 0f && d < da) { da = d; ahead = o; } if (d < 0f && -d < db) { db = -d; behind = o; } }
                if (me.Item == GcPickup.Boost && (straight || _itemHeld > 4f)) c.Use = true;
                if (me.Item == GcPickup.Push && (da < 35f || _itemHeld > 8f)) c.Use = true;
                if (me.Item == GcPickup.Shield && (db < 12f || _itemHeld > 7f)) c.Use = true;
                if (c.Use) _itemHeld = 0f;
            }
            return c;
        }
    }
}
