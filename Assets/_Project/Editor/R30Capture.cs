using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Liuruan
{
    /// R30 畫質升級：固定機位截圖，做改前／改後對照（輸出 Captures/r30/<tag>_<shot>.png）
    public static class R30Capture
    {
        static readonly (string name, Vector3 pos, Vector3 look)[] Shots =
        {
            ("a_start", new Vector3(0, 2.4f, -112), new Vector3(0, 1.2f, -104)),
            ("b_forest", new Vector3(2, 2.4f, -86), new Vector3(8, 1.2f, -72)),
            ("c_gorge", new Vector3(-9, 2.4f, -31), new Vector3(-4, 1, 12)),
            ("d_peach", new Vector3(-7, 5, -40), new Vector3(-18, 20, -24)),
            ("e_village", new Vector3(0, 2.4f, 65), new Vector3(0, 2, 85)),
            ("f_overview", new Vector3(70, 100, -100), new Vector3(0, 0, 5)),
        };

        public static string Run(string tag)
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, tag == "r1" || tag == "r2" ? "../Captures/" + tag : "../Captures/r30"));
            Directory.CreateDirectory(dir);
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)) ps.Simulate(10f, true, true);
            var go = new GameObject("_R30CaptureCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 55; cam.nearClipPlane = 0.1f; cam.farClipPlane = 1000;
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                var shots = new System.Collections.Generic.List<(string name, Vector3 pos, Vector3 look)>(Shots);
                if (tag == "r2")
                {
                    var player = GameObject.Find("Player").transform;
                    var companion = GameObject.Find("Companion").transform;
                    Vector3 center = (player.position + companion.position) * 0.5f;
                    shots.Add(("chars", center + new Vector3(0, 1.8f, 4), center + Vector3.up * 0.9f));
                }
                if (tag == "r2") AnimationMode.StartAnimationMode();
                foreach (var s in shots)
                {
                    if (tag == "r2" && s.name == "chars")
                    {
                        AnimationMode.BeginSampling();
                        foreach (var actor in new[] { "Player", "Companion" })
                        {
                            var animator = GameObject.Find(actor).GetComponentInChildren<Animator>();
                            var walk = System.Array.Find(animator.runtimeAnimatorController.animationClips, c => c.name.ToLowerInvariant().Contains("walk"));
                            if (!walk) throw new System.InvalidOperationException(actor + " 缺少 walk clip。");
                            AnimationMode.SampleAnimationClip(animator.gameObject, walk, walk.length * .3f);
                        }
                        AnimationMode.EndSampling();
                    }
                    go.transform.position = s.pos;
                    go.transform.LookAt(s.look);
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                    File.WriteAllBytes(Path.Combine(dir, $"{tag}_{s.name}.png"), tex.EncodeToPNG());
                }
            }
            finally
            {
                if (tag == "r2" && AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                RenderTexture.active = null;
                cam.targetTexture = null;
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
            }
            return dir;
        }
    }
}
