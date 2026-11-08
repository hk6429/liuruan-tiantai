using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Liuruan
{
    // U 線介面與流程接線；Core 留在獨立 Runtime 組件。
    [DefaultExecutionOrder(-50)]
    public sealed class R3Journey : MonoBehaviour
    {
        public const string DoorPrompt = "跟二女到門口", HousePrompt = "屋內款待", ReturnPrompt = "向二女求歸", VillagerPrompt = "向村人詢問", GrandsonPrompt = "與七世孫交談", MealText = "胡麻飯　山羊脯";
        public const string StoryKey = "liuruan_r3_story", TutorialKey = "liuruan_r3_tutorial";
        [Serializable] public sealed class Page { public string text, orig; }
        [Serializable] public sealed class Story { public Page[] cards; }
        [Serializable] public sealed class Guide { public Page[] pages; }
        [Serializable] public sealed class NarrationAct { public int act; public DialogueLine[] lines; }
        [Serializable] public sealed class Narration { public NarrationAct[] acts; }
        public TextAsset storyJson, narrationJson, questionsJson, guideJson;
        public Sprite[] stories, portraits;
        public Sprite arrowSprite, cardFrame;
        public DialogueDirector dialogue;
        public ObjectiveTracker objectives;
        public StallHintSystem hints;
        public RescueSystem rescue;
        public OffscreenArrow arrow;
        public ReadingDirector reading;
        public R3Npc womanA, womanB;
        public Transform door;
        public GameObject meal;
        public Renderer water;
        GameFlow flow;
        Canvas canvas;
        GameObject modalPanel, storyPanel, termPanel, tutorialPanel;
        TMP_Text dialogueText, dialogueName, dialoguePlain, storyText, termText, tutorialText;
        Image portrait, storyImage, edge;
        RectTransform tutorialFocus;
        bool modal, next, skip;
        int releaseFrame = -1, syncAct, tutorial;
        string step;
        Transform target, shot;
        readonly HashSet<string> expanded = new HashSet<string>();
        float turnUntil, guideUntil, tutorialAt;
        public bool Busy => modal || Time.frameCount <= releaseFrame;
        public bool StoryVisible => storyPanel && storyPanel.activeSelf;
        public int TutorialStep => tutorial;
        public int StoryCount { get; private set; }
        public int TutorialCount { get; private set; }
        public bool TermVisible => termPanel && termPanel.activeSelf;
        public bool ArrowVisible => edge && edge.gameObject.activeSelf;
        public string[] GuidePages { get; private set; }
        public void Progress() { hints.Progress(); }
        void Awake() { flow = GetComponent<GameFlow>(); }
        void Start()
        {
            BuildUI();
            if (flow.Act == 4 && flow.State.villageStep >= 3) ShowMeal(true);
            var pages = JsonUtility.FromJson<Guide>(guideJson.text).pages;
            GuidePages = Array.ConvertAll(pages, p => p.text);
            hints.OnHint += Hint;
            rescue.OnRescue += Rescue;
        }
        void OnDestroy()
        {
            hints.OnHint -= Hint; rescue.OnRescue -= Rescue;
            Canvas.willRenderCanvases -= DrawArrow;
            if (PlayerController.CamShot == shot) PlayerController.CamShot = null;
            if (shot) Destroy(shot.gameObject);
        }
        public void BeginAct(Action after = null) { StartCoroutine(Opening(after)); }
        IEnumerator Opening(Action after)
        {
            yield return null; // HUD 與各資料元件的 Start 完成後才顯示。
            if (flow.Act != 1) { tutorial = 0; tutorialPanel.SetActive(false); tutorialFocus.gameObject.SetActive(false); }
            rescue.ResetSafePoint(GameFlow.ActStart(flow.Act)); syncAct = flow.Act;
            if (flow.Act == 1 && PlayerPrefs.GetInt(StoryKey, 0) == 0) yield return StoryCards();
            foreach (var act in JsonUtility.FromJson<Narration>(narrationJson.text).acts)
                if (act.act == flow.Act)
                    foreach (var line in act.lines) { line.speaker = "旁白"; yield return Line(line); }
            yield return Conversation("act_start:" + flow.Act);
            if (flow.Act == 1 && PlayerPrefs.GetInt(TutorialKey, 0) == 0)
            { tutorial = 1; tutorialAt = Time.unscaledTime; TutorialCount++; TurnToTarget(1.5f); }
            after?.Invoke();
        }
        public void ReplayStory() { if (!Busy) StartCoroutine(StoryCards()); }
        IEnumerator StoryCards()
        {
            modal = true; StoryCount++; storyPanel.SetActive(true);
            var cards = JsonUtility.FromJson<Story>(storyJson.text).cards;
            skip = false;
            for (int i = 0; i < cards.Length && !skip; i++)
            {
                storyImage.sprite = stories[i];
                storyText.text = cards[i].text + (string.IsNullOrEmpty(cards[i].orig) ? "" : "\n<color=#F6CE87>原文：" + cards[i].orig + "</color>");
                next = false;
                while (!next && !skip) yield return null;
            }
            storyPanel.SetActive(false); modal = false; releaseFrame = Time.frameCount;
            PlayerPrefs.SetInt(StoryKey, 1); PlayerPrefs.Save();
        }
        public void Next() { next = true; }
        public void Talk(string trigger, Action done)
        { StartCoroutine(TalkAndThen(trigger, done)); }
        IEnumerator TalkAndThen(string trigger, Action done)
        { yield return Conversation(trigger); done?.Invoke(); }
        IEnumerator Conversation(string trigger)
        {
            while (modal || dialogue.IsActive) yield return null;
            if (!dialogue.TryTrigger(trigger)) yield break;
            while (dialogue.IsActive)
            {
                yield return Line(dialogue.CurrentLine);
                dialogue.Next();
            }
        }
        IEnumerator Line(DialogueLine line)
        {
            while (modal) yield return null;
            modal = true; modalPanel.SetActive(true); next = false;
            dialogueName.text = line.speaker;
            portrait.sprite = Portrait(line.speaker);
            portrait.gameObject.SetActive(portrait.sprite);
            dialogueText.color = line.type == "original" ? new Color(1, .83f, .52f) : Color.white;
            dialogueText.text = (string.IsNullOrEmpty(line.label) ? "" : line.label + "\n") + line.text;
            dialoguePlain.text = !string.IsNullOrEmpty(line.orig) ? "原文對照：" + line.orig : PlainFor(line.text);
            yield return null;
            while (!next) yield return null;
            modalPanel.SetActive(false); modal = false; releaseFrame = Time.frameCount;
        }
        Sprite Portrait(string speaker)
        { if (speaker == "旁白") return null; return portraits[speaker == "劉晨" ? 0 : speaker == "阮肇" ? 1 : speaker == "女子" ? 2 : speaker == "群女" ? 3 : speaker == "七世孫" ? 4 : 1]; }
        string PlainFor(string original)
        {
            var hits = new List<(int at, int len, string plain)>();
            for (int p = 1; p <= 7; p++) foreach (var sentence in reading.GetParagraph(p).sentences)
            {
                string key = sentence.orig.Trim('「', '」', '，', '。', '：', '、', '？', '！');
                int at = key.Length > 2 ? original.IndexOf(key, System.StringComparison.Ordinal) : -1;
                if (at >= 0) hits.Add((at, key.Length, sentence.plain));
            }
            hits.Sort((a, b) => a.at != b.at ? a.at.CompareTo(b.at) : b.len.CompareTo(a.len));
            var s = new StringBuilder(); int end = 0;
            foreach (var h in hits) if (h.at >= end) { s.Append(h.plain); end = h.at + h.len; }
            return s.ToString();
        }
        public void Sync(Transform value)
        {
            target = value; arrow.SetTarget(value);
            if (objectives.Core == null) return;
            if (syncAct != flow.Act) { rescue.ResetSafePoint(GameFlow.ActStart(flow.Act)); syncAct = flow.Act; }
            string id = flow.Act == 1 ? "bark" : flow.Act == 2 ? (flow.State.climbed ? "peach" : "climb") :
                flow.Act == 3 ? (flow.State.inferred ? "cross_mountain" : flow.State.drift == 7 ? "infer" : (flow.State.drift & 1) == 0 ? "leaf" : (flow.State.drift & 2) == 0 ? "cup" : "rice") :
                flow.Act == 4 ? new[] { "women", "follow", "house", "guests", "half_year", "birds", "return" }[flow.State.villageStep] :
                new[] { "village", "ask", "grandson" }[flow.State.homeStep];
            if (step != id || objectives.Core.CurrentStep == null) { step = id; objectives.SetStep(flow.Act, id, flow.Act == 1 ? flow.BarkCount : 0); hints.SetStep(flow.Act, id); }
            else if (flow.Act == 1) objectives.SetCount(flow.BarkCount);
            if (flow.Act == 4 && flow.State.villageStep >= 1)
            { womanA.Destination = door; womanB.Destination = door; }
        }
        public void SampleRescue()
        {
            if (!rescue || rescue.Core == null) return;
            var cc = flow.Player.GetComponent<CharacterController>();
            var p = flow.Player.transform.position;
            bool inWater = water && p.x >= water.bounds.min.x && p.x <= water.bounds.max.x && p.z >= water.bounds.min.z && p.z <= water.bounds.max.z && p.y < water.bounds.max.y + .5f;
            rescue.Sample(Time.deltaTime, p, cc.isGrounded, inWater);
        }
        void Rescue(Vector3 safe)
        {
            if (flow.climb) flow.climb.Release();
            flow.RecoverToSafe(safe);
            ScrollUI.I.Toast("迷路了，回到山徑上");
        }
        void Hint(StallHint hint)
        {
            ScrollUI.I.Toast(hint.Level == 3 ? "阮肇：「" + hint.Text + "」" : hint.Text);
            if (target && hint.BrightenTarget)
            {
                var glow = target.GetComponentInChildren<FindGlow>();
                if (glow) glow.Brightness = 3;
            }
            if (hint.GuideCompanion) { flow.Companion.GuideTarget = target; guideUntil = Time.time + 8; }
            if (hint.TurnCamera) TurnToTarget(1.5f);
        }
        void TurnToTarget(float seconds)
        {
            if (!target) return;
            if (!shot) shot = new GameObject("R3CameraCue").transform;
            shot.SetPositionAndRotation(flow.Player.cam.position, Quaternion.LookRotation(target.position + Vector3.up - flow.Player.cam.position));
            PlayerController.CamShot = shot; turnUntil = Time.time + seconds;
        }
        public void ShowMeal(bool show) { meal.SetActive(show); }
        void Update()
        {
            if (!canvas) return;
            bool tall = Screen.height > Screen.width;
            canvas.GetComponent<CanvasScaler>().referenceResolution = tall ? new Vector2(720, 1280) : new Vector2(1280, 720);
            hints.SetDevice(TouchInput.Enabled);
            hints.SetPaused(Busy || flow.Reading || ScrollUI.I.IsBusy || flow.SeasonPlaying || flow.State.ended || tutorial > 0);
            ScrollUI.I.KeepScene = modalPanel.activeSelf;
            if (Busy && !TermVisible) ScrollUI.I.ExternalUI = true;
            else if (Time.frameCount == releaseFrame + 1 && !flow.State.ended) ScrollUI.I.ExternalUI = false;
            if (modal && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || TouchInput.ConsumeDown())) Next();
            if (shot && PlayerController.CamShot == shot && Time.time >= turnUntil) PlayerController.CamShot = null;
            if (flow.Companion.GuideTarget && Time.time >= guideUntil) flow.Companion.GuideTarget = null;
            if (tutorial > 0) Tutorial();
        }
        void DrawArrow()
        {
            if (!edge) return;
            bool show = arrow.HasResult && !arrow.Result.IsOnscreen && !Busy && !flow.Reading && !flow.State.ended;
            edge.gameObject.SetActive(show);
            if (!show) return;
            var r = arrow.Result; var cameraRect = Camera.main.pixelRect; var safe = Screen.safeArea;
            edge.rectTransform.position = new Vector2(Mathf.Clamp(cameraRect.x + r.X * cameraRect.width, safe.xMin + 32, safe.xMax - 32), Mathf.Clamp(cameraRect.y + r.Y * cameraRect.height, safe.yMin + 32, safe.yMax - 32));
            edge.rectTransform.rotation = Quaternion.Euler(0, 0, r.AngleDegrees); // 素材朝右。
        }
        void Tutorial()
        {
            tutorialPanel.SetActive(true);
            string text;
            if (tutorial == 1)
            {
                text = "用 WASD／搖桿走到第一棵楮樹光柱";
                if (target && Vector2.Distance(new Vector2(flow.Player.transform.position.x, flow.Player.transform.position.z), new Vector2(target.position.x, target.position.z)) <= target.GetComponent<Interactable>().radius) tutorial = 2;
            }
            else if (tutorial == 2)
            {
                text = "按 E／互動鈕，剝取樹皮";
                if (flow.BarkCount > 0) { tutorial = 3; tutorialAt = Time.unscaledTime; ScrollUI.I.Toast("很好！還有 4 棵"); }
            }
            else
            {
                string focusName = tutorial == 3 ? "Peach0" : tutorial == 4 ? "Objective" : "Help";
                text = tutorial == 3 ? "↑ 飽足度：桃子變少就要找食物" : tutorial == 4 ? "↑ 任務欄：看目標、數量與方向" : "↑ ? 按鈕：操作說明與重看故事";
                var focus = GameObject.Find("JourneySystems/JourneyHUD/" + focusName)?.GetComponent<RectTransform>();
                tutorialFocus.gameObject.SetActive(focus);
                if (focus) { tutorialFocus.position = focus.position; tutorialFocus.sizeDelta = focus.rect.size + Vector2.one * 12; }
                if (Time.unscaledTime - tutorialAt >= 3)
                {
                    tutorial++; tutorialAt = Time.unscaledTime;
                    if (tutorial == 6) { tutorial = 0; tutorialPanel.SetActive(false); tutorialFocus.gameObject.SetActive(false); PlayerPrefs.SetInt(TutorialKey, 1); PlayerPrefs.Save(); }
                }
            }
            tutorialText.text = text;
        }
        public string ReadingMarkup(ClueData card)
        {
            int[] ps = card.act == 4 ? new[] { 4, 5, 6 } : new[] { card.act == 5 ? 7 : card.act };
            var sb = new StringBuilder();
            foreach (int p in ps)
            {
                var paragraph = reading.GetParagraph(p);
                if (paragraph == null) continue;
                for (int i = 0; i < paragraph.sentences.Length; i++)
                {
                    var sentence = paragraph.sentences[i]; string key = p + ":" + i;
                    sb.Append(reading.Core.Markup(p, i, expanded.Contains(key)));
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }
        public void ReadingLink(string link)
        {
            if (link.StartsWith("s:")) { string key = link.Substring(2); if (!expanded.Add(key)) expanded.Remove(key); ScrollUI.I.RefreshR3Reading(); }
            else if (link.StartsWith("t:"))
            {
                var term = reading.FindTerm(link.Substring(2));
                if (term == null) return;
                termText.text = term.term + "　" + term.reading + "\n\n" + term.meaning;
                termPanel.SetActive(true); termPanel.transform.SetAsLastSibling();
            }
        }
        public void CloseTerm() { termPanel.SetActive(false); }
        void BuildUI()
        {
            var go = new GameObject("R3UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false); canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
            Canvas.willRenderCanvases += DrawArrow;
            go.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            modalPanel = Panel("Dialogue", new Vector2(.03f, .04f), new Vector2(.97f, .36f));
            modalPanel.AddComponent<Button>().onClick.AddListener(Next);
            portrait = Image("Portrait", modalPanel.transform, new Vector2(.01f, .12f), new Vector2(.18f, .92f)); portrait.preserveAspect = true;
            dialogueName = Text("Speaker", modalPanel.transform, new Vector2(.2f, .79f), new Vector2(.96f, .99f), 26);
            dialogueText = Text("Line", modalPanel.transform, new Vector2(.2f, .3f), new Vector2(.97f, .8f), 29);
            dialoguePlain = Text("Plain", modalPanel.transform, new Vector2(.2f, .02f), new Vector2(.58f, .3f), 22);
            Button("DialogueNext", "下一句　E／空白鍵", modalPanel.transform, new Vector2(.60f, .0f), new Vector2(.98f, .18f), Next);
            storyPanel = Panel("Story", new Vector2(.02f, .03f), new Vector2(.98f, .97f));
            storyImage = Image("Illustration", storyPanel.transform, new Vector2(.02f, .32f), new Vector2(.98f, .98f)); storyImage.preserveAspect = true;
            storyText = Text("StoryText", storyPanel.transform, new Vector2(.06f, .13f), new Vector2(.94f, .32f), 29);
            var advance = storyImage.gameObject.AddComponent<Button>(); storyImage.raycastTarget = true; advance.onClick.AddListener(Next);
            Button("StoryNext", "下一張", storyPanel.transform, new Vector2(.55f, .02f), new Vector2(.94f, .11f), Next);
            Button("StorySkip", "跳過故事", storyPanel.transform, new Vector2(.06f, .02f), new Vector2(.45f, .11f), () => skip = true);
            tutorialPanel = Panel("Tutorial", new Vector2(.15f, .4f), new Vector2(.85f, .54f));
            tutorialPanel.GetComponent<Image>().raycastTarget = false;
            tutorialText = Text("TutorialText", tutorialPanel.transform, Vector2.zero, Vector2.one, 29);
            tutorialFocus = Image("TutorialFocus", canvas.transform, Vector2.zero, Vector2.zero).rectTransform;
            tutorialFocus.GetComponent<Image>().color = new Color(1, .82f, .2f, .35f); tutorialFocus.GetComponent<Image>().raycastTarget = false;
            edge = Image("OffscreenArrow", canvas.transform, Vector2.zero, Vector2.zero); edge.sprite = arrowSprite; edge.rectTransform.sizeDelta = new Vector2(54, 54); edge.raycastTarget = false;
            termPanel = Panel("TermCard", new Vector2(.10f, .32f), new Vector2(.90f, .7f));
            termPanel.GetComponent<Image>().sprite = cardFrame; termPanel.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced; termPanel.GetComponent<Image>().color = Color.white;
            termText = Text("TermText", termPanel.transform, new Vector2(.08f, .22f), new Vector2(.92f, .90f), 28); termText.color = new Color(.2f, .13f, .08f);
            Button("TermClose", "關閉詞卡", termPanel.transform, new Vector2(.30f, .03f), new Vector2(.70f, .2f), CloseTerm);
            modalPanel.SetActive(false); storyPanel.SetActive(false); tutorialPanel.SetActive(false); tutorialFocus.gameObject.SetActive(false); termPanel.SetActive(false); edge.gameObject.SetActive(false);
        }
        GameObject Panel(string name, Vector2 min, Vector2 max)
        {
            var img = Image(name, canvas.transform, min, max); img.color = new Color(.08f, .13f, .13f, .98f); return img.gameObject;
        }
        static RectTransform Rect(GameObject go, Transform parent, Vector2 min, Vector2 max)
        {
            var r = (RectTransform)go.transform; r.SetParent(parent, false); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero; return r;
        }
        Image Image(string name, Transform parent, Vector2 min, Vector2 max)
        { var go = new GameObject(name, typeof(RectTransform), typeof(Image)); Rect(go, parent, min, max); return go.GetComponent<Image>(); }
        TMP_Text Text(string name, Transform parent, Vector2 min, Vector2 max, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); Rect(go, parent, min, max);
            var text = go.GetComponent<TextMeshProUGUI>(); text.font = GetComponent<HudUI>().font; text.fontSize = size;
            text.enableAutoSizing = true; text.fontSizeMin = 20; text.fontSizeMax = size; text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false; return text;
        }
        void Button(string name, string text, Transform parent, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var img = Image(name, parent, min, max); img.color = new Color(.24f, .34f, .25f);
            img.gameObject.AddComponent<Button>().onClick.AddListener(action);
            Text("Label", img.transform, Vector2.zero, Vector2.one, 25).text = text;
        }
    }
}
