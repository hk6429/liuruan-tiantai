using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Liuruan
{
    /// R30 畫質升級第 3 步：HDRI 天空＋烘焙光照（lightmap、light probe、reflection probe）。
    /// 依 staging/R30/laneD/audit.md 第 7 節的建議值；static 名單在 scripts/r30_static_pis.txt（PrefabInstance fileID）。
    public static class R30Lighting
    {
        const string SkyHdr = "Assets/_Project/Art/Sky/kloofendal_48d_partly_cloudy_puresky_2k.hdr";
        const string SkyMat = "Assets/_Project/Art/Sky/M_Sky_Kloofendal.mat";
        const string LightingAsset = "Assets/_Project/Settings/R30_Lighting.lighting";
        const string RootName = "_R30Lighting";
        static readonly string[] VegWords = { "bamboo", "竹", "banana", "芭蕉", "lotus", "荷", "willow", "柳", "grass", "草", "tree", "樹" };

        public static string Setup()
        {
            var log = new List<string>();
            Sky(log);
            Statics(log);
            Lights(log);
            Probes(log);
            Settings(log);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            return string.Join("\n", log);
        }

        public static string Bake() => Lightmapping.BakeAsync() ? "bake started" : "bake failed to start";

        static void Sky(List<string> log)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(SkyHdr);
            if (!ti) { log.Add("sky: 尚無天空資產，保留現有背景"); return; }
            ti.textureShape = TextureImporterShape.TextureCube;
            ti.maxTextureSize = 512; // 每面 512；1024 的 RGB9E5 cube 要 24MB 記憶體，手機吃不消
            ti.mipmapEnabled = true;
            // WebGL 明指 BC6H（2MB、保留 HDR）：RGB9E5 要 8MB，自動壓縮會變 DXT5 把天空亮部截掉；
            // 不支援 BPTC 的手機由 Unity 執行期解壓，與 lightmap 同一條路
            var webgl = ti.GetPlatformTextureSettings("WebGL");
            webgl.overridden = true; webgl.maxTextureSize = 512; webgl.format = TextureImporterFormat.BC6H;
            ti.SetPlatformTextureSettings(webgl);
            ti.SaveAndReimport();

            var mat = AssetDatabase.LoadAssetAtPath<Material>(SkyMat);
            if (mat == null) { mat = new Material(Shader.Find("Skybox/Cubemap")); AssetDatabase.CreateAsset(mat, SkyMat); }
            mat.SetTexture("_Tex", AssetDatabase.LoadAssetAtPath<Cubemap>(SkyHdr));
            mat.SetFloat("_Exposure", 1f);
            EditorUtility.SetDirty(mat);

            RenderSettings.skybox = mat;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.85f; // 1.0 時草地偏白（s3 截圖）
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 128;
            log.Add("sky: " + SkyMat);
        }

        static bool IsVeg(Transform t)
        {
            for (; t != null; t = t.parent)
                foreach (var w in VegWords) if (t.name.ToLowerInvariant().Contains(w)) return true;
            return false;
        }

        static void Statics(List<string> log)
        {
            var cat = new Dictionary<ulong, string>();
            foreach (var line in File.Exists("scripts/r30_static_pis.txt") ? File.ReadAllLines("scripts/r30_static_pis.txt") : new string[0])
            {
                var p = line.Split('\t');
                cat[unchecked((ulong)long.Parse(p[1]))] = p[0]; // fileID 可能是負數
            }

            int matched = 0, removed = 0, uv2 = 0;
            var fbx = new HashSet<string>();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) continue;
                var handle = PrefabUtility.GetPrefabInstanceHandle(go);
                if (handle == null) continue;
                if (!cat.TryGetValue(GlobalObjectId.GetGlobalObjectIdSlow(handle).targetObjectId, out var c)) continue;
                matched++;
                float scale = c == "地面" ? 0.5f : c == "假山石" ? 1.2f : 1f;
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
                    r.receiveGI = ReceiveGI.Lightmaps;
                    var so = new SerializedObject(r);
                    so.FindProperty("m_ScaleInLightmap").floatValue = scale;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // 已勾 ContributeGI 的植被拿掉，免得小葉片吃光 lightmap；改用 light probe
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                if ((flags & StaticEditorFlags.ContributeGI) == 0) continue;
                if (IsVeg(r.transform))
                {
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags & ~StaticEditorFlags.ContributeGI);
                    r.receiveGI = ReceiveGI.LightProbes;
                    removed++;
                    continue;
                }
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var path = AssetDatabase.GetAssetPath(mf.sharedMesh);
                if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) fbx.Add(path);
                else if (path.EndsWith(".asset") && mf.sharedMesh.uv2.Length == 0)
                {
                    Unwrapping.GenerateSecondaryUVSet(mf.sharedMesh);
                    EditorUtility.SetDirty(mf.sharedMesh);
                    uv2++;
                }
            }

            foreach (var path in fbx)
            {
                var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                if (mi.generateSecondaryUV) continue;
                mi.generateSecondaryUV = true;
                mi.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            log.Add($"static: matched {matched}/{cat.Count} instances, veg ContributeGI removed {removed}, fbx lightmapUV {fbx.Count}, .asset uv2 {uv2}");
        }

        static void Lights(List<string> log)
        {
            int baked = 0;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional) { l.lightmapBakeType = LightmapBakeType.Mixed; continue; }
                if (l.GetComponents<MonoBehaviour>().Length > 0) continue; // 有腳本在動的燈（FindGlow 等）維持即時
                l.lightmapBakeType = LightmapBakeType.Baked;
                baked++;
            }
            log.Add($"lights: directional→Mixed, {baked} point/spot→Baked");
        }

        static void Probes(List<string> log)
        {
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName);

            // 反射探針：全園一顆底＋主要區域（audit 7.2）
            var refl = new (string n, Vector3 c, Vector3 s, int res)[]
            {
                ("RP_Global", new Vector3(0, 10, 0), new Vector3(240, 60, 240), 128),
                ("RP_Forest", new Vector3(0, 4, -76), new Vector3(50, 20, 85), 128),
                ("RP_Gorge", new Vector3(0, 8, 10), new Vector3(65, 35, 100), 128),
                ("RP_Village", new Vector3(0, 4, 87), new Vector3(65, 20, 55), 128),
            };
            foreach (var p in refl)
            {
                var go = new GameObject(p.n);
                go.transform.SetParent(root.transform);
                go.transform.position = p.c;
                var rp = go.AddComponent<ReflectionProbe>();
                rp.mode = ReflectionProbeMode.Baked;
                rp.size = p.s;
                rp.resolution = p.res;
                rp.boxProjection = p.n != "RP_Global";
                rp.importance = p.n == "RP_Global" ? 0 : 1;
                rp.hdr = true;
            }

            // Light probe：每 6m 一點，貼地 0.5m 與 2.2m 兩層；往下打不到東西就跳過
            var pts = new List<Vector3>();
            for (float x = -35; x <= 35; x += 6)
                for (float z = -114; z <= 114; z += 6)
                    if (Physics.Raycast(new Vector3(x, 40, z), Vector3.down, out var hit, 60))
                    {
                        pts.Add(hit.point + Vector3.up * 0.5f);
                        pts.Add(hit.point + Vector3.up * 2.2f);
                    }
            var lpg = new GameObject("LightProbes").AddComponent<LightProbeGroup>();
            lpg.transform.SetParent(root.transform);
            lpg.probePositions = pts.ToArray();
            log.Add($"probes: {refl.Length} reflection, {pts.Count} light probes");
        }

        static void Settings(List<string> log)
        {
            var s = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingAsset);
            if (s == null) { s = new LightingSettings(); AssetDatabase.CreateAsset(s, LightingAsset); }
            s.bakedGI = true;
            s.realtimeGI = false;
            s.mixedBakeMode = MixedLightingMode.IndirectOnly;
            s.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            s.lightmapResolution = 4;
            s.lightmapMaxSize = 1024;
            s.lightmapPadding = 2;
            s.directionalityMode = LightmapsMode.NonDirectional;
            s.lightmapCompression = LightmapCompression.NormalQuality;
            s.ao = true;
            s.aoMaxDistance = 1f;
            s.directSampleCount = 32;
            s.indirectSampleCount = 256;
            s.environmentSampleCount = 256;
            s.maxBounces = 2;
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();
            Lightmapping.lightingSettings = s;
            log.Add("lighting settings: " + LightingAsset);
        }
    }
}
