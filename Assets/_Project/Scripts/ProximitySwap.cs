using UnityEngine;

namespace Liuruan
{
    /// 模型外殼與灰盒互換：玩家或鏡頭進到 zone（世界座標盒）裡，
    /// 就藏起 Tripo 模型、亮回灰盒（灰盒有門洞／室內），出來再換回。碰撞體一律是灰盒的，不受影響。
    public class ProximitySwap : MonoBehaviour
    {
        public Transform tripo;          // __Tripo（模型）
        public Renderer[] greybox;       // 進去時要亮的灰盒 Renderer
        public Bounds zone;              // 世界座標
        bool inside;
        Renderer[] model;
        Transform player;

        void Start()
        {
            model = tripo ? tripo.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            Apply(false);
        }

        void LateUpdate()
        {
            if (!player) { var p = FindFirstObjectByType<PlayerController>(); if (p) player = p.transform; }
            var cam = Camera.main;
            bool now = (player && zone.Contains(player.position + Vector3.up * 0.5f)) || (cam && zone.Contains(cam.transform.position));
            if (now != inside) Apply(now);
        }

        void Apply(bool on)
        {
            inside = on;
            foreach (var r in model) if (r) r.enabled = !on;
            if (greybox != null) foreach (var r in greybox) if (r) r.enabled = on;
        }
    }
}
