using System;
using System.IO;
using System.Linq;
using System.Text;
using Liuruan;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.AI.Navigation;

public static class R3bSetup
{
    const string Art = "Assets/_Project/Art/R3/";
    static StringBuilder report;
    public static void Integrate()
    {
        report = new StringBuilder("# R3b 素材與場景安裝\n\n");
        var flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
        var j = Add<R3Journey>(flow.gameObject); flow.journey = j;
        foreach (string file in Directory.GetFiles("docs/content/r3", "*.json")) Copy(file, "Assets/_Project/Data/r3/" + Path.GetFileName(file));
        Copy("docs/content/r3/narration.json", "Assets/_Project/Data/narration.json");
        foreach (string name in new[] { "story_1", "story_2", "story_3", "portrait_liuchen", "portrait_ruanzhao", "portrait_woman_a", "portrait_woman_b", "portrait_grandson", "icon_arrow", "card_frame" })
        {
            Copy("staging/art/R3/" + name + ".png", Art + name + ".png");
            var imp = (TextureImporter)AssetImporter.GetAtPath(Art + name + ".png"); imp.textureType = TextureImporterType.Sprite; imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false; imp.maxTextureSize = name.StartsWith("story") ? 2048 : 512;
            if (name == "card_frame") imp.spriteBorder = new Vector4(60, 60, 60, 60);
            imp.SaveAndReimport();
        }
        j.storyJson = Data("story_card"); j.narrationJson = Data("narration"); j.questionsJson = Data("questions_r3"); j.guideJson = Data("epilogue_guide");
        flow.epilogue.additionalCluesJson = j.questionsJson; EditorUtility.SetDirty(flow.epilogue);
        j.stories = new[] { Sprite("story_1"), Sprite("story_2"), Sprite("story_3") };
        j.portraits = new[] { Sprite("portrait_liuchen"), Sprite("portrait_ruanzhao"), Sprite("portrait_woman_a"), Sprite("portrait_woman_b"), Sprite("portrait_grandson") };
        j.arrowSprite = Sprite("icon_arrow"); j.cardFrame = Sprite("card_frame");
        j.dialogue = Add<DialogueDirector>(flow.gameObject); Ref(j.dialogue, "dialogueJson", Data("dialogue"));
        j.objectives = Add<ObjectiveTracker>(flow.gameObject); Ref(j.objectives, "objectivesJson", Data("objectives"));
        j.hints = Add<StallHintSystem>(flow.gameObject); Ref(j.hints, "hintsJson", Data("hints"));
        j.reading = Add<ReadingDirector>(flow.gameObject); Ref(j.reading, "sentencesJson", Data("sentences")); Ref(j.reading, "glossaryJson", Data("glossary_cards"));
        j.rescue = Add<RescueSystem>(flow.gameObject);
        var so = new SerializedObject(j.rescue); so.FindProperty("initialSafePoint").vector3Value = GameFlow.ActStart(1);
        so.FindProperty("minXZ").vector2Value = new Vector2(-117, -117); so.FindProperty("maxXZ").vector2Value = new Vector2(117, 117); so.ApplyModifiedPropertiesWithoutUndo();
        j.arrow = Add<OffscreenArrow>(flow.gameObject); Ref(j.arrow, "viewCamera", Camera.main);
        j.water = GameObject.Find("Stream_NorthToSouth").GetComponent<Renderer>();
        var points = GameObject.Find("ActPoints").transform;
        Remove(points.Find("Act4_Board")); Remove(GameObject.Find("VillageBoard")?.transform);
        var housePoint = points.Find("Act4_House"); var doorPosition = housePoint.position;
        j.door = Point("Act4_Home", doorPosition, R3Journey.DoorPrompt, 2.2f).transform;
        // 將灰盒實心牆改成三面牆與東側門洞；保留 R3a 的東側安全門前站點。
        var house = GameObject.Find("Village/House_0").transform;
        var solid = house.Find("Walls").GetComponent<BoxCollider>(); var b = solid.bounds; solid.enabled = false;
        Remove(house.Find("R3Interior"));
        var interior = new GameObject("R3Interior").transform; interior.SetParent(house, true);
        Box("West", new Vector3(b.min.x, b.center.y, b.center.z), new Vector3(.2f,b.size.y,b.size.z), interior, true);
        Box("North", new Vector3(b.center.x,b.center.y,b.max.z), new Vector3(b.size.x,b.size.y,.2f), interior, true);
        Box("South", new Vector3(b.center.x,b.center.y,b.min.z), new Vector3(b.size.x,b.size.y,.2f), interior, true);
        foreach (int side in new[] { -1, 1 }) Box("DoorSide" + side, new Vector3(b.max.x,b.center.y,b.center.z + side * (b.size.z / 4 + .7f)), new Vector3(.2f,b.size.y,b.size.z / 2 - 1.4f), interior, true);
        foreach (var r in house.GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var r in interior.GetComponentsInChildren<Renderer>()) { r.enabled = true; r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Greybox/Wall.mat"); }
        var roof = house.Find("WedgeRoof"); if (roof) roof.GetComponent<Renderer>().enabled = true;
        housePoint.position = new Vector3(b.center.x + 1.3f,.2f,b.center.z); housePoint.GetComponent<Interactable>().radius = 1.3f;
        housePoint.GetComponent<Interactable>().prompt = R3Journey.HousePrompt;
        Point("Act4_Return", doorPosition + new Vector3(1,0,2), R3Journey.ReturnPrompt, 3.5f);
        Point("Act5_Villager", new Vector3(-2,.2f,-100), R3Journey.VillagerPrompt, 2.4f);
        Point("Act5_Grandson", new Vector3(2,.2f,-96), R3Journey.GrandsonPrompt, 2.4f);
        var npcRoot = GameObject.Find("R3NPC"); if (npcRoot) UnityEngine.Object.DestroyImmediate(npcRoot);
        npcRoot = new GameObject("R3NPC");
        j.womanA = Npc("woman_a", new Vector3(-1,.2f,65), 1.55f, 4, npcRoot.transform);
        j.womanB = Npc("woman_b", new Vector3(1,.2f,65), 1.55f, 4, npcRoot.transform);
        Npc("grandson", new Vector3(2,.2f,-96), 1.65f, 5, npcRoot.transform);
        foreach (var r in points.Find("Act4_Women").GetComponentsInChildren<Renderer>()) r.enabled = false;
        Remove(flow.transform.Find("R3Meal"));
        j.meal = new GameObject("R3Meal"); j.meal.transform.SetParent(flow.transform); j.meal.transform.position = new Vector3(b.center.x - .7f,.2f,b.center.z);
        Box("Table", j.meal.transform.position + Vector3.up * .6f, new Vector3(1.4f,.12f,1), j.meal.transform, true);
        Box("胡麻飯", j.meal.transform.position + new Vector3(-.3f,.78f,0), new Vector3(.4f,.2f,.4f), j.meal.transform, false);
        Box("山羊脯", j.meal.transform.position + new Vector3(.3f,.76f,0), new Vector3(.4f,.12f,.3f), j.meal.transform, false);
        var mealLabel = new GameObject("MealLabel", typeof(TMPro.TextMeshPro)); mealLabel.transform.SetParent(j.meal.transform,false); mealLabel.transform.localPosition = Vector3.up * 1.3f;
        var text = mealLabel.GetComponent<TMPro.TextMeshPro>(); text.font = flow.GetComponent<HudUI>().font; text.text = R3Journey.MealText; text.fontSize = 2.5f; text.alignment = TMPro.TextAlignmentOptions.Center; text.rectTransform.sizeDelta = new Vector2(4,1); mealLabel.AddComponent<R3Billboard>();
        j.meal.SetActive(false);
        foreach (Transform point in points)
        {
            if (!point.GetComponent<Interactable>()) continue;
            Remove(point.Find("R3Glow"));
            var glow = GameObject.CreatePrimitive(PrimitiveType.Cylinder); glow.name = "R3Glow"; glow.transform.SetParent(point,false);
            glow.transform.localPosition = Vector3.up * 2.5f; glow.transform.localScale = new Vector3(.35f,2.5f,.35f);
            UnityEngine.Object.DestroyImmediate(glow.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.SetColor("_BaseColor", new Color(1,.77f,.17f));
            string path = Art + "glow.mat";
            if (!AssetDatabase.LoadAssetAtPath<Material>(path)) AssetDatabase.CreateAsset(mat,path); else { UnityEngine.Object.DestroyImmediate(mat); mat = AssetDatabase.LoadAssetAtPath<Material>(path); }
            glow.GetComponent<Renderer>().sharedMaterial = mat; glow.AddComponent<FindGlow>().Billboard = false; glow.AddComponent<Light>().range = 5;
        }
        Prop("peach_tree", "Act2_PeachTree", 4);
        for (int i = 1; i <= 5; i++) Prop("chu_tree", "Act1_Bark_" + i, 5);
        Prop("drift_leaf", "Act3_Drift_1", .35f); Prop("drift_rice", "Act3_Drift_3", .35f);
        // 場景新增門洞後重烘 NavMesh，驗收仍使用 CharacterController 真正走入屋內。
        Physics.SyncTransforms();
        foreach (var surface in UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None)) surface.BuildNavMesh();
        EditorUtility.SetDirty(j); EditorUtility.SetDirty(flow);
        var main = EditorSceneManager.GetActiveScene();
        var boot = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity", OpenSceneMode.Additive);
        var loader = boot.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<BootLoader>(true)).Single();
        loader.chineseFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Fonts/NotoSerifTC-Regular.otf");
        if (!loader.chineseFont) throw new InvalidDataException("Boot 缺少中文字型。");
        EditorSceneManager.MarkSceneDirty(boot); EditorSceneManager.SaveScene(boot); EditorSceneManager.CloseScene(boot,true); UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
        File.WriteAllText("logs/U-R3b-assets.md", report.ToString());
    }
    static void Prop(string key, string point, float height)
    {
        var root = GameObject.Find("ActPoints/" + point).transform;
        var model = Model("props", key);
        if (!model) { report.AppendLine("- 備援 " + key + "：模型未到；保留既有外觀。"); return; }
        Remove(root.Find("R2Prop"));
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(model); visual.name = "R2Prop"; visual.transform.SetParent(root,false); Fit(visual.transform,height,materials[key]);
    }
    static R3Npc Npc(string key, Vector3 position, float height, int act, Transform parent)
    {
        var root = new GameObject(key); root.transform.SetParent(parent); root.transform.position = position;
        var npc = root.AddComponent<R3Npc>(); npc.act = act;
        var model = Model("npc",key); GameObject visual;
        if (model) { visual = (GameObject)PrefabUtility.InstantiatePrefab(model); visual.transform.SetParent(root.transform,false); Fit(visual.transform,height,materials[key]); }
        else
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Capsule); visual.transform.SetParent(root.transform,false);
            visual.transform.localScale = new Vector3(.5f,height / 2,.5f); visual.transform.localPosition = Vector3.up * height / 2;
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Greybox/Companion.mat");
            var face = new GameObject("PortraitBillboard", typeof(SpriteRenderer)); face.transform.SetParent(root.transform,false); face.transform.localPosition = Vector3.up * (height + .35f);
            face.transform.localScale = Vector3.one * .15f; face.GetComponent<SpriteRenderer>().sprite = Sprite("portrait_" + key); face.AddComponent<R3Billboard>();
            report.AppendLine("- 備援 " + key + "：FBX／albedo 未齊，用膠囊＋頭像 billboard。");
        }
        visual.name = "Visual"; visual.AddComponent<CharacterBob>(); return npc;
    }
    static GameObject Model(string group, string key)
    {
        string source = "staging/models/" + group + "/" + key;
        if (!File.Exists(source + ".fbx") || !File.Exists(source + "_albedo.png")) return null;
        string folder = group == "npc" ? "Assets/_Project/Art/NPC/" : "Assets/_Project/Art/Props/R3/";
        Copy(source + ".fbx", folder + key + ".fbx"); Copy(source + "_albedo.png",folder + key + "_albedo.png");
        var importer = (ModelImporter)AssetImporter.GetAtPath(folder + key + ".fbx"); importer.materialImportMode = ModelImporterMaterialImportMode.None; importer.importAnimation = false; importer.SaveAndReimport();
        var material = AssetDatabase.LoadAssetAtPath<Material>(folder + key + ".mat");
        if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, folder + key + ".mat"); }
        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder + key + "_albedo.png")); EditorUtility.SetDirty(material);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(folder + key + ".fbx");
        // 材質套用在實例，避免修改匯入的唯讀 FBX。
        materials[key] = material; report.AppendLine("- 模型 " + key + "：FBX＋URP Lit＋albedo。"); return model;
    }
    static readonly System.Collections.Generic.Dictionary<string, Material> materials = new System.Collections.Generic.Dictionary<string, Material>();
    static void Fit(Transform root, float height, Material material)
    {
        var rs = root.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) throw new InvalidDataException(root.name + " 沒有 Renderer");
        Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        if (b.size.y <= .0001f) throw new InvalidDataException(root.name + " 模型高度為零");
        root.localScale *= height / b.size.y;
        b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); root.position += new Vector3(root.parent.position.x - b.center.x, root.parent.position.y - b.min.y, root.parent.position.z - b.center.z);
        b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        if (Mathf.Abs(b.size.y - height) > .01f || Mathf.Abs(b.min.y - root.parent.position.y) > .01f) throw new InvalidDataException("模型尺寸／腳底校準失敗。");
        foreach (var r in rs) r.sharedMaterials = Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();
        foreach (var c in root.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
    }
    static Interactable Point(string name, Vector3 pos, string prompt, float radius)
    {
        var root = GameObject.Find("ActPoints").transform; var t = root.Find(name);
        if (!t) { t = new GameObject(name).transform; t.SetParent(root); }
        t.position = pos; var it = Add<Interactable>(t.gameObject); it.prompt = prompt; it.radius = radius; it.clueId = -1; it.toast = ""; return it;
    }
    static void Box(string name, Vector3 position, Vector3 size, Transform parent, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent,true); go.transform.position = position; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Greybox/" + (name == "Table" || name == "山羊脯" ? "Trunk" : "Wall") + ".mat");
        if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
    }
    static void Remove(Transform t) { if (t) UnityEngine.Object.DestroyImmediate(t.gameObject); }
    static T Add<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? go.AddComponent<T>();
    static void Copy(string source, string path) { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.Copy(source,path,true); AssetDatabase.ImportAsset(path); }
    static TextAsset Data(string name) => AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/r3/" + name + ".json");
    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");
    static void Ref(UnityEngine.Object obj, string field, UnityEngine.Object value) { var so = new SerializedObject(obj); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
}
