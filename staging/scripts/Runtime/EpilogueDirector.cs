using System;
using UnityEngine;

namespace Liuruan
{
    public sealed class EpilogueDirector : MonoBehaviour
    {
        [SerializeField] private TextAsset cluesJson, compareJson;
        public EpilogueModel Model { get; private set; }
        public event Action<EpilogueModel> OnReady;
        public event Action<string> OnError;

        public bool Present(int barkCount, int driftCount, ReadingResult[] readings, float totalSeconds)
        {
            EpilogueModel model;
            try
            {
                if (!cluesJson || !compareJson) throw new ArgumentException("EpilogueDirector 請指定 clues.json 與 compare.json。");
                model = EpilogueCore.Build(JsonUtility.FromJson<EpilogueClueDocument>(cluesJson.text),
                    JsonUtility.FromJson<EpilogueCompareDocument>(compareJson.text), barkCount, driftCount, readings, totalSeconds);
            }
            catch (ArgumentException e)
            {
                Model = null;
                Debug.LogError("結局資料無法讀取：" + e.Message, this);
                OnError?.Invoke(e.Message); return false;
            }
            Model = model; OnReady?.Invoke(Model); return true;
        }
    }
}
