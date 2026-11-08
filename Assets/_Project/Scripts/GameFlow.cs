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
        public R3Journey journey;
        public MinimapCam minimap;
        public ClimbZone climb;
        public DriftCollection driftCollection;
        [Serializable] public sealed class Progress
        {
            public int act = 1, bark, drift, villageStep, homeStep, r3Version;
            public bool climbed, inferred, ended, halfYearComplete;
            public float playSeconds;
            public Vector3 position = new Vector3(0, 0.1f, -105);
        }
        public Progress State { get; private set; }
        public int Act { get { return State.act; } }
        public int BarkCount { get { int n = 0; for (int i = 0; i < 5; i++) if ((State.bark & (1 << i)) != 0) n++; return n; } }
        public bool Reading { get; private set; }
        public int RecoveryCount { get; private set; }
        public bool CanReturn { get { return Act == 4 && State.villageStep == 6 && !Reading && State.halfYearComplete && !SeasonPlaying; } }
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
                if (journey && journey.objectives && !string.IsNullOrEmpty(journey.objectives.Text)) return journey.objectives.Text;
                if (Act == 1) return "一・迷山　剝取穀皮 " + BarkCount + "／5";
                if (Act == 2 && State.climbed && Player && Player.transform.position.y < 10) return "二・攀桃　回崖下攀援處重新攀爬";
                if (Act == 2) return State.climbed ? "二・攀桃　摘取桃子" : "二・攀桃　前往崖下攀援處";
                if (Act == 3) return State.inferred ? "三・流杯　越過山，來到大溪邊" : "三・流杯　拾取三件漂流物";
                if (Act == 4) return "四・仙村　與二女交談";
                return State.ended ? "五・歸鄉　旅程結束" : "五・歸鄉　回到村口";
            }
        }
        void Awake()
        {
            ScrollUI.DeferHelp = true; // 等 GameFlow 還原進度後才決定是否顯示操作說明。
            State = new Progress();
            try { if (PlayerPrefs.HasKey(SaveKey)) State = JsonUtility.FromJson<Progress>(PlayerPrefs.GetString(SaveKey)) ?? new Progress(); }
            catch (ArgumentException) { State = new Progress(); }
            State.act = Mathf.Clamp(State.act, 1, 5);
            if (float.IsNaN(State.playSeconds) || float.IsInfinity(State.playSeconds) || State.playSeconds < 0) State.playSeconds = 0;
            State.bark &= 31; State.drift &= 7; State.villageStep = Mathf.Clamp(State.villageStep, 0, 6);
            if (State.r3Version == 0) { State.villageStep = State.halfYearComplete ? 6 : 0; State.r3Version = 1; }
            State.homeStep = Mathf.Clamp(State.homeStep, 0, 2);
            Player = FindFirstObjectByType<PlayerController>();
            Hunger = Player.GetComponent<HungerSystem>();
            Companion = FindFirstObjectByType<CompanionFollow>();
            if (clueJson)
            {
                try { var list = JsonUtility.FromJson<ClueList>(clueJson.text); if (list != null) clues = list.clues ?? clues; }
                catch (ArgumentException e) { Debug.LogWarning("[GameFlow] 題庫格式無法讀取：" + e.Message); }
            }
            if (journey && journey.questionsJson) { var all = new List<ClueData>(clues); all.AddRange(JsonUtility.FromJson<ClueList>(journey.questionsJson.text).clues); clues = all.ToArray(); }
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
            Debug.Log("[SeasonSwap] 百鳥鳴呼（音效待補）");
            GetComponent<HudUI>().ShowBirds();
            journey.Talk("after:half_year", () => { State.villageStep = 6; Save(); SyncPoints(); });
            Save(); SyncPoints();
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
        void Climbed() { State.climbed = true; journey.Progress(); Save(); SyncPoints(); }
        bool restoringDrift;
        void Collected(int id)
        {
            if (restoringDrift) return;
            State.drift |= 1 << (id - 1); journey.Progress(); Save(); SyncPoints();
            journey.Talk("interact:Act3_" + new[] { "Leaf", "Cup", "Rice" }[id - 1], () => { if (State.drift == 7) Infer(); });
        }
        void Infer()
        {
            if (restoringDrift || Reading || journey.dialogue.IsActive || Act != 3 || State.inferred) return;
            journey.Talk("after:drift_all", () => ReadAct(3, () => { State.inferred = true; journey.Progress(); Save(); SyncPoints(); }));
        }
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
            if (State.ended) GetComponent<HudUI>().ShowEnding();
            else journey.BeginAct(() => {
                if (Act == 4 && State.villageStep >= 3 && State.villageStep < 6)
                {
                    if (State.halfYearComplete) { State.villageStep = 6; Save(); SyncPoints(); }
                    else { State.villageStep = 5; BeginHalfYear(); }
                }
                if (Act == 3 && State.drift == 7 && !State.inferred) Infer();
            });
        }
        void OnDestroy() { if (Hunger) Hunger.OnToast -= Toast; }
        void Update()
        {
            if (climb && climb.IsLocked && TouchInput.Enabled) climb.SetMoveInput(TouchInput.Move);
            Hunger.StarvingMessage = Act == 1 ? "糧食快吃完了，先剝完樹皮" : "肚子餓了，去找些食物吧。";
            bool busy = Reading || journey.Busy || (ScrollUI.I && ScrollUI.I.IsBusy) || State.ended || SeasonPlaying;
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
            journey.SampleRescue();
            if (Act == 2 && State.climbed && !Reading) SyncPoints();
            if (!climb || !climb.IsLocked || !Player.cam) return;
            Player.cam.position = Player.transform.position + new Vector3(6, 2.5f, -4);
            Player.cam.LookAt(Player.transform.position + Vector3.up);
        }
        public void UseSkill(int skill)
        {
            if (Reading || journey.Busy || SeasonPlaying || State.ended || (ScrollUI.I && ScrollUI.I.IsBusy)) return;
            if (skill == 2) { if (Companion.Call()) Toast("已呼喚阮肇"); return; }
            var near = Interactable.Nearest(Player.transform.position);
            if (near && (skill == 0 ? near.name.StartsWith("Act1_Bark_") : near.name.StartsWith("Act3_Drift_"))) near.Interact();
            else Toast(skill == 0 ? "請靠近可剝皮的楮樹" : "請靠近溪中漂流物");
        }
        public void InteractPoint(string key)
        {
            Interactable point;
            if (Reading || journey.Busy || SeasonPlaying || State.ended || !points.TryGetValue(key, out point) || !point.enabled) return;
            if (Act == 1 && key.StartsWith("Act1_Bark_"))
            {
                State.bark |= 1 << (key[key.Length - 1] - '1'); journey.Progress();
                if (State.bark == 31) { State.act = 2; ReadAct(1, () => { journey.BeginAct(); Save(); SyncPoints(); }); }
            }
            else if (Act == 2 && key == "Act2_ClimbBase")
            {
                if (climb) climb.TryInteract();
            }
            else if (Act == 2 && key == "Act2_PeachTree")
            {
                if (Player.transform.position.y < 10) { Toast("回崖下攀援處重新攀爬"); return; }
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
            else if (Act == 3 && key == "Act3_Wade") { State.act = 4; MovePlayer(new Vector3(0, 0.2f, 63)); journey.BeginAct(); }
            else if (Act == 4)
            {
                if (key == "Act4_Women") journey.Talk("interact:Act4_Women", () => VillageStep(1));
                else if (key == "Act4_Home") journey.Talk("interact:Act4_Home", () => VillageStep(2));
                else if (key == "Act4_House")
                {
                    journey.ShowMeal(true);
                    journey.Talk("interact:Act4_House", () => {
                        VillageStep(3);
                        journey.Talk("after:meal", () => {
                            VillageStep(4);
                            ReadAct(4, () => journey.Talk("after:guests", () => { VillageStep(5); BeginHalfYear(); }));
                        });
                    });
                }
                else if (key == "Act4_Return") ReturnHome();
            }
            else if (Act == 5)
            {
                string trigger = key == "Act5_Return" ? "Act5_Village" : key;
                journey.Talk("interact:" + trigger, () => {
                    journey.Progress();
                    if (key == "Act5_Grandson") ReadAct(5, () => { State.ended = true; Save(); SyncPoints(); GetComponent<HudUI>().ShowEnding(); });
                    else { State.homeStep++; Save(); SyncPoints(); }
                });
            }
            Save(); SyncPoints();
        }
        IEnumerator FinishPicking()
        {
            float duration = 1;
            if (Player.animator) foreach (var clip in Player.animator.runtimeAnimatorController.animationClips)
                if (clip.name == "pickfruit") duration = clip.length + .15f;
            yield return new WaitForSeconds(duration);
            ReadAct(2, () => { State.act = 3; MovePlayer(new Vector3(-7, .2f, -16)); journey.BeginAct(); Save(); SyncPoints(); });
        }
        public void ReturnHome()
        {
            if (!CanReturn || (ScrollUI.I && ScrollUI.I.IsBusy)) return;
            journey.Talk("interact:Act4_Return", () => {
                Toast("女子集會奏樂，共送劉、阮"); journey.ShowMeal(false);
                State.act = 5; ApplyReturn(); MovePlayer(new Vector3(0, .2f, -104)); journey.BeginAct(); Save(); SyncPoints();
            });
        }
        void VillageStep(int value) { State.villageStep = value; journey.Progress(); Save(); SyncPoints(); }
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
                    if (Act == 2) enabled = k == "Act2_ClimbBase" || (State.climbed && k == "Act2_PeachTree");
                    if (Act == 3 && k.StartsWith("Act3_Drift_")) enabled = !State.inferred && (State.drift & (1 << (k[k.Length - 1] - '1'))) == 0;
                    if (Act == 3 && k == "Act3_Wade") enabled = State.inferred;
                    if (Act == 4) enabled = k == (State.villageStep == 0 ? "Act4_Women" : State.villageStep == 1 ? "Act4_Home" : State.villageStep == 2 ? "Act4_House" : State.villageStep == 6 ? "Act4_Return" : "");
                    if (Act == 5) enabled = k == new[] { "Act5_Return", "Act5_Villager", "Act5_Grandson" }[State.homeStep];
                }
                var prop = pair.Value.transform.Find("R2Prop");
                if (prop && k.StartsWith("Act4_")) prop.gameObject.SetActive(Act < 5);
                pair.Value.enabled = enabled;
                if (enabled && (!target || Act == 2 && State.climbed && Player.transform.position.y >= 10 && k == "Act2_PeachTree")) target = pair.Value.transform;
                var visual = pair.Value.transform.Find("R1Marker");
                if (visual) visual.gameObject.SetActive(false);
                var glow = pair.Value.transform.Find("R3Glow");
                if (glow) glow.gameObject.SetActive(enabled && pair.Value.transform == target);
            }
            foreach (var pair in points)
            {
                var glow = pair.Value.transform.Find("R3Glow");
                if (glow) glow.gameObject.SetActive(pair.Value.transform == target);
            }
            if (minimap) minimap.SetTarget(target);
            if (journey) journey.Sync(target);
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
        public static Vector3 ActStart(int act)
        {
            if (act == 2) return new Vector3(-11, .2f, -31);
            if (act == 3) return new Vector3(-5, .2f, -13);
            if (act == 4) return new Vector3(0, .2f, 63);
            return new Vector3(0, .2f, -104);
        }
        public void RecoverToSafe(Vector3 safe)
        { RecoveryCount++; if (climb) climb.Release(); MovePlayer(safe); Save(); }
        public void RecoverToStart()
        {
            RecoveryCount++;
            if (climb) climb.Release();
            MovePlayer(ActStart(Act)); Save();
        }
        public static void RestartJourney()
        {
            // 本遊戲所有偏好皆以 liuruan_ 開頭；DeleteAll 亦清除舊版本未知鍵及結局狀態。
            PlayerPrefs.DeleteAll(); PlayerPrefs.Save();
            ScrollUI.DeferHelp = false;
            if (ScrollUI.I) ScrollUI.I.HudOverride = null;
            PlayerController.CamShot = null;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
        public void SkipAct()
        {
            if (Reading || journey.Busy || SeasonPlaying || State.ended || (ScrollUI.I && ScrollUI.I.IsBusy)) return;
            if (climb) climb.Release();
            if (Act == 5) { State.ended = true; Save(); SyncPoints(); GetComponent<HudUI>().ShowEnding(); return; }
            AdvanceAct();
        }
        void AdvanceAct()
        {
            State.act = Mathf.Min(5, Act + 1);
            if (Act == 2) MovePlayer(new Vector3(-11, 0.2f, -31));
            if (Act == 3) MovePlayer(new Vector3(-5, 0.2f, -13));
            if (Act == 4) MovePlayer(new Vector3(0, 0.2f, 63));
            if (Act == 5) { ApplyReturn(); MovePlayer(new Vector3(0, 0.2f, -104)); }
            journey.BeginAct(); Save(); SyncPoints();
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DebugNextAct() { AdvanceAct(); }
#endif
    }
}
