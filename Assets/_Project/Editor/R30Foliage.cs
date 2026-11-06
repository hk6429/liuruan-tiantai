using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Liuruan
{
    /// R30 畫質升級第 5 步：植被換 Liuruan/FoliageWind（風動＋葉背透光）。
    /// 植被和建築共用 M_r10_2side / M_r10_matte 調色盤材質，所以另建風動版本、只換在植被 renderer 上。
    /// Instancing 已由 _R8/_R10/_R12/_R14Outdoor 根物件上的 InstancedGroup 處理。
    public static class R30Foliage
    {
        const string Dir = "Assets/_Project/Materials/R30/";

        // 類別, 權重高度(m), 主擺動(m), 細顫(m), 透光, 網格名
        static readonly (string cls, float height, float strength, float flutter, float transl, string[] meshes)[] Classes =
        {
            ("grass", 0.5f, 0.06f, 0.01f, 0.6f, new[] { "gc_maidong", "gc_shudai", "gc_shudai_drift", "grass_patch_mix", "gc_changpu", "flower_jiguan", "flower_fengxian", "juhua_bed" }),
            ("shrub", 1.5f, 0.05f, 0.008f, 0.5f, new[] { "dujuan_low", "dujuan_mound", "huangyang_trio", "huangyang_ball", "shrub_belt", "mufurong", "bamboo_belt", "bamboo_small" }),
            ("tree", 8f, 0.15f, 0.01f, 0.3f, new[] { "pine_r12", "pine_r12_lod1", "wutong_slim", "wutong_slim_lod1", "guihua", "guihua_lod1" }),
        };

        public static string Run()
        {
            var shader = Shader.Find("Liuruan/FoliageWind");
            var src2 = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Blender/r10r11/M_r10_2side.mat");
            var srcM = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Blender/r10r11/M_r10_matte.mat");
            if (!shader || !src2 || !srcM) return "foliage wind: 尚無風動 shader／調色盤材質，未修改場景";
            System.IO.Directory.CreateDirectory(Dir);
            AssetDatabase.Refresh();

            var swap = new Dictionary<string, (Material two, Material matte)>();
            foreach (var c in Classes)
            {
                var pair = (Make(shader, src2, c, true), Make(shader, srcM, c, false));
                foreach (var m in c.meshes) swap[m] = pair;
            }

            int n = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || !swap.TryGetValue(mf.sharedMesh.name, out var pair)) continue;
                var mats = r.sharedMaterials;
                bool hit = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == src2) { mats[i] = pair.two; hit = true; }
                    else if (mats[i] == srcM) { mats[i] = pair.matte; hit = true; }
                }
                if (!hit) continue;
                r.sharedMaterials = mats;
                n++;
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            return $"foliage wind: {n} renderers";
        }

        static Material Make(Shader shader, Material src, (string cls, float height, float strength, float flutter, float transl, string[] meshes) c, bool twoSided)
        {
            var path = Dir + $"M_r10_{c.cls}_{(twoSided ? "2side" : "matte")}_wind.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetTexture("_BaseMap", src.GetTexture("_BaseMap"));
            m.SetColor("_BaseColor", src.GetColor("_BaseColor"));
            m.SetFloat("_Cutoff", 0.1f); // 調色盤不透明，只為共用 alpha clip 路徑
            m.SetFloat("_Cull", twoSided ? 0f : 2f);
            m.SetFloat("_WrapDiffuse", 0.4f);
            m.SetFloat("_SpecularStrength", 0.05f);
            m.SetFloat("_Smoothness", 0.3f);
            m.SetColor("_TranslucencyColor", new Color(0.6f, 0.8f, 0.25f));
            m.SetFloat("_Translucency", c.transl);
            m.SetFloat("_WindHeight", c.height);
            m.SetVector("_WindDir", new Vector4(1, 0, 0.3f, 0));
            m.SetFloat("_WindStrength", c.strength);
            m.SetFloat("_WindSpeed", c.cls == "grass" ? 1.8f : 1.0f);
            m.SetFloat("_WindWaveScale", c.cls == "grass" ? 0.3f : 0.15f);
            m.SetFloat("_FlutterStrength", c.flutter);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
