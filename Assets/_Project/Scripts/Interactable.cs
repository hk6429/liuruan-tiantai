using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Liuruan
{
    /// 靠近顯示提示，按 E 觸發。clueId >= 0 時開啟該線索；toast 不為空時顯示一句提示。
    public class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public string prompt = Txt.DefaultPrompt;
        public float radius = 2.5f;
        public int clueId = -1;
        [TextArea] public string toast;
        public UnityEvent onInteract = new UnityEvent(); // r12：執行期 AddComponent 時不會自動建立，沒初始值會 NRE

        /// Round14：已收集的線索點顯示「E 再讀」（自訂提示字不動）
        public string CurrentPrompt => clueId >= 0 && prompt == Txt.DefaultPrompt && ClueManager.I && ClueManager.I.IsCollected(clueId) ? Txt.RereadPrompt : prompt;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void Interact()
        {
            if (!string.IsNullOrEmpty(toast)) ScrollUI.I?.Toast(toast);
            if (clueId >= 0) ClueManager.I?.TriggerClue(clueId);
            onInteract.Invoke();
        }

        public static Interactable Nearest(Vector3 pos)
        {
            Interactable best = null;
            float bestDist = float.MaxValue;
            foreach (var it in All)
            {
                Vector3 off = it.transform.position - pos;
                off.y = 0; // 只算水平距離
                float d = off.magnitude;
                if (d <= it.radius && d < bestDist) { best = it; bestDist = d; }
            }
            return best;
        }
    }
}
