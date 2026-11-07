using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Liuruan;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class R2bSetup
{
    const string Art = "Assets/_Project/Art/";
    static void Copy(string source, string dest)
    {
        if (!File.Exists(source)) throw new FileNotFoundException(source);
        Directory.CreateDirectory(Path.GetDirectoryName(dest));
        if (!File.Exists(dest) || !File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(dest))) File.Copy(source, dest, true);
        AssetDatabase.ImportAsset(dest, ImportAssetOptions.ForceSynchronousImport);
    }
    static Material Skin(string source, string folder, string key)
    {
        string texture = folder + "/" + key + "_albedo.png";
        Copy(source, texture);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texture);
        importer.textureType = TextureImporterType.Default; importer.textureShape = TextureImporterShape.Texture2D;
        importer.maxTextureSize = 1024; importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
        string path = folder + "/" + key + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
        mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_Smoothness", .12f); EditorUtility.SetDirty(mat);
        return mat;
    }
    public static void Character(GameObject root, string key)
    {
        string folder = Art + "Characters/" + key;
        var clips = new Dictionary<string, AnimationClip>();
        Avatar avatar = null;
        foreach (string action in key == "liuchen" ? new[] { "idle", "walk", "run", "climb", "pickup", "pickfruit" } : new[] { "idle", "walk", "run", "climb" })
        {
            string path = folder + "/" + key + "@" + action + ".fbx";
            Copy("staging/models/mixamo/" + key + "/" + key + "@" + action + ".fbx", path);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = action == "idle" ? ModelImporterAvatarSetup.CreateFromThisModel : ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = action == "idle" ? null : avatar;
            importer.importAnimation = true; importer.importCameras = false; importer.importLights = false;
            importer.SaveAndReimport();
            var settings = importer.defaultClipAnimations;
            if (settings.Length == 0) throw new InvalidDataException(path + " 缺少動畫。");
            foreach (var clip in settings)
            {
                clip.name = action;
                clip.loopTime = action == "idle" || action == "walk" || action == "run" || action == "climb";
                clip.lockRootRotation = true;
                // Climb translation is extracted and discarded by applyRootMotion=false; ClimbZone owns world movement.
                clip.lockRootHeightY = action != "climb"; clip.lockRootPositionXZ = action != "climb";
                if (action == "climb") { clip.keepOriginalPositionY = true; clip.keepOriginalPositionXZ = true; }
            }
            importer.clipAnimations = settings; importer.SaveAndReimport();
            if (action == "idle")
            {
                avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                if (!avatar || !avatar.isValid || !avatar.isHuman) throw new InvalidDataException(key + " Humanoid Avatar 無效。");
            }
            clips[action] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
        }
        var mat = Skin("staging/models/game/" + key + "_albedo.png", folder, key);
        string controllerPath = folder + "/" + key + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        // Rebuild only this generated controller, preserving its GUID and scene references.
        foreach (var layer in controller.layers) foreach (var child in layer.stateMachine.states) layer.stateMachine.RemoveState(child.state);
        foreach (var tree in AssetDatabase.LoadAllAssetsAtPath(controllerPath).OfType<BlendTree>()) UnityEngine.Object.DestroyImmediate(tree, true);
        controller.parameters = new AnimatorControllerParameter[0];
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float); controller.AddParameter("Climbing", AnimatorControllerParameterType.Bool);
        var machine = controller.layers[0].stateMachine;
        var locomotion = machine.AddState("Locomotion"); machine.defaultState = locomotion;
        var blend = new BlendTree { name = "Speed", blendParameter = "Speed", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(blend, controller); blend.AddChild(clips["idle"], 0); blend.AddChild(clips["walk"], 1.5f); blend.AddChild(clips["run"], 4);
        locomotion.motion = blend;
        var climb = machine.AddState("Climb"); climb.motion = clips["climb"];
        var enter = locomotion.AddTransition(climb); enter.hasExitTime = false; enter.duration = .12f; enter.AddCondition(AnimatorConditionMode.If, 0, "Climbing");
        var exit = climb.AddTransition(locomotion); exit.hasExitTime = false; exit.duration = .12f; exit.AddCondition(AnimatorConditionMode.IfNot, 0, "Climbing");
        foreach (string action in key == "liuchen" ? new[] { "Pickup", "PickFruit" } : new string[0])
        {
            controller.AddParameter(action, AnimatorControllerParameterType.Trigger);
            var state = machine.AddState(action); state.motion = clips[action.ToLowerInvariant()];
            var transition = locomotion.AddTransition(state); transition.hasExitTime = false; transition.duration = .08f;
            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "Climbing"); transition.AddCondition(AnimatorConditionMode.If, 0, action);
            var back = state.AddTransition(locomotion); back.hasExitTime = true; back.exitTime = .95f; back.duration = .12f;
        }
        if (blend.children.Length != 3 || blend.children[0].threshold != 0 || blend.children[1].threshold != 1.5f || blend.children[2].threshold != 4)
            throw new InvalidDataException(key + " Speed 混合樹閾值錯誤。");
        EditorUtility.SetDirty(controller);
        var old = root.transform.Find("R2Visual"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        var pivot = new GameObject("R2Visual"); pivot.transform.SetParent(root.transform, false);
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/" + key + "@idle.fbx"));
        visual.transform.SetParent(pivot.transform, false);
        Fit(pivot.transform, 1.6f);
        foreach (var renderer in pivot.GetComponentsInChildren<Renderer>()) renderer.sharedMaterials = Enumerable.Repeat(mat, renderer.sharedMaterials.Length).ToArray();
        var animator = visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
        animator.avatar = avatar; animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
        if (root.TryGetComponent<PlayerController>(out var player)) player.animator = animator;
        if (root.TryGetComponent<CompanionFollow>(out var companion)) companion.animator = animator;
        foreach (var bob in root.GetComponentsInChildren<R2CharacterMotion>(true)) UnityEngine.Object.DestroyImmediate(bob);
        foreach (var bob in root.GetComponentsInChildren<CharacterBob>(true)) UnityEngine.Object.DestroyImmediate(bob);
    }
    static Bounds Bounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) throw new InvalidDataException(root.name + " 無 Renderer。");
        var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); return bounds;
    }
    static void Fit(Transform root, float height)
    {
        root.localScale = new Vector3(1 / root.parent.lossyScale.x, 1 / root.parent.lossyScale.y, 1 / root.parent.lossyScale.z);
        var bounds = Bounds(root); if (bounds.size.y <= .0001f) throw new InvalidDataException(root.name + " 高度無效。");
        root.localScale *= height / bounds.size.y; bounds = Bounds(root);
        root.position += new Vector3(root.parent.position.x - bounds.center.x, root.parent.position.y - bounds.min.y, root.parent.position.z - bounds.center.z);
        bounds = Bounds(root);
        if (Mathf.Abs(bounds.size.y - height) > .01f || Mathf.Abs(bounds.min.y - root.parent.position.y) > .01f) throw new InvalidDataException(root.name + " 尺寸校準失敗。");
    }
    static void Set(UnityEngine.Object obj, string field, UnityEngine.Object value)
    { var so = new SerializedObject(obj); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void Renderers(SeasonSwap seasons, string field, Renderer[] renderers)
    {
        var so = new SerializedObject(seasons); var array = so.FindProperty(field); array.arraySize = renderers.Length;
        for (int i = 0; i < renderers.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    public static void World(GameFlow flow, Material sky)
    {
        var village = GameObject.Find("Village");
        var home = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "R2HomeVillage");
        if (!home) home = new GameObject("R2HomeVillage").transform;
        home.gameObject.SetActive(true);
        foreach (string name in new[] { "HomeVillage_West", "HomeVillage_East", "VillageGate_Left", "VillageGate_Right", "VillageGate_Top" })
        {
            var obj = GameObject.Find(name); if (obj) obj.transform.SetParent(home, true);
        }
        for (int i = 0; i < 2; i++)
        {
            string name = "HomeHouse_" + i;
            if (!home.Find(name))
            {
                var house = UnityEngine.Object.Instantiate(village.transform.Find("House_" + i).gameObject, home);
                house.name = name; house.transform.position = new Vector3(i % 2 == 0 ? -18 : 18, 0, -91 + (i / 2) * 14);
            }
        }
        var report = new System.Text.StringBuilder("# R2b 道具安裝\n\n");
        foreach (string key in new[] { "house_tongwa", "pavilion_luozhang", "peach_tree", "chu_tree", "drift_items", "home_house", "vine_cliff", "village_board" })
        {
            string source = "staging/models/props/" + key;
            if (!File.Exists(source + ".fbx") || !File.Exists(source + "_albedo.png")) { report.AppendLine("- SKIP " + key + "：缺少 FBX 或 albedo PNG。"); continue; }
            string folder = Art + "Props/" + key, path = folder + "/" + key + ".fbx";
            Copy(source + ".fbx", path);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path); importer.materialImportMode = ModelImporterMaterialImportMode.None; importer.importAnimation = false; importer.SaveAndReimport();
            var mat = Skin(source + "_albedo.png", folder, key);
            var targets = new List<Transform>();
            if (key == "house_tongwa") for (int i = 0; i < 4; i++) targets.Add(village.transform.Find("House_" + i));
            if (key == "home_house")
            {
                for (int i = 0; i < 2; i++) targets.Add(home.Find("HomeHouse_" + i));
                foreach (string name in new[] { "HomeVillage_West", "HomeVillage_East" }) if (home.Find(name)) targets.Add(home.Find(name));
            }
            if (key == "chu_tree") for (int i = 1; i <= 5; i++) targets.Add(GameObject.Find("ActPoints/Act1_Bark_" + i).transform);
            if (key == "drift_items") for (int i = 1; i <= 3; i++) targets.Add(GameObject.Find("ActPoints/Act3_Drift_" + i).transform);
            if (key == "village_board") targets.Add(GameObject.Find("ActPoints/Act4_Board").transform);
            if (key == "vine_cliff") targets.Add(GameObject.Find("ActPoints/Act2_ClimbBase").transform);
            if (key == "peach_tree")
            {
                targets.Add(GameObject.Find("ActPoints/Act2_PeachTree").transform);
                for (int i = 0; i < 3; i++) targets.Add(Node("R2Peach_" + i, new Vector3(-20 + i * 3, 18, -26 + i * 2), GameObject.Find("Gorge").transform));
            }
            if (key == "pavilion_luozhang") targets.Add(Node("R2Pavilion", new Vector3(0, 0, 90), village.transform));
            float height = key == "peach_tree" ? 4 : key == "village_board" ? 2.2f : key == "drift_items" ? .35f : key == "vine_cliff" ? new SerializedObject(flow.climb).FindProperty("height").floatValue : 5;
            foreach (var target in targets)
            {
                var old = target.Find("R2Prop"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                foreach (var r in target.GetComponentsInChildren<Renderer>(true)) if (r.name != "R1Marker") r.enabled = false;
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                visual.name = "R2Prop"; visual.transform.SetParent(target, false); Fit(visual.transform, height);
                foreach (var r in visual.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
                // Visual-only imports retain the existing gameplay colliders and interaction roots.
                foreach (var collider in visual.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            }
            if (key == "house_tongwa") for (int i = 4; i < 6; i++) foreach (var r in village.transform.Find("House_" + i).GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            if (key == "village_board") foreach (var r in GameObject.Find("VillageBoard").GetComponentsInChildren<Renderer>()) r.enabled = false;
            if (key == "peach_tree") foreach (var r in GameObject.Find("PeachTree").GetComponentsInChildren<Renderer>()) r.enabled = false;
            report.AppendLine("- PASS " + key + "：" + targets.Count + " 個外觀，height=" + height + "m。");
        }
        File.WriteAllText("logs/U-R2-props.md", report.ToString());
        var seasons = flow.GetComponent<SeasonSwap>() ?? flow.gameObject.AddComponent<SeasonSwap>(); flow.seasons = seasons;
        Set(seasons, "immortalVillageRoot", village); Set(seasons, "homeVillageRoot", home.gameObject); Set(seasons, "skyReturn", sky); Set(seasons, "sun", RenderSettings.sun);
        Renderers(seasons, "canopyRenderers", GameObject.Find("Gorge").GetComponentsInChildren<Renderer>().Where(r => r.name.Contains("Canopy") || r.GetComponentsInParent<Transform>().Any(t => t.name.StartsWith("R2Peach_"))).Concat(GameObject.Find("ActPoints/Act2_PeachTree").GetComponentsInChildren<Renderer>()).ToArray());
        Renderers(seasons, "groundRenderers", village.GetComponentsInChildren<Renderer>().Where(r => r.name == "VillageSquare").ToArray());
        var epilogue = flow.GetComponent<EpilogueDirector>() ?? flow.gameObject.AddComponent<EpilogueDirector>(); flow.epilogue = epilogue;
        Set(epilogue, "cluesJson", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/clues.json"));
        Set(epilogue, "compareJson", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/compare.json"));
        home.gameObject.SetActive(false); village.SetActive(true);
    }
    static Transform Node(string name, Vector3 position, Transform parent)
    { var t = parent.Find(name); if (!t) { t = new GameObject(name).transform; t.SetParent(parent, false); } t.position = position; return t; }
}
