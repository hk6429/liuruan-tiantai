using UnityEngine;

namespace Liuruan
{
    /// 找東西的提示：上下飄、面向鏡頭、光一閃一閃
    public class FindGlow : MonoBehaviour
    {
        public float Brightness = 1;
        public bool Billboard = true;
        MaterialPropertyBlock block;
        Renderer visual;
        Vector3 basePos; Light lt;
        void Start() { basePos = transform.localPosition; lt = GetComponent<Light>(); visual = GetComponent<Renderer>(); block = new MaterialPropertyBlock(); }
        void LateUpdate()
        {
            float t = Time.time;
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(t * 3f) * 0.08f;
            if (Billboard && Camera.main) transform.rotation = Camera.main.transform.rotation;
            if (lt) lt.intensity = Brightness * (1.5f + Mathf.Sin(t * 4f));
            if (!Billboard && visual) { block.SetColor("_BaseColor", new Color(1,.77f,.17f) * Brightness); visual.SetPropertyBlock(block); }
        }
    }
}
