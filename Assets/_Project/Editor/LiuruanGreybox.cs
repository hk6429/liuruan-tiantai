using System;
using System.IO;
using System.Linq;
using Liuruan;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>R0 灰盒；由 batchmode -executeMethod LiuruanGreybox.Build 建立。</summary>
public static class LiuruanGreybox
{
    const string Scenes = "Assets/_Project/Scenes";
    const string Materials = "Assets/_Project/Materials/Greybox";
    static Material ground, path, trunk, leaves, rock, water, wall, roof;

    [MenuItem("Tools/Liuruan/Build/Greybox")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Scenes);
        Directory.CreateDirectory(Materials);
        AssetDatabase.Refresh();
        ground = Mat("Ground", new Color(0.39f, 0.49f, 0.23f));
        path = Mat("Path", new Color(0.65f, 0.52f, 0.32f));
        trunk = Mat("Trunk", new Color(0.34f, 0.22f, 0.12f));
        leaves = Mat("Leaves", new Color(0.25f, 0.47f, 0.18f));
        rock = Mat("Rock", new Color(0.40f, 0.42f, 0.38f));
        water = Mat("Stream", new Color(0.15f, 0.49f, 0.68f));
        wall = Mat("Wall", new Color(0.77f, 0.68f, 0.49f));
        roof = Mat("Roof", new Color(0.27f, 0.43f, 0.36f));

        var boot = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var bootCamera = new GameObject("Camera").AddComponent<Camera>();
        bootCamera.clearFlags = CameraClearFlags.SolidColor;
        bootCamera.backgroundColor = new Color(0.12f, 0.09f, 0.07f);
        new GameObject("BootLoader", typeof(BootLoader));
        Save(boot, Scenes + "/Boot.unity");

        var main = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TerrainGround();
        var forest = Empty("Forest", new Vector3(0, 0, -76));
        var gorge = Empty("Gorge", Vector3.zero);
        var village = Empty("Village", new Vector3(0, 0, 84));
        var markers = Empty("ActPoints", Vector3.zero);

        Vector3 previous = ForestPath(-110);
        for (int z = -106; z <= -34; z += 4)
        {
            var next = ForestPath(z);
            var segment = Primitive("ForestPath", PrimitiveType.Cube, (previous + next) * 0.5f + Vector3.up * 0.025f,
                new Vector3(4.5f, 0.05f, Vector3.Distance(previous, next) + 0.15f), path, forest);
            segment.transform.rotation = Quaternion.LookRotation(next - previous);
            previous = next;
        }
        for (int i = 0; i < 24; i++)
        {
            float z = -102 + i * 2.8f;
            var p = ForestPath(z);
            Tree("ForestTree_" + i, p + Vector3.right * (i % 2 == 0 ? -7 - i % 3 : 7 + i % 3), forest);
        }
        House("HomeVillage_West", new Vector3(-14, 0, -109), forest);
        House("HomeVillage_East", new Vector3(14, 0, -109), forest);
        Primitive("VillageGate_Left", PrimitiveType.Cube, new Vector3(-4, 2, -108), new Vector3(0.6f, 4, 0.6f), trunk, forest);
        Primitive("VillageGate_Right", PrimitiveType.Cube, new Vector3(4, 2, -108), new Vector3(0.6f, 4, 0.6f), trunk, forest);
        Primitive("VillageGate_Top", PrimitiveType.Cube, new Vector3(0, 4, -108), new Vector3(9, 0.5f, 0.6f), trunk, forest);

        for (int i = 0; i < 6; i++)
        {
            float z = -25 + i * 14;
            Primitive("Cliff_West_" + i, PrimitiveType.Cube, new Vector3(-22, 9 + i % 2, z), new Vector3(14, 18 + 2 * (i % 2), 13), rock, gorge);
            Primitive("Cliff_East_" + i, PrimitiveType.Cube, new Vector3(22, 11 - i % 2, z), new Vector3(14, 22 - 2 * (i % 2), 13), rock, gorge);
        }
        var stream = Primitive("Stream_NorthToSouth", PrimitiveType.Plane, new Vector3(0, 0.08f, 13), new Vector3(0.8f, 1, 9.4f), water, gorge);
        Object.DestroyImmediate(stream.GetComponent<Collider>());
        // shortcut: 靜態箭頭標示北往南水流；R1 接入漂流物時再加水面動畫。
        for (int i = 0; i < 7; i++)
        {
            float z = -25 + i * 13;
            for (int side = -1; side <= 1; side += 2)
            {
                var arrow = Primitive("Flow_South_" + i + "_" + side, PrimitiveType.Cube,
                    new Vector3(side * 0.5f, 0.10f, z), new Vector3(0.12f, 0.02f, 1.4f), wall, gorge);
                arrow.transform.rotation = Quaternion.Euler(0, side * 45, 0);
                Object.DestroyImmediate(arrow.GetComponent<Collider>());
            }
        }
        Tree("PeachTree", new Vector3(-18, 18, -24), gorge);
        Primitive("Gorge_WestPath", PrimitiveType.Cube, new Vector3(-9, 0.03f, 12), new Vector3(4.5f, 0.06f, 94), path, gorge);
        Primitive("VillagePath", PrimitiveType.Cube, new Vector3(-4.5f, 0.03f, 63), new Vector3(13.5f, 0.06f, 7), path, village);
        Primitive("VillageSquare", PrimitiveType.Cube, new Vector3(0, 0.03f, 87), new Vector3(28, 0.06f, 38), path, village);
        for (int i = 0; i < 6; i++) House("House_" + i, new Vector3(i % 2 == 0 ? -18 : 18, 0, 75 + (i / 2) * 15), village);
        Primitive("VillageBoard", PrimitiveType.Cube, new Vector3(-8, 1.5f, 85), new Vector3(3, 2, 0.25f), trunk, village);

