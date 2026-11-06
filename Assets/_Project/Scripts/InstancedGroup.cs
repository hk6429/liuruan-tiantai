using UnityEngine;

namespace Liuruan
{
    /// Round8：大量重複的戶外擺件（粉牆、花街鋪地、盆菊、秋草…）改走 GPU Instancing。
    /// URP 開著 SRP Batcher 時，相容的 Renderer 一律走 SRP Batcher，同模型同材質也是一個物件一個 draw call；
    /// 給 Renderer 一個 MaterialPropertyBlock 會讓它退出 SRP Batcher，改由材質的 Enable GPU Instancing 合併成一批。
    /// 掛在擺件的根物件上（_R8Outdoor），底下所有 MeshRenderer 都套用；材質要勾 Enable GPU Instancing。
    public class InstancedGroup : MonoBehaviour
    {
        static readonly int Tag = Shader.PropertyToID("_InstancedGroup");

        void Awake()
        {
            var mpb = new MaterialPropertyBlock();
            mpb.SetFloat(Tag, 1f); // 空的 block 不算數，放一個 shader 沒用到的值
            foreach (var r in GetComponentsInChildren<MeshRenderer>(true)) r.SetPropertyBlock(mpb);
        }
    }
}
