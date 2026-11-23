using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using L = Seven.FinaleLayout;

namespace Seven
{
    /// <summary>QF30 — FIN_09 restaged (the danger, "Seven!", the leap, the cut on the impact), FIN_10 (he dies large in her arms and shrinks
    /// as the life leaves him; "Panther, no!"), FIN_11 (she carries him, lays him on the archive, the Scree take Tunneler, the Archive
    /// asks), FIN_13 (her ascent into the light). Plus the helpers: the rune shots, the barrier set-up, Panther's states, the restraints.</summary>
    public partial class FinaleDirector
    {
        // ------------------------------------------------------------------------------------------ item 2: every placement framed clear
        /// <summary>The first framing that sees her hands, the rune and its socket with nothing in between (geometry, another socket,
        /// the platform). Returns where the camera is.</summary>
        Vector3 RuneShot(int i, Vector3 stand, Vector3 toS)
        {
            var side = Vector3.Cross(Vector3.up, toS); var top = L.SocketTop(i); var hands = stand + Vector3.up * 1.05f + toS * 0.32f;
            var look = Vector3.Lerp(hands, top, 0.5f) + Vector3.up * 0.05f;
            int n = 0;
            foreach (var sg in new[] { 1f, -1f })
                foreach (var (back, off, h) in new[] { (0.1f, 2.2f, 1.35f), (0.5f, 2.1f, 1.5f), (-0.4f, 2.0f, 1.3f), (0.9f, 1.9f, 1.6f), (1.2f, 1.6f, 1.8f), (1.6f, 1.2f, 2.1f) })   // from her side first: her hands and the rune never behind her body
                {
                    n++;
                    var cam = stand - toS * back + side * sg * off + Vector3.up * h;
                    if (!ChapterCamera.ClearLine(cam, hands) || !ChapterCamera.ClearLine(cam, top) || !ChapterCamera.ClearLine(cam, top + Vector3.up * 0.2f)) continue;
                    bool blocked = false;
                    for (int j = 0; j < 7 && !blocked; j++) if (j != i && SegDist(cam, top, L.SocketTop(j) - Vector3.up * 0.4f) < 0.7f) blocked = true;
                    if (SegDist(cam, top, L.PlatformAt + Vector3.up * 0.5f) < L.PlatformR + 0.4f) blocked = true;
                    if (blocked) continue;
                    Shoot(cam, look, 42f, 0.01f);
                    Log($"RUNE SHOT (QF30 2) rune {i + 1} ({L.SocketWorlds[i]}): her hands, the {L.SocketRunes[i]} and its socket in clear view - framing {n} of 12");
                    return cam;
                }
            var fb = stand - toS * 1.2f + side * 1.4f + Vector3.up * 2.6f; Shoot(fb, look, 42f, 0.01f);
            Log($"RUNE SHOT (QF30 2) rune {i + 1}: NO clear framing among 12 - the high fallback");
            return fb;
        }
        static float SegDist(Vector3 a, Vector3 b, Vector3 p) { var A = Flat(a); var B = Flat(b); var P = Flat(p); var ab = B - A; float t = Mathf.Clamp01(Vector3.Dot(P - A, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude)); return (P - (A + ab * t)).magnitude; }

        /// <summary>QF30 (10): FIN_05_04a-c - Panther looks at the old maintenance barrier's frame across the side bay; it is on screen as
        /// they speak of it (over her shoulder toward the frame, the frame's lights faintly awake).</summary>
        IEnumerator BarrierSetUp()
        {
            if (Cat != null) { Cat.Hold(L.BarrierAt + Vector3.up * 1.5f, "sit"); var ov = Cat.GetComponent<CatPoseOverlay>(); if (ov != null) ov.LookAt = L.BarrierAt + Vector3.up * 2f; }
            var toB = Flat(L.BarrierAt - P7).normalized; var side = Vector3.Cross(Vector3.up, toB);
            Shoot(P7 - toB * 1.9f + side * 0.7f + Vector3.up * 1.75f, L.BarrierAt + Vector3.up * 2.0f, 46f, 0.01f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f) { Glow(BarrierFrameLines, Mathf.Lerp(0.08f, 0.55f, t)); yield return null; }
            ImageGate.ExpectPoint(L.BarrierAt + Vector3.up * 2.2f, "the old maintenance barrier's frame across the side bay");
            yield return Shot("f22b_the_barrier_frame_across_the_side_bay");
            yield return DialogueFraming.Over("the barrier's frame across the side bay (QF24 7)", SayIds("FIN_05_04a", "FIN_05_04b", "FIN_05_04c"));
            Glow(BarrierFrameLines, 0.15f);
            if (Cat != null) { var ov = Cat.GetComponent<CatPoseOverlay>(); if (ov != null) ov.LookAt = null; }
            Log("QF30 (10): the barrier set up in the story before it is needed (FIN_05_04a-c, the frame shown as it is mentioned)");
        }

        /// <summary>A drill boring through the chamber wall, framed from inside the chamber (the bit, the broken stone, the dust).</summary>
        void DrillShot(Transform bit, bool overHer = false)
        {
            var into = Flat(bit.forward).normalized; var side = Vector3.Cross(Vector3.up, into);
            if (overHer) { var to = Flat(bit.position - P7).normalized; var sd = Vector3.Cross(Vector3.up, to); Shoot(P7 - to * 1.5f + sd * 0.55f + Vector3.up * 1.75f, bit.position + Vector3.down * 0.6f, 50f, 0.01f); return; }
            var cam = bit.position + into * 8.5f + side * 3.0f; cam.y = L.Y + 1.5f;                          // from the chamber floor: the floor's lit conduits, the wall, the drill
            Shoot(cam, bit.position + into * 1.2f + Vector3.down * 1.2f, 56f, 0.01f);
        }