        Marker("Act1_Start", new Vector3(0, 0, -108), markers);
        for (int i = 0; i < 5; i++) Marker("Act1_Bark_" + (i + 1), ForestPath(-94 + i * 12) + Vector3.right * (i % 2 == 0 ? -5 : 5), markers);
        Marker("Act2_ClimbBase", new Vector3(-13, 0, -31), markers);
        Marker("Act2_PeachTree", new Vector3(-18, 18, -24), markers);
        for (int i = 0; i < 3; i++) Marker("Act3_Drift_" + (i + 1), new Vector3(-2, 0.12f, -13 + i * 18), markers);
        Marker("Act3_Wade", new Vector3(-3, 0.12f, 48), markers);
        Marker("Act4_Women", new Vector3(0, 0, 65), markers);
        Marker("Act4_House", new Vector3(-18, 0, 75), markers);
        Marker("Act4_Board", new Vector3(-8, 0, 83), markers);
        Marker("Act5_Return", new Vector3(0, 0, -108), markers);

        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        var cam = camera.GetComponent<Camera>();
        cam.fieldOfView = 55;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 350;
        cam.backgroundColor = new Color(0.65f, 0.78f, 0.82f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camera.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0, 0.1f, -105);
        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f; controller.radius = 0.3f; controller.center = new Vector3(0, 0.9f, 0);
        controller.stepOffset = 0.3f;
        Primitive("劉晨", PrimitiveType.Capsule, new Vector3(0, 1, -105), new Vector3(0.65f, 0.9f, 0.65f), Mat("Player", new Color(0.66f, 0.33f, 0.19f)), player.transform, false);
        var pc = player.AddComponent<PlayerController>();
        pc.cam = camera.transform; pc.camDistance = 3.6f; pc.camHeight = 1.2f;
        camera.transform.SetPositionAndRotation(player.transform.position + new Vector3(0, 2.43f, -3.38f), Quaternion.Euler(20, 0, 0));
        Primitive("Companion", PrimitiveType.Capsule, new Vector3(1.7f, 0.9f, -106), new Vector3(0.65f, 0.9f, 0.65f), Mat("Companion", new Color(0.26f, 0.41f, 0.59f)), null);
        var sun = new GameObject("Warm Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(35, -35, 0);
        sun.color = new Color(1, 0.87f, 0.67f); sun.intensity = 1.4f; sun.shadows = LightShadows.Soft;
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.58f, 0.65f, 0.69f);
        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/MainVolumeProfile.asset");
        new GameObject("AudioManager", typeof(AudioManager));
        var ui = new GameObject("ScrollUI").AddComponent<ScrollUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/NotoSerifTC Static SDF.asset");
        so.ApplyModifiedPropertiesWithoutUndo();
        // R0 不掛 ClueManager：題庫由 C 線交付後再綁定，避免空資料啟動錯誤。
        Validate(main);
        Save(main, Scenes + "/Main.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scenes + "/Boot.unity", true), new EditorBuildSettingsScene(Scenes + "/Main.unity", true) };
        Debug.Log(Liuruan.EditorTools.FontSubset.RebuildNoDialog());
        AssetDatabase.SaveAssets();
        Debug.Log("[LiuruanGreybox] 已建立 Boot/Main、240×240 地形、三區與 16 個五幕定位點。");
    }

