using UnityEngine;

namespace Liuruan
{
    // shortcut: 未綁骨角色只移動外觀，S 線正式 CharacterBob 驗收後替換。
    public sealed class R2CharacterMotion : MonoBehaviour
    {
        Vector3 origin, previous;
        float phase;
        void OnEnable() { origin = transform.localPosition; previous = transform.parent.position; }
        void LateUpdate()
        {
            if (GetComponentInChildren<Animator>()) return;
            Vector3 delta = transform.parent.position - previous; delta.y = 0;
            previous = transform.parent.position;
            float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            bool walking = speed > 0.1f && speed < 12f;
            phase += Time.deltaTime * (walking ? 10f : 2f);
            transform.localPosition = origin + Vector3.up * (walking ? Mathf.Abs(Mathf.Sin(phase)) * 0.035f : Mathf.Sin(phase) * 0.006f);
        }
        void OnDisable() { transform.localPosition = origin; }
    }
}
