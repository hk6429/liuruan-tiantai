namespace Liuruan
{
    /// 遊戲內所有中文 UI 文案集中在這裡（FontSubset 會掃 Scripts/*.cs 的字串常值）。
    /// 改字後用 execute_code 呼叫 FontSubset.RebuildNoDialog()，否則 WebGL 上會變方框。
    public static class Txt
    {
        // ---------- 互動 ----------
        public const string DefaultPrompt = "E 查看";
        public const string InteractTooFar = "附近沒有可互動目標，請靠近發光卷軸或人物再試一次。";
        public const string RereadPrompt = "E 再讀";            // Round14：已收集的線索點
        public const string TouchPromptPrefix = "點「互動」";
        public const string TouchInteract = "互動";

        // ---------- HUD ----------
        public static string HudTouch(int got, int total) =>
            $"線索冊・主線 {got}／{total}\n拖曳方向盤移動　滑動畫面轉視角　按「互動」查看";
        public static string HudDesk(int got, int total) =>
            $"線索冊・主線 {got}／{total}　［Tab／J 開啟］\nWASD 移動　右鍵拖曳轉視角　E 互動　M 朗讀";

        // ---------- Toast ----------
        public const string Opening = "沿著森林小徑向北探索。";
        public const string FirstScrollComplete = "線索已收錄。";
        public static string EndingLocked(int got, int total) =>
            $"線索還沒集齊喔（{got}／{total}），再四處找找吧！";
        public static string RequiresLocked(string title) => $"先去看看「{title}」吧！";
        public const string EndingReady = "線索已收齊，可以繼續閱讀。";

        // ---------- 卷軸 ----------
        public const string RollPrefix = "卷";
        public const string Numerals = "一二三四五六七八九十";
        public const string EndingTag = "結局";
        public const string InferTitle = "結局·推論";
        public const string QuestionPrefix = "問：";
        public const string ReadToEndHint = "讀完全文後作答 →";  // Round14：卷軸多頁時，翻到最後一頁才出題
        public const string PlainPrefix = "白話：";
        public const string WrongTryAgain = "再想想看！回頭看看上面的線索。";
        public const string WrongSeePlainBtn = "再想想看！讀不懂原文的話，可以按「看白話」。";
        public const string WrongReadPlain = "再想想看！可以回頭讀一讀上面的白話。";
        public const string LookHighlight = "\n（看看上面標亮的地方）";
        public const string StageOrigHidden = "<color=#9A8C78>（原文會露出答案，作答後才展開）</color>";
        public const string StageCorrect = "答對了！";
        public const string Continue = "繼續";
        public const string Collect = "收進線索冊";
        public const string Speak = "朗讀";
        public const string SeePlain = "看白話";
        public const string OptionLetters = "ABCD";
        // 對錯符號依國中考卷慣例：答對 ○（U+25CB）、答錯 ×（U+00D7）。兩字都是全形寬、靜態字型子集已收。
        // 舊的 ✓（U+2713）在 Noto Serif TC 是半寬比例字形，和全形 × 並排大小不一；✗（U+2717）來源字型沒有。
        public const string CorrectMark = "○ ";
        public const string WrongMark = "× ";
        public const string PrevPage = "上一頁";
        public const string NextPage = "下一頁";
        public static string PageOf(int page, int total) => $"{page}／{total}";

        // ---------- 按鈕列 ----------
        // Round8 起這顆鈕控制朗讀（對話自動朗讀＋卷軸「朗讀」鈕）；R18 起優先播 StreamingAssets/voice 的大乃預錄 mp3，查不到才退回瀏覽器 Web Speech
        public const string ReadOn = "朗讀：開";
        public const string ReadOff = "朗讀：關";
        public const string QualityHigh = "畫質：高";
        public const string QualityLow = "畫質：低";
        // 字級三段（Round6）：標準 → 大 → 特大 → 標準；影響卷軸原文＋白話、選項、回饋、劇情對話框、小遊戲文字
        public const string FontNormal = "字級：標準";
        public const string FontBig = "字級：大";
        public const string FontXL = "字級：特大";
        public static string FontLabel(int level) => level >= 2 ? FontXL : level == 1 ? FontBig : FontNormal;
        public const string Book = "線索冊";
        public const string ScoreBtn = "成績";
        public const string Close = "關閉";
        public const string Help = "操作說明";
        public const string Restart = "重新開始";
        public const string Start = "開始";
        public const string KeepWalking = "繼續逛逛";

        // ---------- 線索冊 ----------
        public static string BookHeader(int got, int total) =>
            $"<size=40><b>線索冊・主線</b></size>　已收集 {got}／{total}（不含結局）\n";
        public const string NotFound = "？？？（尚未找到）";

        // ---------- 操作說明 ----------
        public const string HelpTitle = "<size=130%><b>操作說明</b></size>\n\n";
        public const string HelpTouch = "左下方向盤　拖曳移動\n滑動畫面　轉動視角\n右下「互動」　查看、對話\n右上「線索冊」　看收集到的線索";
        public const string HelpDesk = "WASD／方向鍵　移動\n按住右鍵拖曳　轉動視角\nE　查看、對話\nTab／J　打開線索冊\nM　朗讀開／關\n←→　翻頁　空白鍵／Enter　繼續\n1–4　選答案　V　看白話　R　朗讀\nEsc　關閉線索冊／有「關閉」的面板";
        public static string HelpTask(int gardenCount) =>
            "\n\n閱讀卷軸並完成答題後，再收進線索冊。";
        public const string Credits = "\n\n製作：大乃老師　字型：Noto Serif TC（OFL）";

        // ---------- 結局 ----------
        public const string FullTitle = "〈劉晨阮肇入天台〉選文";
        public const string FullOutro = "";
        public const string InferOriginalPrefix = "一路的線索：";
        public const string EvidenceTitle = "線索證據卡";
        public const string EvidenceIntro = "<color=#6B5638>推論之前，先把一路收集到的證據看一遍。</color>\n\n";
        public const string StartInfer = "開始推論";
        public const string ReflectionTitle = "想一想";
        public const string ReflectionNote = "\n\n<size=85%><color=#6B5638>（這題不計分，想好了可以寫在學習單上）</color></size>";
        public const string ReflectionDone = "想好了";
        public const string ReadFullText = "選文全文";
        public const string BackToScore = "回到成績卡";
        public const string EndingReveal = "";

        // ---------- 成績卡 ----------
        public const string ScoreTitleTop = "閱讀完成";
        public const string ScoreLineTop = "繼續整理你的發現。";
        public const string ScoreTitleMid = "持續探索";
        public const string ScoreLineMid = "可以回頭再讀線索。";
        public const string ScoreTitleLow = "再次閱讀";
        public const string ScoreLineLow = "回顧原文，再試一次。";
        public static string ScoreStats(int first, int total, int noPlain, int plainTotal, int tries) =>
            $"首答正確 <b>{first}／{total}</b>　不看白話答對 {noPlain}／{plainTotal} 題　共作答 {tries} 次\n";
        public static string ScoreTitle(string title, string line) =>
            $"<size=130%><color=#7F542E><b>稱號：{title}</b></color></size>　{line}\n";
        public const string ResultHeader = "<size=85%>每題結果（首答）：";
        public const string ResultTail = "</size>\n";
        public const string InferShort = "推論";
        public const string ResultRight = "○";
        public const string ResultWrong = "×";
        public const string ResultNone = "－";
        public const string ScoreShot ="<size=80%><color=#9A8C78>截圖交給老師（請在截圖旁寫上班級、座號）</color></size>";

    }
}
