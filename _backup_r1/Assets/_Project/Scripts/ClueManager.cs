using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Liuruan
{
    /// clues.json 的閱讀、問答與進度資料。
    /// JsonUtility 缺欄位時給預設值（字串 null、陣列 null），一律用下面的唯讀屬性判斷。
    [Serializable]
    public class ClueData
    {
        public int id;
        public string title;
        public string original;
        public string plain;
        public string question;
        public string[] options;
        public int answer;
        public string feedback;
        public string[] wrongHints;

        public bool isEnding;     // 選填：結局卷軸（沒有任何一筆標記時，退回「最大 id＝結局」）
        public int[] requires;    // 選填：這些線索收齊前鎖住（標記灰色、靠近時提示「先去看看〇〇」）

        /// 題目及答案索引完整時才出題。
        public bool HasQuiz => !string.IsNullOrEmpty(question) && options != null && options.Length > 0
                               && answer >= 0 && answer < options.Length;

    }

    /// 頂層 "evidenceCards" 的一張：結局推論題前顯示（只顯示已收集線索的卡）
    [Serializable]
    public class EvidenceCard
    {
        public int clueId;
        public string label;
        public string quote;
    }

    /// 頂層 "reflection"：不計分的反思題
    [Serializable]
    public class Reflection
    {
        public string question;
        public string[] prompts;
    }

    [Serializable]
    class ClueList
    {
        public ClueData[] clues;
        public ClueData infer;      // 選填：結局後的推論題；沒填就不出（結局線索本身可當推論題）
        public string endingText;   // 選填：作者／人物揭曉文字；沒填用 Txt.EndingReveal
        public EvidenceCard[] evidenceCards; // 選填
        public Reflection reflection;        // 選填
    }

    /// 線索資料（clues.json）＋進度（PlayerPrefs）。
    /// 有 requires 的線索要等列出的線索都收齊才開。
    public class ClueManager : MonoBehaviour
    {
        const string SaveKey = "liuruan_collected";

        public static ClueManager I { get; private set; }
        /// 結局線索 id（資料中最大 id）；QuizStats 以它推算推論題 id
        public static int EndingId { get; private set; }
        public static bool IsEnding(int id) => id == EndingId;

        [SerializeField] TextAsset clueJson;
        [SerializeField] ScrollUI ui;
        [SerializeField] Material markerMat; // 線索標記用（URP Unlit）
        public Material MarkerMat => markerMat; // R17 任務關卡標記共用

        ClueData[] clues = new ClueData[0];
        ClueData inferData;
        string endingText;
        EvidenceCard[] evidenceCards = new EvidenceCard[0];
        Reflection reflection;
        readonly HashSet<int> collected = new HashSet<int>();
        // id → 線索。ClueMarker 每幀呼叫 IsLocked → Find；舊版 Array.Find 帶捕獲 lambda，每次呼叫都配置記憶體（審查 #17）
        readonly Dictionary<int, ClueData> byId = new Dictionary<int, ClueData>();

        public IReadOnlyList<ClueData> Clues => clues;
        public bool IsCollected(int id) => collected.Contains(id);
        public bool FirstScrollPending => clues.Length > 0 && !collected.Contains(clues[0].id);
        public ClueData Find(int id) => byId.TryGetValue(id, out var c) ? c : null;
        public ClueData InferData => inferData;
        public string EndingText => string.IsNullOrEmpty(endingText) ? Txt.EndingReveal : endingText;
        /// 反思題（沒填 question 時為 null）
        public Reflection ReflectionData => reflection;

        /// 已收集線索的證據卡（依 clueId 排序）
        public List<EvidenceCard> CollectedEvidence()
        {
            var list = new List<EvidenceCard>();
            foreach (var e in evidenceCards)
                if (e != null && collected.Contains(e.clueId) && (!string.IsNullOrEmpty(e.label) || !string.IsNullOrEmpty(e.quote)))
                    list.Add(e);
            list.Sort((a, b) => a.clueId.CompareTo(b.clueId));
            return list;
        }

        // 以下計數每幀被 HUD／ClueMarker 呼叫：用迴圈，不用捕獲 this 的 lambda 或方法群組（每次都會配置委派）
        /// 結局以外的線索數（HUD「線索冊 x／N」）
        public int ScrollCount { get { int n = 0; foreach (var c in clues) if (c.id != EndingId) n++; return n; } }
        public int ScrollsCollected { get { int n = 0; foreach (var c in clues) if (c.id != EndingId && collected.Contains(c.id)) n++; return n; } }
        public bool EndingUnlocked => ScrollsCollected == ScrollCount;


        /// ClueMarker 每幀呼叫：只回布林、不產生字串
        public bool IsLocked(int id)
        {
            if (id == EndingId) return !EndingUnlocked;
            var c = Find(id);
            return c != null && (MissingRequirement(c) != null);
        }

        /// requires 裡第一張還沒收集的線索；都收齊回傳 null。
        /// 會造成永久鎖死的條件一律略過（check.py 也會擋，這裡是 C# 端防呆，審查 #16）：
        public ClueData MissingRequirement(ClueData c)
        {
            if (c == null || c.requires == null) return null;
            foreach (int r in c.requires)
            {
                if (r == c.id || r == EndingId || collected.Contains(r)) continue;
                var rc = Find(r);
                if (rc == null) continue;
                return rc;
            }
            return null;
        }

        /// 鎖住時的提示；沒鎖回傳 null
        string LockedMessage(int id)
        {
            if (id == EndingId) return EndingUnlocked ? null : Txt.EndingLocked(ScrollsCollected, ScrollCount);
            var c = Find(id);
            if (c == null) return null;
            var miss = MissingRequirement(c);
            return miss != null ? Txt.RequiresLocked(miss.title) : null;
        }

        /// 算進成績的題目：一般選擇題、配對關（以配對後選擇題首答計）、
        /// 有選擇題的小遊戲（小遊戲後由 ScrollUI 出題）、推論題（有 infer 區塊時）。
        public bool IsScored(int id)
        {
            if (id == QuizStats.InferId) return inferData != null;
            var c = Find(id);
            return c != null && c.HasQuiz;
        }

        public string LabelOf(ClueData c)
        {
            if (c.id == EndingId) return Txt.EndingTag;
            int n = 0;
            foreach (var x in clues) if (x.id != EndingId && x.id < c.id) n++;
            return Txt.RollPrefix + (n < Txt.Numerals.Length ? Txt.Numerals[n].ToString() : (n + 1).ToString());
        }


        void Awake()
        {
            I = this;
            if (clueJson == null) Debug.LogError("[ClueManager] 沒有指定 clues.json");
            else
            {
                var list = JsonUtility.FromJson<ClueList>(clueJson.text);
                if (list != null)
                {
                    clues = list.clues ?? new ClueData[0];
                    inferData = list.infer != null && !string.IsNullOrEmpty(list.infer.question) ? list.infer : null;
                    endingText = list.endingText;
                    if (list.evidenceCards != null) evidenceCards = list.evidenceCards;
                    reflection = list.reflection != null && !string.IsNullOrEmpty(list.reflection.question) ? list.reflection : null;
                }
            }
            Array.Sort(clues, (a, b) => a.id.CompareTo(b.id));
            byId.Clear();
            foreach (var c in clues) if (c != null && !byId.ContainsKey(c.id)) byId[c.id] = c;
            var flagged = Array.Find(clues, c => c.isEnding);
            EndingId = flagged != null ? flagged.id : clues.Length > 0 ? clues[clues.Length - 1].id : 0;
            foreach (var s in PlayerPrefs.GetString(SaveKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(s, out int v)) collected.Add(v);
        }

        void Start()
        {
            foreach (var it in Interactable.All)
                if (it.clueId >= 0) ClueMarker.Create(it, markerMat);
            // 不自動跳題：先讓玩家自由走動，用一句話指出第一步
            if (collected.Count == 0 && !ScrollUI.DeferHelp) Invoke(nameof(StartOpening), 1f);
        }

        void StartOpening() { if (ui) ui.Toast(Txt.Opening); }
        /// R17：選經典模式後才放開場提示
        public void ShowOpening() { if (collected.Count == 0) Invoke(nameof(StartOpening), 0.5f); }

        public void TriggerClue(int id)
        {
            var locked = LockedMessage(id);
            if (locked != null) { ui.Toast(locked); return; }
            var clue = Find(id);
            if (clue == null) { Debug.LogWarning($"[ClueManager] clues.json 沒有 id={id}"); return; }
            if (IsCollected(id))
                ui.ShowClue(clue, () => { }, true); // 已收集：直接重讀，不重播演出
            else if (id == EndingId) ui.StartEnding(clue, () => Collect(id));
            else ui.ShowClue(clue, () => Collect(id));
        }

        void Collect(int id)
        {
            if (!collected.Add(id)) return;
            PlayerPrefs.SetString(SaveKey, string.Join(",", collected));
            PlayerPrefs.Save();
            if (id == EndingId) return;
            string msg = EndingUnlocked ? Txt.EndingReady : null;
            if (clues.Length > 0 && id == clues[0].id) msg = Txt.FirstScrollComplete;
            StartCoroutine(NextStopToast(msg));
        }

        System.Collections.IEnumerator NextStopToast(string msg)
        {
            yield return null;
            for (float t = 0; t < 30f && ScrollUI.I && ScrollUI.I.IsBusy; t += Time.unscaledDeltaTime) yield return null;
            string text = msg;
            if (string.IsNullOrEmpty(text)) yield break;
            ui.Toast(text);
        }

        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            QuizStats.Clear();
            PlayerPrefs.DeleteKey(ScrollUI.HelpSeenKey); // 共用電腦：下一位學生重新開始也看得到操作說明
            PlayerPrefs.Save();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
