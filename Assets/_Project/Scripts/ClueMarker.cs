using UnityEngine;

namespace Liuruan
{
    /// 線索互動點上方的浮空卷軸：上下飄、慢慢轉、亮度呼吸。未收集才顯示；
    public class ClueMarker : MonoBehaviour
    {
        static readonly Color Paper = new Color(0.94f, 0.88f, 0.72f);
        static readonly Color Wood = new Color(0.31f, 0.17f, 0.07f);
        static readonly Color[] PartColors = { Paper, Wood, Wood };
        static readonly Color Grey = new Color(0.45f, 0.45f, 0.45f);
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        int clueId;
        Transform target;
        float height;
        Transform visual;
        Renderer[] rends;
        MaterialPropertyBlock mpb;
        bool shown = true;

        public int ClueId => clueId;

        public static ClueMarker Create(Interactable target, Material mat)
        {
            // 放在物件頂端再往上一點
            float top = target.transform.position.y + 1.2f;
            foreach (var r in target.GetComponentsInChildren<Renderer>())
                top = Mathf.Max(top, r.bounds.max.y + 0.7f);

            var go = new GameObject("ClueMarker_" + target.clueId);
            var m = go.AddComponent<ClueMarker>();
            m.clueId = target.clueId;
            m.target = target.transform; // 不設父物件，免得吃到模型的縮放
            m.height = top - target.transform.position.y;
            m.Build(mat);
            return m;
        }

        void Build(Material mat)
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            // 軸頭比紙卷粗，旋轉到側面仍保留卷軸輪廓。
            rends = new[]
            {
                Part(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.22f, 0.3f, 0.22f), mat),
                Part(PrimitiveType.Cylinder, new Vector3(-0.33f, 0, 0), new Vector3(0.32f, 0.045f, 0.32f), mat),
                Part(PrimitiveType.Cylinder, new Vector3(0.33f, 0, 0), new Vector3(0.32f, 0.045f, 0.32f), mat)
            };
            mpb = new MaterialPropertyBlock();
        }

        Renderer Part(PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            var p = GameObject.CreatePrimitive(type);
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(visual, false);
            p.transform.localPosition = pos;
            p.transform.localRotation = Quaternion.Euler(0, 0, 90);
            p.transform.localScale = scale;
            var r = p.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        void Update()
        {
            var cm = ClueManager.I;
            bool show = cm && !cm.IsCollected(clueId) && !(ScrollUI.I && ScrollUI.I.Cinematic);
            if (show != shown)
            {
                shown = show;
                visual.gameObject.SetActive(show);
            }
            if (!show || !target) return;

            bool locked = cm.IsLocked(clueId);
            float t = Time.time + clueId;
            transform.position = target.position + Vector3.up * (height + (locked ? 0f : Mathf.Sin(t * 2f) * 0.15f));
            visual.localRotation = Quaternion.Euler(0, t * 45f, 0);

            // 離鏡頭越遠放大一點，遠處也看得到
            var cam = Camera.main;
            float s = cam ? Mathf.Clamp(Vector3.Distance(cam.transform.position, transform.position) * 0.06f, 1f, 3f) : 1f;
            visual.localScale = Vector3.one * s;

            float brightness = 0.94f + 0.04f * Mathf.Sin(t * 3f);
            for (int i = 0; i < rends.Length; i++)
            {
                Color c = locked ? Grey : PartColors[i] * brightness;
                c.a = 1f;
                mpb.SetColor(BaseColor, c);
                rends[i].SetPropertyBlock(mpb);
            }
        }
    }
}
