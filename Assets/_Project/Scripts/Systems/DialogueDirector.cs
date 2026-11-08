using System;
using UnityEngine;

namespace Liuruan
{
    public sealed class DialogueDirector : MonoBehaviour
    {
        [SerializeField] TextAsset dialogueJson;
        public DialogueCore Core { get; private set; }
        public event Action<string> OnError;
        public bool IsActive => Core != null && Core.IsActive;
        public DialogueLine CurrentLine => Core?.CurrentLine;
        public event Action<DialogueLine> OnLineChanged;
        public event Action<string> OnSceneEnd;
        public bool Play(string sceneId) => Core != null && Core.Start(sceneId);
        public bool Replay(string sceneId) => Core != null && Core.Start(sceneId, true);
        public bool TryTrigger(string trigger) => Core != null && Core.TryTrigger(trigger);
        public bool Next() => Core != null && Core.Next();

        void Awake() { Load(); }
        public bool Load()
        {
            try { Core = new DialogueCore(R3Json.Read<DialogueDocument>(dialogueJson)); }
            catch (ArgumentException e)
            {
                Core = null;
                Debug.LogError("DialogueDirector 資料無法讀取：" + e.Message, this);
                OnError?.Invoke(e.Message); return false;
            }
            Core.OnLineChanged += line => OnLineChanged?.Invoke(line);
            Core.OnSceneEnd += id => OnSceneEnd?.Invoke(id);
            return true;
        }
    }
}
