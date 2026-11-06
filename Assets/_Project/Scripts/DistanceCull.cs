using System.Collections.Generic;
using UnityEngine;

namespace Liuruan
{
    /// Round10：戶外大量「靜態」擺件的距離剔除。掛在擺件根物件上（例如 _R10Outdoor），底下所有 MeshRenderer 都管。
    /// - 用 CullingGroup：每個 Renderer 一顆包圍球，距離量到球面，所以大物件（假山、亭子）自然比小盆栽晚消失；
    ///   只在跨越門檻時回呼，不必每幀掃清單。
    /// - 開關 Renderer.enabled，不碰 forceRenderingOff：ZoneVisibility 用 forceRenderingOff（室內／戶外分區），
    ///   最後「有畫」＝ enabled 且 !forceRenderingOff，兩支各管各的旗標，不會互相蓋掉。
    /// - 不能管 ProximitySwap 的物件（它也用 enabled 換外殼／灰盒）：開場自動排除，並記 warning。
    /// - 只適用不會移動的擺件（包圍球只在開場算一次）；NPC、演出道具不要放進來。
    /// - 畫質「低」時距離 × lowQualityFactor。
    public class DistanceCull : MonoBehaviour
    {
        [Tooltip("超過這個距離（公尺，量到包圍球表面）就不畫")]
        public float cullDistance = 40f;
        [Tooltip("畫質「低」時的距離倍率")]
        [Range(0.3f, 1f)] public float lowQualityFactor = 0.7f;
        [Tooltip("包圍球半徑小於這個值（公尺）的小物件，距離再乘 smallFactor（花盆、石子、燈籠先消失）")]
        public float smallRadius = 0.6f;
        [Range(0.3f, 1f)] public float smallFactor = 0.6f;

        readonly List<Renderer> renderers = new List<Renderer>();
        readonly List<Renderer> small = new List<Renderer>();
        CullingGroup group, smallGroup;
        BoundingSphere[] spheres, smallSpheres; // CullingGroup 不複製陣列，必須自己留著參考
        Camera cam;
        bool lastHigh = true;

        void OnEnable()
        {
            Collect();
            cam = Camera.main;
            if (!cam) { Debug.LogWarning("[DistanceCull] Camera.main not found; disabled."); enabled = false; return; }
            group = Make(renderers, out spheres);
            smallGroup = Make(small, out smallSpheres);
            lastHigh = !QualityToggle.I || QualityToggle.I.High;
            ApplyDistances();
            Debug.Log($"[DistanceCull] {name}: {renderers.Count} renderer(s), {small.Count} small.");
        }

        void Collect()
        {
            renderers.Clear(); small.Clear();
            var swapped = new HashSet<Renderer>();
            foreach (var ps in FindObjectsByType<ProximitySwap>(FindObjectsSortMode.None))
            {
                if (ps.tripo) foreach (var r in ps.tripo.GetComponentsInChildren<Renderer>(true)) swapped.Add(r);
                if (ps.greybox != null) foreach (var r in ps.greybox) if (r) swapped.Add(r);
            }
            int skipped = 0;
            foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (swapped.Contains(r)) { skipped++; continue; }
                if (r.bounds.extents.magnitude < smallRadius) small.Add(r); else renderers.Add(r);
            }
            if (skipped > 0) Debug.LogWarning($"[DistanceCull] {name}: skipped {skipped} renderer(s) owned by ProximitySwap.");
        }

        CullingGroup Make(List<Renderer> list, out BoundingSphere[] spheres)
        {
            spheres = new BoundingSphere[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i].bounds;
                spheres[i] = new BoundingSphere(b.center, b.extents.magnitude);
            }
            var g = new CullingGroup { targetCamera = cam };
            g.SetBoundingSpheres(spheres);
            g.SetBoundingSphereCount(spheres.Length);
            g.SetDistanceReferencePoint(cam.transform);
            var target = list;
            g.onStateChanged = ev =>
            {
                // 只看距離帶（0＝門檻內、1＝門檻外）；視錐剔除照舊交給 Unity
                if (ev.currentDistance != ev.previousDistance && ev.index < target.Count && target[ev.index])
                    target[ev.index].enabled = ev.currentDistance == 0;
            };
            return g;
        }

        void ApplyDistances()
        {
            float d = cullDistance * (lastHigh ? 1f : lowQualityFactor);
            group?.SetBoundingDistances(new[] { d });
            smallGroup?.SetBoundingDistances(new[] { d * smallFactor });
        }

        void Update()
        {
            // 換相機（場景重載）就重建；畫質切換就改距離
            if (Camera.main != cam) { OnDisable(); OnEnable(); return; }
            bool high = !QualityToggle.I || QualityToggle.I.High;
            if (high != lastHigh) { lastHigh = high; ApplyDistances(); }
        }

        void OnDisable()
        {
            group?.Dispose(); group = null;
            smallGroup?.Dispose(); smallGroup = null;
            foreach (var r in renderers) if (r) r.enabled = true;
            foreach (var r in small) if (r) r.enabled = true;
        }
    }
}
