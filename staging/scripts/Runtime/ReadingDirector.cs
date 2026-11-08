using System;
using UnityEngine;

namespace Liuruan
{
    public sealed class ReadingDirector : MonoBehaviour
    {
        [SerializeField] TextAsset sentencesJson, glossaryJson;
        public ReadingCore Core { get; private set; }
        public event Action<string> OnError;
        public event Action<ReadingMode> OnModeChanged;
        public ReadingParagraph GetParagraph(int p) => Core?.GetParagraph(p);
        public GlossaryCard FindTerm(string term) => Core?.FindTerm(term);
        public void SetMode(ReadingMode mode) { Core?.SetMode(mode); }
        public string Display(ReadingSentence sentence) => Core?.Display(sentence);

        void Awake() { Load(); }
        public bool Load()
        {
            try { Core = new ReadingCore(R3Json.Read<SentenceDocument>(sentencesJson), R3Json.Read<GlossaryDocument>(glossaryJson)); }
            catch (ArgumentException e)
            {
                Core = null;
                Debug.LogError("ReadingDirector 資料無法讀取：" + e.Message, this);
                OnError?.Invoke(e.Message); return false;
            }
            Core.OnModeChanged += mode => OnModeChanged?.Invoke(mode);
            if (Core.MissingTerms.Count > 0)
                Debug.LogWarning("缺少詞卡：" + string.Join("、", Core.MissingTerms), this);
            return true;
        }
    }
}
