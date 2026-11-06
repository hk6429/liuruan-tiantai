using UnityEngine;

namespace Liuruan
{
    /// R17-U：鏡頭專用碰撞層。亭子、迴廊、廳堂等建築模型本身沒有碰撞體（走路靠另外擺的隱形牆），
    /// 跟隨鏡頭與演出鏡頭的 SphereCast 打不到它們，於是鏡頭會卡進屋頂、坐凳裡。
    /// 編輯器用 CamOcclusionBaker.Bake() 在場景 _CamOcclusion 底下複製一份 MeshCollider（放第 8 層），
    /// 執行期讓第 8 層不和任何層發生物理碰撞：只給射線查詢用，不擋人、不擋 CharacterController。
    public static class CamOcclusion
    {
        public const int Layer = 8;
        public const string LayerName = "CamOcclude";
        /// 地面探測、站位檢查要排除鏡頭層（不然會站到迴廊屋頂上）
        public const int WalkMask = ~(1 << Layer);

        /// 樹沒有鏡頭碰撞體時，用鏡頭到角色的線段判斷；不把角色後方的樹算成遮擋。
        public static bool BlocksView(Bounds bounds, Vector3 camera, Vector3 target)
        {
            bounds.Expand(0.3f);
            if (bounds.Contains(camera)) return true;
            Vector3 to = target - camera;
            float length = to.magnitude;
            float distance;
            return length > 0.01f && bounds.IntersectRay(new Ray(camera, to / length), out distance)
                && distance < length - 0.1f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(Layer, i, true);
        }
    }
}
