using System;
using System.IO;
using System.Linq;
using Liuruan;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public static class R2Setup
{
    public static bool UseInstalledArt;
    const string Art = "Assets/_Project/Art/R1/";
    const string Mats = "Assets/_Project/Materials/R2/";
    [Serializable] sealed class AtlasEntry { public string name; public float[] uv_bottom_left; }
    [Serializable] sealed class AtlasList { public AtlasEntry[] entries; }
    [MenuItem("Tools/Liuruan/R2/Install")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("請先離開 Play 模式。");
        R1Setup.Install();
        Directory.CreateDirectory(Mats); AssetDatabase.Refresh();
        ConfigureTextures();
        Material day = Sky("sky_day"), dusk = Sky("sky_return");
        RenderSettings.skybox = day;
        Camera.main.clearFlags = CameraClearFlags.Skybox;
        var flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
        flow.returnSky = dusk;
        Ground();
        Foliage();
        var player = GameObject.Find("Player");
        var companion = GameObject.Find("Companion");
        R2bSetup.Character(player, "liuchen"); R2bSetup.Character(companion, "ruanzhao");
        var capsule = companion.GetComponent<CapsuleCollider>();
        if (capsule) capsule.center = Vector3.up * (capsule.height * 0.5f);
        companion.GetComponent<NavMeshAgent>().baseOffset = 0;
        companion.transform.position = new Vector3(1.7f, 0.1f, -106);
        var mapGo = GameObject.Find("R2Minimap") ?? new GameObject("R2Minimap", typeof(Camera));
        var map = Add<MinimapCam>(mapGo);
        Ref(map, "player", player.transform); Ref(map, "companion", companion.transform);
        var so = new SerializedObject(map);
        so.FindProperty("worldMin").vector2Value = new Vector2(-120, -120);
        so.FindProperty("worldSize").vector2Value = new Vector2(240, 240);
        so.ApplyModifiedPropertiesWithoutUndo();
        map.FrameTexture = Texture("ui/minimap_frame.png"); flow.minimap = map;
        var hud = flow.GetComponent<HudUI>();
        hud.peachFull = Sprite("peach_full"); hud.peachHalf = Sprite("peach_half"); hud.peachEmpty = Sprite("peach_empty");
        hud.barkIcon = Sprite("bark"); hud.skillBark = Sprite("skill_bark"); hud.skillWater = Sprite("skill_water"); hud.skillCall = Sprite("skill_call");
        hud.portraitImage = Sprite("portrait_ruanzhao"); hud.portraitFrame = Sprite("portrait_frame");
        var zoneGo = GameObject.Find("ActPoints/Act2_ClimbBase");
        // East face of the west cliff; keep the whole attach/top-out path outside its collider.
        zoneGo.transform.position = new Vector3(-13.8f, 0.1f, -29);
        var box = Add<BoxCollider>(zoneGo); box.isTrigger = true; box.size = new Vector3(5, 4, 6); box.center = Vector3.up;
        var body = Add<Rigidbody>(zoneGo); body.isKinematic = true; body.useGravity = false;
        var zone = Add<ClimbZone>(zoneGo);
        var plane = Node("R2VinePlane", new Vector3(-13.8f, 0.1f, -29)); plane.rotation = Quaternion.Euler(0, 90, 0);
        var landing = Node("R2VineLanding", new Vector3(-16, 18.1f, -27));
        Ref(zone, "player", player.transform); Ref(zone, "playerMovement", player.GetComponent<PlayerController>());
        Ref(zone, "hunger", player.GetComponent<HungerSystem>()); Ref(zone, "planeOrigin", plane); Ref(zone, "topLanding", landing);
        Number(zone, "height", 18); Number(zone, "width", 1.5f);
        flow.climb = zone;
        Vine(plane);
        var collection = Add<DriftCollection>(flow.gameObject); flow.driftCollection = collection;
        for (int i = 1; i <= 3; i++)
        {
            var go = GameObject.Find("ActPoints/Act3_Drift_" + i);
            var pickup = Add<DriftPickup>(go);
            Ref(pickup, "player", player.transform); Ref(pickup, "collection", collection);
            var properties = new SerializedObject(pickup);
            properties.FindProperty("id").intValue = i;
            properties.FindProperty("cycleSeconds").floatValue = 38 + i * 5;
            var waypoints = properties.FindProperty("waypoints"); waypoints.arraySize = 4;
            for (int j = 0; j < 4; j++) waypoints.GetArrayElementAtIndex(j).objectReferenceValue =
                Node("R2Drift" + i + "_" + j, new Vector3(-1.8f + (i - 1) * 1.8f, 0.32f, 48 - j * 24));
            properties.ApplyModifiedPropertiesWithoutUndo();
            var it = go.GetComponent<Interactable>(); it.radius = 3; it.clueId = -1; it.toast = "";
            var marker = go.transform.Find("R1Marker");
            marker.localPosition = Vector3.zero; marker.localScale = new Vector3(0.45f, 0.18f, 0.55f);
        }
        R2bSetup.World(flow, dusk);
        EditorUtility.SetDirty(flow); EditorUtility.SetDirty(hud); EditorUtility.SetDirty(map);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        if (!EditorSceneManager.SaveOpenScenes()) throw new IOException("R2 場景儲存失敗。");
        AssetDatabase.SaveAssets();
        File.WriteAllText("logs/U-R2-install.md", "# R2b 安裝\n\nPASS：R1 安裝＋五組地面／法線、兩天空、森林 240＋溪谷 160 卡片、10 UI 圖、2 Mixamo Humanoid 人物（獨立 PNG／動畫控制器）、SeasonSwap／EpilogueDirector、迷霧地圖、18m 攀藤、3 共享收集器漂流物；道具結果見 U-R2-props.md；Main 已儲存。\n");
    }
    static void ConfigureTextures()
    {
        foreach (string file in Directory.GetFiles(Art, "*.png", SearchOption.AllDirectories))
        {
            string path = file.Replace('\\', '/'); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool ui = path.Contains("/ui/"), normal = path.EndsWith("_n.png"), sky = path.Contains("/sky/");
            importer.textureType = ui ? TextureImporterType.Sprite : normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (ui) importer.spriteImportMode = SpriteImportMode.Single;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.maxTextureSize = ui ? 256 : normal ? 512 : sky || path.Contains("atlas") ? 2048 : 1024;
            importer.wrapMode = ui || path.Contains("/foliage/") ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.alphaIsTransparency = ui || path.Contains("/foliage/");
            importer.mipmapEnabled = !ui; importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = !ui && !normal; importer.compressionQuality = 60;
            importer.SaveAndReimport();
        }
    }
    static Material Sky(string name)
    {
        var mat = Material(name, "Skybox/Panoramic"); mat.SetTexture("_MainTex", Texture("sky/" + name + ".png"));
        mat.SetFloat("_Mapping", 1); mat.SetFloat("_ImageType", 0); mat.SetFloat("_Exposure", 1); EditorUtility.SetDirty(mat); return mat;
    }
    static void Ground()
    {
        string[] names = { "grass", "dirt_path", "moss_rock", "riverbed_pebble", "village_stone" };
        var mats = new Material[5]; var layers = new TerrainLayer[5];
        for (int i = 0; i < 5; i++)
        {
            mats[i] = Material(names[i], "Universal Render Pipeline/Lit");
            mats[i].SetTexture("_BaseMap", Texture("ground/" + names[i] + ".png"));
            mats[i].SetTexture("_BumpMap", Texture("ground/" + names[i] + "_n.png"));
            mats[i].EnableKeyword("_NORMALMAP"); mats[i].SetFloat("_BumpScale", 0.3f); mats[i].SetFloat("_Smoothness", 0.12f);
            mats[i].mainTextureScale = Vector2.one * (i == 2 ? 3 : 1); EditorUtility.SetDirty(mats[i]);
            string path = Mats + names[i] + ".terrainlayer";
            layers[i] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (!layers[i]) { layers[i] = new TerrainLayer(); AssetDatabase.CreateAsset(layers[i], path); }
            layers[i].diffuseTexture = Texture("ground/" + names[i] + ".png"); layers[i].normalMapTexture = Texture("ground/" + names[i] + "_n.png");
            layers[i].normalScale = 0.3f; layers[i].tileSize = Vector2.one * (i == 4 ? 6 : 8); EditorUtility.SetDirty(layers[i]);
        }
        var terrain = GameObject.Find("Terrain_240x240").GetComponent<Terrain>(); var data = terrain.terrainData;
        data.terrainLayers = layers; data.alphamapResolution = 256;
        var weights = new float[256, 256, 5];
        for (int z = 0; z < 256; z++) for (int x = 0; x < 256; x++)
        {
            float wx = x * 240f / 255 - 120, wz = z * 240f / 255 - 120;
            int type = Mathf.Abs(wx) > 30 ? 2 : wz > 65 && Mathf.Abs(wx) < 30 ? 4 : wz > -34 && wz < 60 && Mathf.Abs(wx) < 4 ? 3 : 0;
            weights[z, x, type] = 1;
        }
        data.SetAlphamaps(0, 0, weights); EditorUtility.SetDirty(data);
        foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = r.name;
            if (n.Contains("Path"))
            {
                var props = new MaterialPropertyBlock();
                props.SetVector("_BaseMap_ST", new Vector4(Mathf.Max(1, r.bounds.size.x / 4), Mathf.Max(1, r.bounds.size.z / 4), 0, 0));
                r.sharedMaterial = mats[1]; r.SetPropertyBlock(props);
            }
            else if (n == "VillageSquare")
            {
                var props = new MaterialPropertyBlock(); props.SetVector("_BaseMap_ST", new Vector4(r.bounds.size.x / 6, r.bounds.size.z / 6, 0, 0));
                r.sharedMaterial = mats[4]; r.SetPropertyBlock(props);
            }
            else if (n.StartsWith("Cliff_") || n == "R1PeachLanding") r.sharedMaterial = mats[2];
        }
    }
    static void Foliage()
    {
        var entries = JsonUtility.FromJson<AtlasList>("{\"entries\":" + File.ReadAllText(Art + "foliage/atlas_layout.json") + "}").entries;
        var mat = Material("foliage", "Universal Render Pipeline/Lit"); mat.SetTexture("_BaseMap", Texture("foliage/foliage_atlas.png"));
        mat.SetFloat("_AlphaClip", 1); mat.EnableKeyword("_ALPHATEST_ON"); mat.SetFloat("_Cutoff", 0.4f);
        mat.SetFloat("_Cull", 0); mat.SetFloat("_Smoothness", 0); mat.renderQueue = 2450; mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        var meshes = new Mesh[6];
        for (int i = 0; i < 6; i++)
        {
            var uv = entries[i].uv_bottom_left;
            if (uv.Length != 4 || uv[0] < 0 || uv[1] < 0 || uv[0] + uv[2] > 1 || uv[1] + uv[3] > 1) throw new InvalidDataException("圖集 UV 無效。");
            string path = Mats + "foliage_" + entries[i].name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); bool exists = mesh;
            if (!mesh) mesh = new Mesh(); else mesh.Clear();
            mesh.vertices = new[] { new Vector3(-0.5f,0,0),new Vector3(0.5f,0,0),new Vector3(-0.5f,1,0),new Vector3(0.5f,1,0),new Vector3(0,0,-0.5f),new Vector3(0,0,0.5f),new Vector3(0,1,-0.5f),new Vector3(0,1,0.5f) };
            mesh.uv = Enumerable.Range(0, 8).Select(j => new Vector2(uv[0] + (j % 2) * uv[2], uv[1] + (j % 4 / 2) * uv[3])).ToArray();
            mesh.triangles = new[] { 0,2,1,1,2,3,4,6,5,5,6,7 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            if (!exists) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh); meshes[i] = mesh;
        }
        for (int zone = 0; zone < 2; zone++)
        {
            string name = zone == 0 ? "R2ForestFoliage" : "R2GorgeFoliage";
            var old = GameObject.Find(name); if (old) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject(name); root.AddComponent<InstancedGroup>(); var random = new System.Random(271 + zone);
            for (int i = 0; i < (zone == 0 ? 240 : 160); i++)
            {
                float z = zone == 0 ? -104 + (float)random.NextDouble() * 68 : -28 + (float)random.NextDouble() * 85;
                float center = zone == 0 ? 8 * Mathf.Sin((z + 110) * 0.065f) : 0;
                float x = center + (i % 2 == 0 ? -1 : 1) * (zone == 0 ? 4 + (float)random.NextDouble() * 14 : 5 + (float)random.NextDouble() * 8);
                int kind = random.Next(5); var go = new GameObject("Card_" + i, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(root.transform); float y = Terrain.activeTerrain.SampleHeight(new Vector3(x, 0, z)) + Terrain.activeTerrain.transform.position.y;
                go.transform.position = new Vector3(x, y, z); go.transform.rotation = Quaternion.Euler(0, (float)random.NextDouble() * 360, 0);
                go.transform.localScale = Vector3.one * (kind == 4 ? 2.5f : 0.6f + (float)random.NextDouble() * 0.7f);
                go.GetComponent<MeshFilter>().sharedMesh = meshes[kind]; var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off; go.isStatic = false;
            }
        }
    }
    static void Vine(Transform plane)
    {
        var root = GameObject.Find("R2Vines"); if (root) UnityEngine.Object.DestroyImmediate(root);
        root = new GameObject("R2Vines"); root.AddComponent<InstancedGroup>();
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Mats + "foliage_hanging_vine.asset");
        for (int i = 0; i < 6; i++)
        {
            var go = new GameObject("Vine_" + i, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root.transform);
            go.transform.position = plane.position + Vector3.up * i * 3; go.transform.rotation = plane.rotation;
            go.transform.localScale = new Vector3(1.8f, 3, 1.8f); go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Mats + "foliage.mat");
        }
    }
    static Transform Node(string name, Vector3 position) { var go = GameObject.Find(name) ?? new GameObject(name); go.transform.position = position; return go.transform; }
    static T Add<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c ? c : go.AddComponent<T>(); }
    static void Ref(UnityEngine.Object target, string name, UnityEngine.Object value) { var s = new SerializedObject(target); s.FindProperty(name).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
    static void Number(UnityEngine.Object target, string name, float value) { var s = new SerializedObject(target); s.FindProperty(name).floatValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
    static Texture2D Texture(string name) { return AssetDatabase.LoadAssetAtPath<Texture2D>(Art + name) ?? throw new FileNotFoundException(Art + name); }
    static Sprite Sprite(string name) { return AssetDatabase.LoadAssetAtPath<Sprite>(Art + "ui/" + name + ".png") ?? throw new FileNotFoundException(name); }
    static Material Material(string name, string shader)
    {
        string path = Mats + name + ".mat"; var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        var s = Shader.Find(shader); if (!s) throw new InvalidOperationException("找不到 shader " + shader);
        if (!mat) { mat = new Material(s); AssetDatabase.CreateAsset(mat, path); } else mat.shader = s;
        return mat;
    }
}
