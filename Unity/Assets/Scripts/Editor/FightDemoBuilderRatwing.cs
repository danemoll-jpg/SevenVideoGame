using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 4: Ratwing wakes. Imports Art/Export/ratwing/ratwing_v1_rigged.fbx (rig + clips from
    /// Tools/Blender/cleanup_ratwing.py), builds the cave scene (Assets/Scenes/Ratwing.unity) with an Atmosphere_Cave
    /// variant: dark rock shell, stalactites, the ceiling ledge he hangs from (flat underside at CeilingY), a cave mouth
    /// opening onto bright glowing fog with huge spires fading into it, light shafts and drifting haze.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string RatwingScenePath = "Assets/Scenes/Ratwing.unity";
        const string RatwingDir = CharRoot + "/Ratwing";
        const string RatwingFbx = RatwingDir + "/ratwing_v1_rigged.fbx";
        const string AtmoCavePath = AtmoDir + "/Atmosphere_Cave.prefab";
        /// <summary>Ledge underside above the floor point under him. Must equal CEIL in cleanup_ratwing.py.</summary>
        const float RatwingCeil = 4.2f;

        static void BuildRound4(GameObject atmoPrefab)
        {
            var ctl = SetupRatwing(out var skin, out var eyes);
            var atmo = BuildAtmosphereCave(atmoPrefab);
            BuildRatwingScene(ctl, skin, eyes, atmo);
        }

        // ---------------------------------------------------------------------------------- model
        static AnimatorController SetupRatwing(out Material skin, out Material eyes)
        {
            var src = Path.Combine(RepoRoot, "Art", "Export", "ratwing", "ratwing_v1_rigged.fbx");
            if (!File.Exists(src)) throw new Exception("Ratwing export missing: run Tools/Blender/cleanup_ratwing.py first (" + src + ")");
            Directory.CreateDirectory(Path.Combine(ProjectRoot, RatwingDir, "Textures"));
            File.Copy(src, Path.Combine(ProjectRoot, RatwingFbx), true);
            File.Copy(Path.Combine(RepoRoot, "Art", "Export", "ratwing", "Textures", "ratwing_v1_basecolor.jpg"),
                Path.Combine(ProjectRoot, RatwingDir, "Textures", "ratwing_v1_basecolor.jpg"), true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var texPath = RatwingDir + "/Textures/ratwing_v1_basecolor.jpg";
            var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
            ti.maxTextureSize = 2048; ti.sRGBTexture = true; ti.SaveAndReimport();

            skin = Dissolve("Ratwing_skin", new Color(0.82f, 0.82f, 0.82f), 0.35f, false);     // double-sided (membranes)
            skin.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            skin.SetFloat("_Rim", 0.22f); skin.SetFloat("_RimPower", 3.5f);
            eyes = Dissolve("Ratwing_eyes", Color.white, 0.5f, false);
            eyes.SetColor("_EmissionColor", Color.white * 2.6f);
            EditorUtility.SetDirty(eyes);

            var imp = (ModelImporter)AssetImporter.GetAtPath(RatwingFbx);
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = true;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.animationCompression = ModelImporterAnimationCompression.Off;     // keep the ceiling / floor contact exact
            imp.SaveAndReimport();
            var clips = imp.defaultClipAnimations;
            foreach (var c in clips)
            {
                var n = c.takeName.Contains("|") ? c.takeName.Substring(c.takeName.LastIndexOf('|') + 1) : c.takeName;
                c.name = n;
                c.loopTime = n == "idle";
                c.loopPose = false;
            }
            imp.clipAnimations = clips;
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "ratwing_skin"), skin);
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "ratwing_eyeglow"), eyes);
            imp.SaveAndReimport();

            var path = AnimDir + "/Ratwing.controller";
            AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm = ac.layers[0].stateMachine;
            var all = AssetDatabase.LoadAllAssetsAtPath(RatwingFbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();
            AnimatorState seq = null, idle = null;
            foreach (var clip in all)
            {
                var st = sm.AddState(clip.name);
                st.motion = clip;
                if (clip.name == "sequence") seq = st;
                if (clip.name == "idle") idle = st;
                Log($"ratwing clip {clip.name} {clip.length:0.00}s loop={clip.isLooping}");
            }
            if (seq == null || idle == null) throw new Exception("Ratwing FBX needs the clips 'sequence' and 'idle'");
            sm.defaultState = seq;
            var tr = seq.AddTransition(idle);
            tr.hasExitTime = true; tr.exitTime = 1f; tr.duration = 0f; tr.hasFixedDuration = true;
            EditorUtility.SetDirty(ac);
            return ac;
        }

        // ----------------------------------------------------------------------------- atmosphere
        static GameObject BuildAtmosphereCave(GameObject atmoPrefab)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(atmoPrefab);
            var a = inst.GetComponent<Seven.Atmosphere>();
            // Dark interior (near fog nearly black, dim looking up at him), bright glowing fog from the mouth outward.
            a.FogStart = 3f;
            a.GroundDensity = 0.035f; a.GroundBase = 0f; a.GroundFalloff = 0.8f; a.GroundRange = 30f;
            a.ValleyDensity = 0.004f; a.ValleyBase = -2f; a.ValleyFalloff = 25f;
            a.DistanceDensity = 0.010f; a.HazeHeight = 60f; a.MaxFog = 1f;
            a.NearBrightness = 0.08f; a.FarBrightness = 1.15f; a.BrightnessDistance = 24f; a.ZenithBrightness = 0.12f;
            a.NoiseStrength = 0.55f; a.NoiseScale = 0.08f; a.NoiseSpeed = 0.02f; a.NoiseFadeDistance = 30f;
            a.GlowIntensity = 0.25f; a.GlowRadius = 22f;
            var glows = new[]
            {
                (new Vector3(0f, 2.5f, 19f), new Vector3(0.45f, 1.5f, 1f)),      // just outside the mouth
                (new Vector3(-7f, 4f, 34f), new Vector3(0.8f, 1.1f, 1f)),
                (new Vector3(9f, 7f, 52f), new Vector3(1.1f, 1.1f, 1f)),
                (new Vector3(-20f, 12f, 90f), new Vector3(1.6f, 1f, 1f)),
                (new Vector3(25f, 8f, 120f), new Vector3(1.8f, 1f, 1f)),
                (new Vector3(0f, 6f, 70f), new Vector3(1.2f, 1.2f, 1f)),
                (new Vector3(0f, 30f, 260f), new Vector3(7f, 1.8f, 1f)),
            };
            int gi = 0;
            foreach (Transform c in inst.transform)
            {
                if (c.name.StartsWith("Glow") && gi < glows.Length) { c.position = glows[gi].Item1; c.localScale = glows[gi].Item2; gi++; }
                if (c.name == "BackLight")
                {
                    c.rotation = Quaternion.Euler(16f, 186f, 0f);      // from the bright mouth into the cave
                    var l = c.GetComponent<Light>(); l.intensity = 1.7f; l.shadowStrength = 0.85f;
                }
                if (c.name == "FrontFill")
                {
                    c.rotation = Quaternion.Euler(30f, 15f, 0f);
                    c.GetComponent<Light>().intensity = 0.16f;
                }
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, AtmoCavePath);
            UnityEngine.Object.DestroyImmediate(inst);
            Log("atmosphere variant saved " + AtmoCavePath);
            return prefab;
        }

        // ---------------------------------------------------------------------------------- cave
        static float Noise3(Vector3 p, float f)
        {
            return (Mathf.PerlinNoise(p.x * f + 11.3f, p.y * f + 3.1f) + Mathf.PerlinNoise(p.y * f + 7.7f, p.z * f + 19.5f)
                    + Mathf.PerlinNoise(p.z * f + 29.1f, p.x * f + 5.9f)) / 1.5f - 1f;     // about -1..1
        }

        // Cave shell: inside of an ellipsoid (double-sided material), rock noise, mouth cut at +Z.
        static readonly Vector3 CaveC = new Vector3(0f, 1.2f, 3f), CaveR = new Vector3(8f, 4.8f, 13f);

        static float CaveCeilingY(float x, float z)
        {
            float k = 1f - Mathf.Pow(x / CaveR.x, 2) - Mathf.Pow((z - CaveC.z) / CaveR.z, 2);
            return k <= 0f ? CaveC.y : CaveC.y + CaveR.y * Mathf.Sqrt(k);
        }

        static Mesh CaveShell()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            const int lat = 140, lon = 220;
            for (int i = 0; i <= lat; i++)
            {
                float th = Mathf.PI * i / lat;
                for (int j = 0; j <= lon; j++)
                {
                    float ph = Mathf.PI * 2f * j / lon;
                    var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), -Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    var p = CaveC + Vector3.Scale(d, CaveR);
                    float n = Noise3(p, 0.22f) * 0.55f + Noise3(p, 0.9f) * 0.14f;
                    v.Add(p - Vector3.Scale(d, CaveR).normalized * n);
                }
            }
            for (int i = 0; i < lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    float th = Mathf.PI * (i + 0.5f) / lat, ph = Mathf.PI * 2f * (j + 0.5f) / lon;
                    var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), -Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    // the mouth: a ragged opening at the far (+Z) end, from the floor up
                    float rag = Noise3(new Vector3(ph * 3f, th * 3f, 0f), 1.3f) * 0.07f;
                    if (d.z > 0.62f + rag && d.y > -0.45f && d.y < 0.62f + rag) continue;
                    int a = i * (lon + 1) + j, b = a + 1, c = a + lon + 1, e = c + 1;
                    t.Add(a); t.Add(b); t.Add(c);
                    t.Add(b); t.Add(e); t.Add(c);
                }
            return SmoothFinish(v, t);
        }

        /// <summary>Noisy lump: ellipsoid with rock noise; points below flatY are clamped to it (flat underside).</summary>
        static void Lump(List<Vector3> v, List<int> t, Vector3 c, Vector3 r, float noise, int seed, float flatY = float.NegativeInfinity)
        {
            int start = v.Count;
            const int lat = 18, lon = 28;
            var off = new Vector3(seed * 3.7f, seed * 1.3f, seed * 5.1f);
            for (int i = 0; i <= lat; i++)
            {
                float th = Mathf.PI * i / lat;
                for (int j = 0; j <= lon; j++)
                {
                    float ph = Mathf.PI * 2f * j / lon;
                    var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), -Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    var p = c + Vector3.Scale(d, r) * (1f + noise * Noise3(d * 2.2f + off, 1f));
                    if (p.y < flatY) p.y = flatY;
                    v.Add(p);
                }
            }
            for (int i = 0; i < lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    int a = start + i * (lon + 1) + j, b = a + 1, cc = a + lon + 1, dd = cc + 1;
                    t.Add(a); t.Add(cc); t.Add(b);
                    t.Add(b); t.Add(cc); t.Add(dd);
                }
        }

        /// <summary>Hanging (tip down) or standing (tip up) rock spike.</summary>
        static void Spike(List<Vector3> v, List<int> t, Vector3 baseC, float radius, float length, bool down, System.Random rnd)
        {
            int start = v.Count;
            const int sides = 7, rings = 6;
            float dir = down ? -1f : 1f;
            float lean = (float)rnd.NextDouble() * 0.12f;
            float la = (float)rnd.NextDouble() * Mathf.PI * 2f;
            for (int k = 0; k <= rings; k++)
            {
                float u = (float)k / rings;
                float rr = radius * Mathf.Pow(1f - u, 1.35f) + 0.004f;
                var cc = baseC + new Vector3(Mathf.Cos(la) * lean * u * length, dir * length * u, Mathf.Sin(la) * lean * u * length);
                for (int s = 0; s < sides; s++)
                {
                    float a = Mathf.PI * 2f * s / sides;
                    float wob = 1f + 0.25f * ((float)rnd.NextDouble() - 0.5f);
                    v.Add(cc + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rr * wob);
                }
            }
            for (int k = 0; k < rings; k++)
                for (int s = 0; s < sides; s++)
                {
                    int a = start + k * sides + s, b = start + k * sides + (s + 1) % sides, c = a + sides, d = b + sides;
                    if (down) { t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c); }
                    else { t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d); }
                }
        }

        static Mesh CaveFloor()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            const int nx = 96, nz = 140;
            const float x0 = -12f, x1 = 12f, z0 = -12f, z1 = 22f;
            for (int i = 0; i <= nz; i++)
                for (int j = 0; j <= nx; j++)
                {
                    float x = Mathf.Lerp(x0, x1, (float)j / nx), z = Mathf.Lerp(z0, z1, (float)i / nz);
                    float r = new Vector2(x, z + 0.3f).magnitude;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.6f, 6f, r));      // exactly flat where he lands
                    float y = k * (0.22f * Noise3(new Vector3(x, 0f, z), 0.35f) + 0.08f * Noise3(new Vector3(x, 1f, z), 1.4f) + 0.05f);
                    v.Add(new Vector3(x, y, z));
                }
            for (int i = 0; i < nz; i++)
                for (int j = 0; j < nx; j++)
                {
                    int a = i * (nx + 1) + j, b = a + 1, c = a + nx + 1, d = c + 1;
                    t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
                }
            return SmoothFinish(v, t);
        }

        /// <summary>Soft additive light shaft: a tapered quad strip, alpha fading toward the edges and the far end.</summary>
        static Mesh Shaft(Vector3 from, Vector3 to, float w0, float w1, float alpha, Vector3 faceDir)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var uv = new List<Vector2>(); var t = new List<int>();
            const int segL = 10, segW = 6;
            var axis = (to - from).normalized;
            var side = Vector3.Cross(axis, faceDir).normalized;
            for (int i = 0; i <= segL; i++)
            {
                float u = (float)i / segL;
                var p = Vector3.Lerp(from, to, u);
                float w = Mathf.Lerp(w0, w1, u);
                for (int j = 0; j <= segW; j++)
                {
                    float s = (float)j / segW * 2f - 1f;
                    v.Add(p + side * s * w * 0.5f);
                    float a = alpha * (1f - s * s) * Mathf.SmoothStep(0f, 1f, u / 0.15f) * (1f - Mathf.SmoothStep(0.45f, 1f, u));
                    c.Add(new Color(1f, 1f, 1f, a));
                    uv.Add(new Vector2(0.5f, 0.5f));
                }
            }
            for (int i = 0; i < segL; i++)
                for (int j = 0; j < segW; j++)
                {
                    int a = i * (segW + 1) + j, b = a + 1, cc = a + segW + 1, d = cc + 1;
                    t.Add(a); t.Add(cc); t.Add(b); t.Add(b); t.Add(cc); t.Add(d);
                }
            var m = new Mesh();
            m.SetVertices(v); m.SetColors(c); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        static Texture2D WhiteTex()
        {
            var path = GenDir + "/Fx_white.png";
            var full = Path.Combine(ProjectRoot, path);
            if (!File.Exists(full))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                var tx = new Texture2D(4, 4, TextureFormat.RGB24, false);
                tx.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray()); tx.Apply();
                File.WriteAllBytes(full, tx.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tx);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void BuildRatwingScene(AnimatorController ctl, Material skin, Material eyeMat, GameObject atmoCave)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.13f, 0.13f, 0.13f);
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(RenderSettings.ambientLight);
            RenderSettings.ambientProbe = sh;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            var atmo = (GameObject)PrefabUtility.InstantiatePrefab(atmoCave);
            atmo.name = "Atmosphere_Cave";
            // Cave-only post override (priority above the Atmosphere volume): tighter bloom so the bright mouth does
            // not veil the dark interior, a little more contrast; saturation stays -100 (monochrome safety net).
            const string caveProfilePath = AtmoDir + "/Cave_Volume.asset";
            AssetDatabase.DeleteAsset(caveProfilePath);
            var cp = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(cp, caveProfilePath);
            var cb = cp.Add<Bloom>(true);
            cb.threshold.value = 0.95f; cb.intensity.value = 1.0f; cb.scatter.value = 0.6f; cb.tint.value = Color.white;
            var cca = cp.Add<ColorAdjustments>(true);
            cca.saturation.value = -100f; cca.contrast.value = 14f; cca.postExposure.value = -0.1f; cca.colorFilter.value = Color.white; cca.hueShift.value = 0f;
            foreach (var c in cp.components) AssetDatabase.AddObjectToAsset(c, cp);
            EditorUtility.SetDirty(cp);
            var cvGo = new GameObject("CaveVolume");
            var cv = cvGo.AddComponent<Volume>();
            cv.isGlobal = true; cv.priority = 10f; cv.sharedProfile = cp;

            var rock = Dissolve("Cave_rock", new Color(0.17f, 0.17f, 0.17f), 0.12f, false);
            var rockDark = Dissolve("Cave_rock_dark", new Color(0.09f, 0.09f, 0.09f), 0.1f, false);
            var floorMat = Dissolve("Cave_floor", new Color(0.13f, 0.13f, 0.13f), 0.18f, false);
            var outside = Dissolve("Cave_outside", new Color(0.32f, 0.32f, 0.32f), 0.1f, false);
            var shaftMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cave_shaft.mat");
            if (shaftMat == null) { shaftMat = new Material(Shader.Find("Seven/Additive")); AssetDatabase.CreateAsset(shaftMat, MatDir + "/Cave_shaft.mat"); }
            shaftMat.shader = Shader.Find("Seven/Additive");
            shaftMat.SetTexture("_MainTex", WhiteTex());
            shaftMat.SetFloat("_Intensity", 1f);
            EditorUtility.SetDirty(shaftMat);

            var world = new GameObject("Cave").transform;
            var shell = MeshObject("Shell", SaveMesh(CaveShell(), "Cave_Shell"), rock, world);
            shell.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.TwoSided;
            var floor = MeshObject("Floor", SaveMesh(CaveFloor(), "Cave_Floor"), floorMat, world);
            floor.AddComponent<MeshCollider>();

            // The ledge he hangs from: a rock outcrop from the ceiling with a flat underside at RatwingCeil.
            var lv = new List<Vector3>(); var lt = new List<int>();
            Lump(lv, lt, new Vector3(0f, RatwingCeil + 0.75f, 0.1f), new Vector3(1.8f, 0.9f, 1.35f), 0.28f, 3, RatwingCeil);
            Lump(lv, lt, new Vector3(-1.1f, RatwingCeil + 0.5f, -0.4f), new Vector3(0.7f, 0.6f, 0.6f), 0.35f, 5, RatwingCeil + 0.05f);
            Lump(lv, lt, new Vector3(1.2f, RatwingCeil + 0.55f, 0.7f), new Vector3(0.8f, 0.55f, 0.7f), 0.35f, 6, RatwingCeil + 0.08f);
            Lump(lv, lt, new Vector3(0.4f, CaveCeilingY(0.4f, 0.3f) - 0.2f, 0.3f), new Vector3(1.4f, 1.3f, 1.2f), 0.2f, 4);
            // Rock lumps along the mouth rim: a ragged dark frame instead of the shell's cut edge.
            var mv = new List<Vector3>(); var mt = new List<int>();
            var rrim = new System.Random(77);
            for (float ang = -40f; ang <= 220f; ang += 7.5f)
            {
                float a = ang * Mathf.Deg2Rad;
                var dd = new Vector3(0.785f * Mathf.Cos(a), 0.785f * Mathf.Sin(a), 0.62f);
                if (dd.y < -0.45f || dd.y > 0.66f) continue;
                var p = CaveC + Vector3.Scale(dd, CaveR);
                if (p.y < -0.5f) continue;
                float s = Mathf.Lerp(0.7f, 1.6f, (float)rrim.NextDouble());
                Lump(mv, mt, p, new Vector3(s, s * 0.9f, s * 1.3f), 0.35f, 30 + (int)ang);
            }
            MeshObject("MouthRim", SaveMesh(SmoothFinish(mv, mt), "Cave_MouthRim"), rockDark, world);

            var ledge = MeshObject("Ledge", SaveMesh(SmoothFinish(lv, lt), "Cave_Ledge"), rockDark, world);
            ledge.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.TwoSided;
            ledge.AddComponent<MeshCollider>();

            // Stalactites (ceiling) and stalagmites (floor edges); a few close to the camera as dark foreground shapes.
            var rnd = new System.Random(41);
            float R() => (float)rnd.NextDouble();
            var sv = new List<Vector3>(); var st = new List<int>();
            int nDown = 0, nUp = 0;
            for (int i = 0; i < 260 && nDown < 70; i++)
            {
                float x = Mathf.Lerp(-7f, 7f, R()), z = Mathf.Lerp(-9f, 12f, R());
                if (new Vector2(x, z - 0.1f).magnitude < 2.2f) continue;                // keep the ledge clear
                if (Mathf.Abs(x) < 1.3f && z < -2.5f && R() < 0.75f) continue;         // mostly clear camera line
                float cy = CaveCeilingY(x, z);
                if (cy < 2.4f) continue;
                float len = Mathf.Lerp(0.35f, 1f, R() * R()) * Mathf.Min(2.6f, (cy - 1.6f) * 0.55f);
                Spike(sv, st, new Vector3(x, cy + 0.15f, z), Mathf.Lerp(0.1f, 0.38f, R()) * Mathf.Sqrt(len), len + 0.15f, true, rnd);
                nDown++;
            }
            for (int i = 0; i < 200 && nUp < 34; i++)
            {
                float x = Mathf.Lerp(-7f, 7f, R()), z = Mathf.Lerp(-9f, 11f, R());
                float r = new Vector2(x, z).magnitude;
                if (r < 4f || (Mathf.Abs(x) < 1.6f && z < -2f)) continue;
                float len = Mathf.Lerp(0.25f, 1.6f, R() * R());
                Spike(sv, st, new Vector3(x, -0.1f, z), Mathf.Lerp(0.12f, 0.4f, R()) * Mathf.Sqrt(len), len + 0.1f, false, rnd);
                nUp++;
            }
            MeshObject("Spikes", SaveMesh(SmoothFinish(sv, st), "Cave_Spikes"), rockDark, world);
            // Loose boulders along the walls.
            var bv = new List<Vector3>(); var bt = new List<int>();
            for (int i = 0; i < 26; i++)
            {
                float a = R() * Mathf.PI * 2f, r = Mathf.Lerp(4.5f, 7f, R());
                var p = new Vector3(Mathf.Cos(a) * r, 0f, 3f + Mathf.Sin(a) * r * 1.4f);
                if (p.z > 10f || (Mathf.Abs(p.x) < 1.8f && p.z < -2f)) continue;
                float s = Mathf.Lerp(0.25f, 0.9f, R() * R());
                Lump(bv, bt, p + Vector3.up * s * 0.3f, new Vector3(s, s * 0.7f, s * 0.9f), 0.25f, 10 + i);
            }
            MeshObject("Boulders", SaveMesh(SmoothFinish(bv, bt), "Cave_Boulders"), rock, world);

            // Outside the mouth: ground falling away into the fog and huge spires fading into the haze.
            var ov = new List<Vector3>(); var ot = new List<int>();
            AddBox(ov, ot, new Vector3(0f, -0.6f, 220f), new Vector3(900f, 1f, 420f));
            for (int i = 0; i < 16; i++)
            {
                float x = Mathf.Lerp(-140f, 140f, R()), z = Mathf.Lerp(45f, 300f, R());
                if (Mathf.Abs(x) < 10f && z < 70f) continue;
                float h = Mathf.Lerp(25f, 110f, R());
                Spike(ov, ot, new Vector3(x, -1f, z), Mathf.Lerp(4f, 12f, R()), h, false, rnd);
            }
            MeshObject("Outside", SaveMesh(Finish(ov, ot), "Cave_Outside"), outside, world, false);

            // Light shafts from the mouth and a crack above the ledge (additive, faint), haze is spawned at runtime.
            var shafts = new GameObject("LightShafts").transform;
            shafts.SetParent(world, false);
            var faceCam = new Vector3(0f, 0f, -1f);
            var sDefs = new[]
            {
                (new Vector3(-2.2f, 5.2f, 11f), new Vector3(-0.8f, 0f, 1.5f), 1.6f, 3.6f, 0.10f),
                (new Vector3(1.4f, 5.4f, 11.5f), new Vector3(1.9f, 0f, 2.6f), 1.2f, 3.0f, 0.085f),
                (new Vector3(3.2f, 4.6f, 10.5f), new Vector3(3.0f, 0f, 3.8f), 0.9f, 2.4f, 0.07f),
                (new Vector3(-0.9f, 5.9f, -1.2f), new Vector3(-1.3f, 0f, -2.0f), 0.35f, 1.4f, 0.06f),
            };
            for (int i = 0; i < sDefs.Length; i++)
            {
                var d = sDefs[i];
                var go = MeshObject("Shaft" + i, SaveMesh(Shaft(d.Item1, d.Item2, d.Item3, d.Item4, d.Item5, faceCam), "Cave_Shaft" + i), shaftMat, shafts, false);
                go.GetComponent<MeshRenderer>().receiveShadows = false;
            }
            AddProbe(new Vector3(0f, 2.5f, 0f), new Vector3(16f, 8f, 26f), new Vector3(0f, 0f, 3f));

            // Ratwing: root at the floor point under the ledge; the clip carries him from the ceiling to the floor.
            var rwRoot = new GameObject("Ratwing");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RatwingFbx));
            model.name = "RatwingModel";
            model.transform.SetParent(rwRoot.transform, false);
            var tf = model.GetComponentsInChildren<Transform>();
            Transform B(string n) => tf.FirstOrDefault(x => x.name == n);
            var head = B("head"); var eyesB = B("eyes"); var hips = B("hips");
            if (head != null && eyesB != null)
            {
                var fwd = rwRoot.transform.InverseTransformDirection(eyesB.position - head.position); fwd.y = 0f;
                float yaw = Vector3.SignedAngle(fwd, Vector3.back, Vector3.up);       // face the camera (-Z)
                model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * model.transform.localRotation;
                Log($"ratwing model turned {yaw:0.0} deg so he faces -Z");
            }
            else Log("ratwing: head/eyes bones not found; facing not corrected");
            var anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.updateWhenOffscreen = true;
                bool isEyes = smr.sharedMaterials.Any(mm => mm != null && mm.name.ToLower().Contains("eye")) || smr.name.Contains("eyes");
                smr.sharedMaterials = smr.sharedMaterials.Select(_ => isEyes ? eyeMat : skin).ToArray();
                smr.shadowCastingMode = isEyes ? ShadowCastingMode.Off : ShadowCastingMode.On;
                smr.quality = SkinQuality.Bone4;
            }
            var bb = new Bounds(rwRoot.transform.position, Vector3.zero);
            foreach (var r in model.GetComponentsInChildren<Renderer>()) bb.Encapsulate(r.bounds);
            Log($"ratwing bind-pose bounds size {bb.size} (expect ~2.1 m tall incl. wing hooks, ~2.9 m wingspan)");

            var camGo = new GameObject("RatwingCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.85f, 0.85f, 0.85f);
            cam.fieldOfView = 46f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 1500f;
            camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.requiresDepthTexture = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.transform.position = new Vector3(0.8f, 2.2f, -8f);
            camGo.transform.LookAt(new Vector3(0f, 3f, 0f));

            var dir = new GameObject("RatwingDirector").AddComponent<Seven.RatwingDirector>();
            dir.Anim = anim; dir.Model = model.transform; dir.Cam = cam; dir.Hips = hips; dir.Head = head;
            dir.CeilingY = RatwingCeil;
            dir.Additive = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Fx_additive.mat");

            EditorSceneManager.SaveScene(scene, RatwingScenePath);
            Log("scene saved " + RatwingScenePath);
        }
    }
}
