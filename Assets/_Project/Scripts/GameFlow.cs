using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Liuruan
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameFlow : MonoBehaviour
    {
        const string SaveKey = "liuruan_r1_progress";
        [SerializeField] TextAsset clueJson;
        [SerializeField] Material returnMaterial;
        public Material returnSky;
        public SeasonSwap seasons;
        public EpilogueDirector epilogue;
        public bool SeasonPlaying => seasons && seasons.IsPlaying;
        public MinimapCam minimap;
        public ClimbZone climb;
        public DriftCollection driftCollection;
        [Serializable] public sealed class Progress
        {
            public int act = 1, bark, drift, villageStep;
            public bool climbed, inferred, ended, halfYearComplete;
            public float playSeconds;
            public Vector3 position = new Vector3(0, 0.1f, -105);
        }
        public Progress State { get; private set; }
        public int Act { get { return State.act; } }
        public int BarkCount { get { int n = 0; for (int i = 0; i < 5; i++) if ((State.bark & (1 << i)) != 0) n++; return n; } }
        public bool Reading { get; private set; }
        public bool CanReturn { get { return Act == 4 && State.villageStep == 3 && !Reading && State.halfYearComplete && !SeasonPlaying; } }
        public HungerSystem Hunger { get; private set; }
        public CompanionFollow Companion { get; private set; }
        public PlayerController Player { get; private set; }
        public ClueData CurrentCard { get; private set; }
        readonly Dictionary<string, Interactable> points = new Dictionary<string, Interactable>();
        ClueData[] clues = new ClueData[0];
        public string Objective
        {
            get
            {
                if (Act == 1) return "一・迷山　剝取穀皮 " + BarkCount + "／5";
                if (Act == 2) return State.climbed ? "二・攀桃　摘取桃子" : "二・攀桃　前往崖下攀援處";
                if (Act == 3) return State.inferred ? "三・流杯　向北涉水" : "三・流杯　拾取三件漂流物";
                if (Act == 4) return new[] { "四・仙村　前往二女子處", "四・仙村　前往銅瓦屋", "四・仙村　閱讀告示板", "四・仙村　選擇求歸" }[State.villageStep];
                return State.ended ? "五・歸鄉　旅程結束" : "五・歸鄉　回到村口";
            }
        }
        void Awake()
        {
            ScrollUI.DeferHelp = true;
            State = new Progress();
            try { if (PlayerPrefs.HasKey(SaveKey)) State = JsonUtility.FromJson<Progress>(PlayerPrefs.GetString(SaveKey)) ?? new Progress(); }
            catch (ArgumentException) { State = new Progress(); }
            State.act = Mathf.Clamp(State.act, 1, 5);
            if (float.IsNaN(State.playSeconds) || float.IsInfinity(State.playSeconds) || State.playSeconds < 0) State.playSeconds = 0;
            State.bark &= 31; State.drift &= 7; State.villageStep = Mathf.Clamp(State.villageStep, 0, 3);
            Player = FindFirstObjectByType<PlayerController>();
            Hunger = Player.GetComponent<HungerSystem>();
            Companion = FindFirstObjectByType<CompanionFollow>();
            if (clueJson)
            {
                try { var list = JsonUtility.FromJson<ClueList>(clueJson.text); if (list != null) clues = list.clues ?? clues; }
                catch (ArgumentException e) { Debug.LogWarning("[GameFlow] 題庫格式無法讀取：" + e.Message); }
            }
            foreach (Transform t in GameObject.Find("ActPoints").transform)
            {
                var it = t.GetComponent<Interactable>();
                if (!it) continue;
                string key = t.name;
                points.Add(key, it);
                it.onInteract.AddListener(() => InteractPoint(key));
            }
        }
        void OnEnable()
        {
            if (seasons) seasons.OnBirdsSing += BirdsSing;
            if (climb) climb.OnTopOut += Climbed;
            if (driftCollection) { driftCollection.OnCollected += Collected; driftCollection.OnAllCollected += Infer; }
        }
        void OnDisable()
        {
            if (seasons) seasons.OnBirdsSing -= BirdsSing;
            if (climb) climb.OnTopOut -= Climbed;
            if (driftCollection) { driftCollection.OnCollected -= Collected; driftCollection.OnAllCollected -= Infer; }
        }
        void BirdsSing()
        {
            State.halfYearComplete = true;
            Debug.Log("[SeasonSwap] 百鳥啼鳴（音效待補）");
            GetComponent<HudUI>().ShowBirds(); Save(); SyncPoints();
        }
        void BeginHalfYear()
        {
            if (State.halfYearComplete || !seasons) return;
            Toast("遂停半年"); seasons.PlayHalfYear();
        }
        public EpilogueModel EndingModel()
        {
            var results = new List<ReadingResult>();
            foreach (var clue in clues) if (!clue.isEnding) results.Add(new ReadingResult(clue.id, QuizStats.FirstTryCorrect(clue.id)));
            int drift = 0; for (int i = 0; i < 3; i++) if ((State.drift & (1 << i)) != 0) drift++;
            return epilogue && epilogue.Present(BarkCount, drift, results.ToArray(), State.playSeconds) ? epilogue.Model : null;
        }
        void Climbed() { State.climbed = true; Save(); SyncPoints(); }
        bool restoringDrift;
        void Collected(int id) { if (restoringDrift) return; State.drift |= 1 << (id - 1); Save(); SyncPoints(); }
        void Infer() { if (!restoringDrift && !Reading && Act == 3 && !State.inferred) ReadAct(3, () => { State.inferred = true; Save(); SyncPoints(); }); }
        void Start()
        {
            restoringDrift = true;
            if (driftCollection) for (int id = 1; id <= 3; id++)
                if ((State.drift & (1 << (id - 1))) != 0) driftCollection.Core.Collect(id);
            restoringDrift = false;
            MovePlayer(State.position);
            if (Act >= 3) Hunger.FillAll();
            Hunger.OnToast += Toast;
            SyncPoints();
            if (Act == 5) ApplyReturn();
            if (Act == 4 && State.villageStep == 3) BeginHalfYear();
            if (Act == 3 && State.drift == 7 && !State.inferred) Infer();
            if (State.ended) GetComponent<HudUI>().ShowEnding();
        }
        void OnDestroy() { if (Hunger) Hunger.OnToast -= Toast; }
        void Update()
        {
            if (climb && climb.IsLocked && TouchInput.Enabled) climb.SetMoveInput(TouchInput.Move);
            bool busy = Reading || (ScrollUI.I && ScrollUI.I.IsBusy) || State.ended || SeasonPlaying;
            if (!busy) State.playSeconds += Time.deltaTime;
            if (busy) Hunger.Pause(); else Hunger.Resume();
            if (!busy)
            {
                if (Input.GetKeyDown(KeyCode.F)) UseSkill(0);
                if (Input.GetKeyDown(KeyCode.R)) UseSkill(1);
                if (Input.GetKeyDown(KeyCode.G)) UseSkill(2);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.F9) && !busy) DebugNextAct();
#endif
        }
        void LateUpdate()
        {
            if (!climb || !climb.IsLocked || !Player.cam) return;
            Player.cam.position = Player.transform.position + new Vector3(6, 2.5f, -4);
            Player.cam.LookAt(Player.transform.position + Vector3.up);
        }
        public void UseSkill(int skill)
        {
            if (Reading || SeasonPlaying || State.ended || (ScrollUI.I && ScrollUI.I.IsBusy)) return;
            if (skill == 2) { if (Companion.Call()) Toast("已呼喚阮肇"); return; }
            var near = Interactable.Nearest(Player.transform.position);
            if (near && (skill == 0 ? near.name.StartsWith("Act1_Bark_") : near.name.StartsWith("Act3_Drift_"))) near.Interact();
            else Toast(skill == 0 ? "請靠近可剝皮的楮樹" : "請靠近溪中漂流物");
        }
        public void InteractPoint(string key)
        {
            Interactable point;
            if (Reading || SeasonPlaying || State.ended || !points.TryGetValue(key, out point) || !point.enabled) return;
            if (Act == 1 && key.StartsWith("Act1_Bark_"))
            {
                State.bark |= 1 << (key[key.Length - 1] - '1');
                if (State.bark == 31) { State.act = 2; ReadAct(1, null); }
            }
            else if (Act == 2 && key == "Act2_ClimbBase")
            {
                if (climb) climb.TryInteract();
            }
            else if (Act == 2 && key == "Act2_PeachTree")
            {
                if (Player.animator) Player.animator.SetTrigger("PickFruit");
                Hunger.FillAll();
                Reading = true;
                StartCoroutine(FinishPicking());
            }
            else if (Act == 3 && key.StartsWith("Act3_Drift_"))
            {
                var pickup = point.GetComponent<DriftPickup>();
                if (pickup) pickup.TryInteract();
            }
            else if (Act == 3 && key == "Act3_Wade") { State.act = 4; MovePlayer(new Vector3(0, 0.2f, 63)); }
            else if (Act == 4)
            {
                State.villageStep = Mathf.Min(3, State.villageStep + 1);
                if (key == "Act4_Board") ReadAct(4, BeginHalfYear);
            }
            else if (Act == 5 && key == "Act5_Return")
            {
                ApplyReturn();
                ReadAct(5, () => { State.ended = true; Save(); SyncPoints(); GetComponent<HudUI>().ShowEnding(); });
            }
            Save(); SyncPoints();
        }
        IEnumerator FinishPicking()
        {
            float duration = 1;
            if (Player.animator) foreach (var clip in Player.animator.runtimeAnimatorController.animationClips)
                if (clip.name == "pickfruit") duration = clip.length + .15f;
            yield return new WaitForSeconds(duration);
            State.act = 3; MovePlayer(new Vector3(-7, .2f, -16)); ReadAct(2, null); Save(); SyncPoints();
        }
        public void ReturnHome()
        {
            if (!CanReturn || (ScrollUI.I && ScrollUI.I.IsBusy)) return;
            State.act = 5; ApplyReturn(); MovePlayer(new Vector3(0, 0.2f, -104)); Save(); SyncPoints();
        }
        void ReadAct(int act, Action done)
        {
            var cards = new List<ClueData>();
            foreach (var c in clues) if (c != null && !c.isEnding && c.act == act) cards.Add(c);
            if (cards.Count == 0) cards.Add(new ClueData { id = -act, act = act, title = "〔act" + act + "-原文〕", original = "〔act" + act + "-原文〕" });
            Reading = true; Hunger.Pause();
            ReadCard(cards, 0, done);
        }
        void ReadCard(List<ClueData> cards, int i, Action done)
        {
            if (i == cards.Count) { Reading = false; CurrentCard = null; if (done != null) done(); return; }
            CurrentCard = cards[i];
            ScrollUI.I.ShowClue(CurrentCard, () => ReadCard(cards, i + 1, done));
        }
        void SyncPoints()
        {
            if (minimap) minimap.SetAct(Act);
            Transform target = null;
            foreach (var pair in points)
            {
                string k = pair.Key;
                bool enabled = false;
                if (!State.ended)
                {
                    if (Act == 1 && k.StartsWith("Act1_Bark_")) enabled = (State.bark & (1 << (k[k.Length - 1] - '1'))) == 0;
                    if (Act == 2) enabled = k == (State.climbed ? "Act2_PeachTree" : "Act2_ClimbBase");
                    if (Act == 3 && k.StartsWith("Act3_Drift_")) enabled = !State.inferred && (State.drift & (1 << (k[k.Length - 1] - '1'))) == 0;
                    if (Act == 3 && k == "Act3_Wade") enabled = State.inferred;
                    if (Act == 4 && State.villageStep < 3) enabled = k == new[] { "Act4_Women", "Act4_House", "Act4_Board" }[State.villageStep];
                    if (Act == 4 && State.villageStep == 3) enabled = k == "Act4_Board";
                    if (Act == 5) enabled = k == "Act5_Return";
                }
                var prop = pair.Value.transform.Find("R2Prop");
                if (prop && k.StartsWith("Act4_")) prop.gameObject.SetActive(Act < 5);
                pair.Value.enabled = enabled;
                if (enabled && !target) target = pair.Value.transform;
                var visual = pair.Value.transform.Find("R1Marker");
                if (visual) visual.gameObject.SetActive(enabled);
            }
            if (minimap) minimap.SetTarget(target);
        }
        public void MovePlayer(Vector3 position)
        {
            var cc = Player.GetComponent<CharacterController>();
            cc.enabled = false; Player.transform.position = position; cc.enabled = true;
        }
        void Save() { State.position = Player.transform.position; PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(State)); PlayerPrefs.Save(); }
        void ApplyReturn()
        {
            if (seasons) seasons.SwapToHome();
        }
        void Toast(string text) { if (ScrollUI.I) ScrollUI.I.Toast(text); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DebugNextAct()
        {
            State.act = Mathf.Min(5, Act + 1);
            if (Act == 2) MovePlayer(new Vector3(-11, 0.2f, -31));
            if (Act == 3) MovePlayer(new Vector3(-5, 0.2f, -13));
            if (Act == 4) MovePlayer(new Vector3(0, 0.2f, 63));
            if (Act == 5) { ApplyReturn(); MovePlayer(new Vector3(0, 0.2f, -104)); }
            Save(); SyncPoints();
        }
#endif
    }
}
