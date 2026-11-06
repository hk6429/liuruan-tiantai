using UnityEngine;

namespace Liuruan
{
    /// 找東西的提示：上下飄、面向鏡頭、光一閃一閃
    public class FindGlow : MonoBehaviour
    {
        Vector3 basePos; Light lt;
        void Start() { basePos = transform.position; lt = GetComponent<Light>(); }
        void LateUpdate()
        {
            float t = Time.time;
            transform.position = basePos + Vector3.up * Mathf.Sin(t * 3f) * 0.08f;
            if (Camera.main) transform.rotation = Camera.main.transform.rotation;
            if (lt) lt.intensity = 1.5f + Mathf.Sin(t * 4f) * 1f;
        }
    }
}
