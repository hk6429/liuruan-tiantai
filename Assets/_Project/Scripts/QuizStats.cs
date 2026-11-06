using UnityEngine;

namespace Liuruan
{
    /// 作答紀錄（PlayerPrefs）：每題答對前的作答次數、是否已答對、答對前是否看過白話。
    /// 題號 = 線索 id；結局推論題 = 固定 InferId（100，不與線索 id 撞號）。首答正確 = 已答對且只答了 1 次。
    /// 小遊戲不計分，見 ClueManager.IsScored。
    public static class QuizStats
    {
        /// 推論題固定題號；線索 id 必須小於它（check.py 另驗）
        public const int InferId = 100;
        /// 線索題號上限（不含推論題），依 ClueManager 實際最大 id
        static int MaxClueId => ClueManager.I != null && ClueManager.I.Clues.Count > 0 ? ClueManager.I.Clues[ClueManager.I.Clues.Count - 1].id : ClueManager.EndingId;
        static System.Collections.Generic.IEnumerable<int> Ids()
        {
            for (int i = 0; i <= MaxClueId; i++) yield return i;
            yield return InferId;
        }
        const int ClearMax = 64; // 重新開始時一律清到這裡，不依賴 ClueManager 是否在場

        static string TriesKey(int id) => $"liuruan_q{id}_tries";
        static string SolvedKey(int id) => $"liuruan_q{id}_solved";
        static string PlainKey(int id) => $"liuruan_q{id}_plain";

        public static bool Solved(int id) => PlayerPrefs.GetInt(SolvedKey(id), 0) == 1;
        public static int Tries(int id) => PlayerPrefs.GetInt(TriesKey(id), 0);
        public static bool SawPlain(int id) => PlayerPrefs.GetInt(PlainKey(id), 0) == 1;
        public static bool FirstTryCorrect(int id) => Solved(id) && Tries(id) == 1;

        static bool Scored(int id) => ClueManager.I == null || ClueManager.I.IsScored(id);

        /// 只記「第一次答對之前」的作答，重看已答對的卷軸不改成績。
        public static void Record(int id, bool correct)
        {
            if (Solved(id)) return;
            PlayerPrefs.SetInt(TriesKey(id), Tries(id) + 1);
            if (correct) PlayerPrefs.SetInt(SolvedKey(id), 1);
            PlayerPrefs.Save();
        }

        public static void MarkPlain(int id)
        {
            if (Solved(id) || SawPlain(id)) return;
            PlayerPrefs.SetInt(PlainKey(id), 1);
            PlayerPrefs.Save();
        }

        public static int ScoredCount()
        {
            int n = 0;
            foreach (int i in Ids()) if (Scored(i)) n++;
            return n;
        }

        public static int FirstTryCount()
        {
            int n = 0;
            foreach (int i in Ids()) if (Scored(i) && FirstTryCorrect(i)) n++;
            return n;
        }

        /// 有白話的計分題中，沒看白話就答對的題數
        public static int NoPlainCount()
        {
            int n = 0;
            for (int i = 0; i <= MaxClueId; i++) if (Scored(i) && Solved(i) && !SawPlain(i)) n++;
            return n;
        }

        /// 有白話的計分題數（NoPlainCount 的分母；推論題沒有白話，不算）
        public static int PlainScoredCount()
        {
            int n = 0;
            for (int i = 0; i <= MaxClueId; i++) if (Scored(i)) n++;
            return n;
        }

        public static int TotalTries()
        {
            int n = 0;
            foreach (int i in Ids()) if (Scored(i)) n += Tries(i);
            return n;
        }

        public static void Clear()
        {
            for (int i = 0; i <= Mathf.Max(InferId, ClearMax); i++)
            {
                PlayerPrefs.DeleteKey(TriesKey(i));
                PlayerPrefs.DeleteKey(SolvedKey(i));
                PlayerPrefs.DeleteKey(PlainKey(i));
            }
            PlayerPrefs.Save();
        }
    }
}
