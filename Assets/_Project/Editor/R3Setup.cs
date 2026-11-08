using System;
using System.IO;
using System.Text;
using Liuruan;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class R3Setup
{
    public static void Install()
    {
        // 保留 R3a 已安裝美術與碰撞修正；R3b 再接入本輪交付。
        R2Setup.UseInstalledArt = true;
        try { R2Setup.Install(); }
        finally { R2Setup.UseInstalledArt = false; }
        var houseCollider = GameObject.Find("Village/House_0/Walls").GetComponent<Collider>();
        houseCollider.enabled = true; Physics.SyncTransforms();
        var house = houseCollider.bounds;
        var point = GameObject.Find("ActPoints/Act4_House").transform;
        // 灰盒無門洞；朝廣場的東牆作為門前，腳底離牆面至少一公尺。
        point.position = new Vector3(house.max.x + 1, .2f, house.center.z);
        point.GetComponent<Interactable>().radius = 5;
        Physics.SyncTransforms();
        if (Vector3.Distance(house.ClosestPoint(point.position), point.position) < .999f)
            throw new InvalidOperationException("門前站點離牆不足一公尺。");
        R3bSetup.Integrate();
        var report = new StringBuilder("# R3 互動中心碰撞檢查\n\n|互動點|原座標|修正後座標|\n|---|---|---|\n");
        var colliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
        foreach (Transform t in GameObject.Find("ActPoints").transform)
        {
            if (!t.GetComponent<Interactable>()) continue;
            var before = t.position;
            // 以站立膠囊中心檢查實心障礙，地板與 trigger 不屬於阻擋中心。
            for (int pass = 0; pass < 8; pass++)
            {
                bool moved = false;
                foreach (var c in colliders)
                {
                    if (!c.enabled || c.isTrigger || c is TerrainCollider || c is CharacterController || c.GetComponentInParent<CompanionFollow>()) continue;
                    Vector3 probe = t.position + Vector3.up * .9f;
                    if ((c.ClosestPoint(probe) - probe).sqrMagnitude > .000001f) continue;
                    var b = c.bounds;
                    Vector3[] exits = { new Vector3(b.min.x - 1, t.position.y, t.position.z), new Vector3(b.max.x + 1, t.position.y, t.position.z), new Vector3(t.position.x, t.position.y, b.min.z - 1), new Vector3(t.position.x, t.position.y, b.max.z + 1) };
                    var best = exits[0]; foreach (var exit in exits) if ((exit - t.position).sqrMagnitude < (best - t.position).sqrMagnitude) best = exit;
                    t.position = best; Physics.SyncTransforms(); moved = true;
                }
                if (!moved) break;
                if (pass == 7) throw new InvalidOperationException(t.name + " 無法移出碰撞體。");
            }
            report.AppendLine("|" + t.name + "|" + before.ToString("F2") + "|" + t.position.ToString("F2") + "|");
        }
        var terrain = Terrain.activeTerrain;
        if (!terrain) throw new InvalidOperationException("缺少地形，無法建立邊界。");
        var min = terrain.transform.position + new Vector3(3, 0, 3);
        var max = terrain.transform.position + terrain.terrainData.size - new Vector3(3, 0, 3);
        var walls = GameObject.Find("R3Boundaries") ?? new GameObject("R3Boundaries");
        for (int i = 0; i < 4; i++)
        {
            var child = walls.transform.Find("Boundary" + i);
            if (!child) { child = new GameObject("Boundary" + i, typeof(BoxCollider)).transform; child.SetParent(walls.transform); }
            bool x = i < 2;
            child.position = new Vector3(x ? (i == 0 ? min.x - .5f : max.x + .5f) : (min.x + max.x) / 2, 30,
                x ? (min.z + max.z) / 2 : (i == 2 ? min.z - .5f : max.z + .5f));
            child.GetComponent<BoxCollider>().size = new Vector3(x ? 1 : max.x - min.x + 2, 80, x ? max.z - min.z + 2 : 1);
        }
        Debug.Log(Liuruan.EditorTools.FontSubset.RebuildNoDialog());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        if (!EditorSceneManager.SaveOpenScenes()) throw new IOException("R3 Main 儲存失敗。");
        AssetDatabase.SaveAssets();
        File.WriteAllText("logs/U-R3-install.md", report.ToString());
    }
}