        // ------------------------------------------------------------------------------------------ Panther's states (items 13-15)
        const float LargeScale = 2.7f;
        CatPoseOverlay CatOv => Cat != null ? Cat.GetComponent<CatPoseOverlay>() : null;
        /// <summary>Lying on his side (the lie-down pose rolled over), head toward +x, at <paramref name="at"/>, scale <paramref name="scale"/>.</summary>
        void LayCat(Vector3 at, Vector3 headDir, float scale, float floorY)
        {
            if (Cat == null) return;
            Cat.gameObject.SetActive(true); Cat.Scripted("lie_down"); Cat.enabled = false;
            Cat.transform.localScale = Vector3.one * scale;
            Cat.transform.SetPositionAndRotation(at, Quaternion.LookRotation(Flat(headDir).normalized) * Quaternion.Euler(0f, 0f, 78f));
            // settle him on the floor: his lowest point just above it
            var b = CatBounds(); Cat.transform.position += Vector3.up * (floorY + 0.01f - b.min.y);
        }
        Bounds CatBounds()
        {
            var b = new Bounds(Cat.transform.position, Vector3.zero); bool f = true;
            foreach (var r in Cat.GetComponentsInChildren<Renderer>()) { if (!r.enabled) continue; if (f) { b = r.bounds; f = false; } else b.Encapsulate(r.bounds); }
            return b;
        }
        void PantherFallen(bool large)
        {
            LayCat(L.PantherFalls, L.PantherHead - L.PantherFalls, large ? LargeScale : 1f, L.Y);
            var ov = CatOv; if (ov != null) { ov.Alive = true; ov.Slack = 0f; ov.Breath = 0.8f; ov.BreathRate = 0.55f; }
        }
        void PantherOnTheArchive()
        {
            LayCat(L.PantherOnArchive, Vector3.right, 1f, L.Y + L.PlatformH);
            var ov = CatOv; if (ov != null) { ov.Alive = false; ov.Slack = 1f; ov.Breath = 0f; ov.SettleTo = null; }
        }
        /// <summary>The cat's head / chest surface points (her hands rest on his fur; never inside him).</summary>
        Transform CatBone(string n) { if (Cat == null) return null; foreach (var t in Cat.GetComponentsInChildren<Transform>()) if (t.name == n) return t; return null; }
        Vector3 FurAt(string bone, Vector3 fromSide, float r)
        {
            var b = CatBone(bone); var c = b != null ? b.position : Cat.transform.position; float s = Cat.transform.lossyScale.x;
            var n = fromSide.sqrMagnitude > 1e-4f ? fromSide.normalized : Vector3.up;
            return c + n * r * s;
        }

        // ------------------------------------------------------------------------------------------ item 16: Tunneler restrained
        GameObject _chains;
        /// <summary>The Scree's heavy mining restraints round him and the rig, chains to the floor, his power cables pulled.</summary>
        void Restrained(bool on)
        {
            if (!on) { if (_chains != null) _chains.SetActive(false); return; }
            if (_chains == null && TunnelerCraft != null)
            {
                _chains = new GameObject("TunnelerRestraints");
                var mat = RockMat();
                var c = TunnelerCraft.position; var body = Tunneler != null ? Tunneler.position : c + Vector3.up * 0.8f;
                // three heavy bands round his body
                foreach (var h in new[] { 0.7f, 1.25f, 1.8f })
                {
                    var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(g.GetComponent<Collider>()); g.name = "RestraintBand"; g.transform.SetParent(_chains.transform, false);
                    g.transform.position = body + Vector3.up * h; g.transform.localScale = new Vector3(1.25f, 0.09f, 1.25f); g.GetComponent<Renderer>().sharedMaterial = mat;
                }
                // chains from the rig's rim to floor anchors (links = small flattened cylinders)
                for (int k = 0; k < 4; k++)
                {
                    float a = (35f + k * 90f) * Mathf.Deg2Rad; var p0 = c + new Vector3(Mathf.Sin(a) * 2.2f, 0.45f, Mathf.Cos(a) * 2.2f); var p1 = c + new Vector3(Mathf.Sin(a) * 3.3f, 0.02f, Mathf.Cos(a) * 3.3f);
                    for (int j = 0; j < 7; j++)
                    {
                        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(g.GetComponent<Collider>()); g.name = "RestraintChainLink"; g.transform.SetParent(_chains.transform, false);
                        float u = j / 6f; g.transform.position = Vector3.Lerp(p0, p1, u) + Vector3.down * Mathf.Sin(u * Mathf.PI) * 0.12f;
                        g.transform.rotation = Quaternion.LookRotation(p1 - p0) * Quaternion.Euler(90f, j * 90f, 0f); g.transform.localScale = new Vector3(0.12f, 0.03f, 0.2f); g.GetComponent<Renderer>().sharedMaterial = mat;
                    }
                    var anchor = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(anchor.GetComponent<Collider>()); anchor.name = "RestraintAnchor"; anchor.transform.SetParent(_chains.transform, false);
                    anchor.transform.position = p1 + Vector3.up * 0.08f; anchor.transform.localScale = new Vector3(0.3f, 0.16f, 0.3f); anchor.GetComponent<Renderer>().sharedMaterial = mat;
                }
            }
            if (_chains != null) _chains.SetActive(true);
            if (Rig != null) { Rig.PowerDown(); foreach (var r in Rig.GetComponentsInChildren<Renderer>()) if (r.name.Contains("Light")) r.enabled = false; }
            _rigUp = false; _sag = 7f; DrillSpeed = 0f;
        }
    }
}
