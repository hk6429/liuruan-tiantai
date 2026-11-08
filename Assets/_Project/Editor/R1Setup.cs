using System;
using System.IO;
using Liuruan;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class R1Setup
{
    [MenuItem("Tools/Liuruan/R1/Install")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("請先離開 Play 模式。");
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
        var player = GameObject.Find("Player");
        Add<HungerSystem>(player);
        var companion = GameObject.Find("Companion");
        var agent = Add<NavMeshAgent>(companion);
        agent.radius = 0.3f; agent.height = 1.8f; agent.baseOffset = 0.9f;
        var follow = Add<CompanionFollow>(companion);
        Set(follow, "player", player.transform);
        var deck = GameObject.Find("R1PeachLanding");
        if (!deck)
        {
            deck = GameObject.CreatePrimitive(PrimitiveType.Cube); deck.name = "R1PeachLanding";
            deck.transform.position = new Vector3(-17, 17.8f, -24); deck.transform.localScale = new Vector3(8, 0.4f, 8);
            deck.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Greybox/Rock.mat");
        }
        foreach (Transform t in GameObject.Find("ActPoints").transform)
        {
            if (t.name == "Act1_Start") continue;
            var it = Add<Interactable>(t.gameObject);
            it.radius = 3.5f;
            if (t.name.StartsWith("Act1_")) it.prompt = "剝皮";
            else if (t.name == "Act2_ClimbBase") it.prompt = "攀援";
            else if (t.name == "Act2_PeachTree") it.prompt = "摘桃";
            else if (t.name.StartsWith("Act3_Drift_")) it.prompt = "撿起";
            else if (t.name == "Act3_Wade") it.prompt = "越過山，來到大溪邊";
            else if (t.name == "Act4_Board") it.prompt = "閱讀原文";
            else if (t.name == "Act5_Return") it.prompt = "返回村口";
            else it.prompt = "查看";
            if (!t.Find("R1Marker"))
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere); marker.name = "R1Marker";
                UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
                marker.transform.SetParent(t, false); marker.transform.localPosition = Vector3.up * 1.3f; marker.transform.localScale = Vector3.one * 0.55f;
                marker.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Greybox/Player.mat");
            }
        }
        var cold = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Greybox/ReturnCold.mat");
        if (!cold)
        {
            cold = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Greybox/Wall.mat"));
            cold.SetColor("_BaseColor", new Color(0.32f, 0.37f, 0.42f));
            AssetDatabase.CreateAsset(cold, "Assets/_Project/Materials/Greybox/ReturnCold.mat");
        }
        var systems = GameObject.Find("JourneySystems");
        if (!systems) systems = new GameObject("JourneySystems");
        var flow = Add<GameFlow>(systems); BindData(flow); Set(flow, "returnMaterial", cold);
        var hud = Add<HudUI>(systems);
        hud.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/NotoSerifTC Static SDF.asset");
        var surface = Add<NavMeshSurface>(GameObject.Find("Terrain_240x240"));
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        var mesh = NavMesh.CalculateTriangulation();
        if (mesh.vertices.Length == 0) throw new InvalidOperationException("NavMesh 烘焙結果為空。");
        PlayerSettings.runInBackground = true;
        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Main 場景儲存失敗。");
        Debug.Log(Liuruan.EditorTools.FontSubset.RebuildNoDialog());
        AssetDatabase.SaveAssets();
        File.WriteAllText("logs/U-R1-install.md", "# R1 安裝結果\n\nMain 已儲存；互動點 15 個；NavMesh vertices=" + mesh.vertices.Length + "。\n題庫：" + (AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/clues.json") ? "已綁定" : "使用占位鍵") + "\n");
    }
    public static void BindData(GameFlow flow)
    {
        Set(flow, "clueJson", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/clues.json"));
    }
    static T Add<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c ? c : go.AddComponent<T>(); }
    static void Set(UnityEngine.Object target, string key, UnityEngine.Object value)
    {
        var so = new SerializedObject(target); so.FindProperty(key).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
}