    static void TerrainGround()
    {
        const string assetPath = Scenes + "/GreyboxTerrain.asset";
        var data = AssetDatabase.LoadAssetAtPath<TerrainData>(assetPath);
        if (!data) { data = new TerrainData(); AssetDatabase.CreateAsset(data, assetPath); }
        data.heightmapResolution = 129;
        data.size = new Vector3(240, 30, 240);
        var heights = new float[129, 129];
        for (int z = 0; z < 129; z++)
            for (int x = 0; x < 129; x++)
            {
                float wx = x * 240f / 128 - 120, wz = z * 240f / 128 - 120;
                float hill = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(35, 100, Mathf.Abs(wx))) * 8 * (0.7f + 0.3f * Mathf.Sin(wz * 0.08f));
                float bed = Mathf.Abs(wx) < 3 && wz > -34 && wz < 60 ? -0.4f : 0;
                heights[z, x] = (2 + hill + bed) / 30;
            }
        data.SetHeights(0, 0, heights);
        const string layerPath = Scenes + "/GreyboxGround.terrainlayer";
        var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
        if (!layer) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, layerPath); }
        const string texturePath = Scenes + "/GreyboxGround.asset";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (!texture) { texture = new Texture2D(1, 1); AssetDatabase.CreateAsset(texture, texturePath); }
        texture.SetPixel(0, 0, ground.color); texture.Apply();
        layer.diffuseTexture = texture; layer.tileSize = new Vector2(8, 8);
        data.terrainLayers = new[] { layer };
        data.alphamapResolution = 32;
        var splat = new float[32, 32, 1];
        for (int z = 0; z < 32; z++)
            for (int x = 0; x < 32; x++) splat[z, x, 0] = 1;
        data.SetAlphamaps(0, 0, splat);
        EditorUtility.SetDirty(texture); EditorUtility.SetDirty(layer);
        EditorUtility.SetDirty(data);
        var terrain = Terrain.CreateTerrainGameObject(data);
        terrain.name = "Terrain_240x240"; terrain.transform.position = new Vector3(-120, -2, -120);
        terrain.GetComponent<Terrain>().materialTemplate = Mat("Terrain", ground.color, "Universal Render Pipeline/Terrain/Lit");
    }

    static Material Mat(string name, Color color, string shaderName = "Universal Render Pipeline/Lit")
    {
        string assetPath = Materials + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        var shader = Shader.Find(shaderName);
        if (!shader) throw new InvalidOperationException("找不到 Shader：" + shaderName);
        if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, assetPath); }
        material.shader = shader;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    static Transform Empty(string name, Vector3 position, Transform parent = null)
    {
        var t = new GameObject(name).transform; t.SetParent(parent); t.position = position; return t;
    }
    static void Marker(string name, Vector3 position, Transform parent) => Empty(name, position, parent);
    static Vector3 ForestPath(float z) => new Vector3(8 * Mathf.Sin((z + 110) * 0.065f), 0, z);
    static GameObject Primitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material material, Transform parent, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent);
        go.transform.position = pos; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static void Tree(string name, Vector3 pos, Transform parent)
    {
        var t = Empty(name, pos, parent);
        Primitive("Trunk", PrimitiveType.Cylinder, pos + Vector3.up * 2, new Vector3(0.6f, 2, 0.6f), trunk, t);
        Primitive("Canopy", PrimitiveType.Sphere, pos + Vector3.up * 4.8f, new Vector3(4, 4, 4), leaves, t, false);
    }
    static void House(string name, Vector3 pos, Transform parent)
    {
        var t = Empty(name, pos, parent);
        Primitive("Walls", PrimitiveType.Cube, pos + Vector3.up * 2, new Vector3(7, 4, 7), wall, t);
        const string meshPath = Scenes + "/GreyboxRoof.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (!mesh)
        {
            mesh = new Mesh { name = "WedgeRoof" };
            mesh.vertices = new[] { new Vector3(-4, 0, -4), new Vector3(4, 0, -4), new Vector3(0, 2, -4), new Vector3(-4, 0, 4), new Vector3(4, 0, 4), new Vector3(0, 2, 4) };
            mesh.triangles = new[] { 0, 2, 1, 3, 4, 5, 0, 3, 5, 0, 5, 2, 1, 2, 5, 1, 5, 4, 0, 1, 4, 0, 4, 3 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, meshPath);
        }
        var top = new GameObject("WedgeRoof", typeof(MeshFilter), typeof(MeshRenderer));
        top.transform.SetParent(t); top.transform.position = pos + Vector3.up * 4;
        top.GetComponent<MeshFilter>().sharedMesh = mesh; top.GetComponent<MeshRenderer>().sharedMaterial = roof;
    }
    static void Save(Scene scene, string path)
    {
        if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("場景儲存失敗：" + path);
    }
    static void Validate(Scene scene)
    {
        var objects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (string zone in new[] { "Forest", "Gorge", "Village" })
            if (objects.Count(t => t.name == zone) != 1) throw new InvalidOperationException("區域數量錯誤：" + zone);
        if (objects.Count(t => t.name.StartsWith("Act", StringComparison.Ordinal) && t.name != "ActPoints") != 16)
            throw new InvalidOperationException("五幕定位點必須有 16 個");
        if (objects.Single(t => t.name == "Act1_Start").position != objects.Single(t => t.name == "Act5_Return").position)
            throw new InvalidOperationException("歸鄉必須回到原村口");
        if (objects.Single(t => t.name == "Player").GetComponent<PlayerController>() == null || Camera.main == null)
            throw new InvalidOperationException("玩家或攝影機缺漏");
    }
}
