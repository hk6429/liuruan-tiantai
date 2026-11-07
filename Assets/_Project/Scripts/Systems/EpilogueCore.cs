using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Liuruan
{
    [Serializable] public sealed class EpilogueClueDocument { public EpilogueClue[] clues = null; }
    [Serializable] public sealed class EpilogueClue
    {
        public int id = 0;
        public bool isEnding = false;
        public string original = null;
    }
    [Serializable] public sealed class EpilogueCompareDocument { public EpilogueCompareRow[] rows = null; }
    [Serializable] public sealed class EpilogueCompareRow
    { public string aspect = null, liuruan = null, taohua = null; }

    public readonly struct ReadingResult
    {
        public int Id { get; }
        public bool FirstTryCorrect { get; }
        public ReadingResult(int id, bool firstTryCorrect) { Id = id; FirstTryCorrect = firstTryCorrect; }
    }
    public sealed class EndingQuestion
    {
        public string Prompt => "為什麼找到的是七世孫？";
        public ReadOnlyCollection<string> Options { get; } = Array.AsReadOnly(new[] {
            "「攀葛乃得至」與「而饑止體充」",
            "「遂停半年」與「問得七世孫」",
            "「見蕪菁葉從山腹流出」與「此處去人徑不遠」" });
        public int AnswerIndex => 1;
        public string Explanation => "「遂停半年」與「問得七世孫」；以異境與人間時間落差為合理推論，不假稱原文明說固定換算率。";
    }
    public readonly struct ComparisonRow
    {
        public string Aspect { get; }
        public string Liuruan { get; }
        public string Taohua { get; }
        public ComparisonRow(EpilogueCompareRow row)
        { Aspect = row.aspect; Liuruan = row.liuruan; Taohua = row.taohua; }
    }
    public readonly struct EndingScore
    {
        public int BarkCount { get; }
        public int DriftCount { get; }
        public int ReadingCorrect { get; }
        public int ReadingTotal { get; }
        public float ReadingAccuracy => ReadingTotal == 0 ? 0 : (float)ReadingCorrect / ReadingTotal;
        public float TotalSeconds { get; }
        public EndingScore(int bark, int drift, int correct, int total, float seconds)
        {
            HungerCore.RequireFinite(seconds, nameof(seconds));
            if (bark < 0 || bark > 5 || drift < 0 || drift > 3 || total < 0 || correct < 0 || correct > total || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(bark));
            BarkCount = bark; DriftCount = drift; ReadingCorrect = correct; ReadingTotal = total; TotalSeconds = seconds;
        }
    }
    public sealed class EpilogueModel
    {
        public ReadOnlyCollection<string> Paragraphs { get; }
        public EndingQuestion Question { get; } = new EndingQuestion();
        public ReadOnlyCollection<ComparisonRow> Comparison { get; }
        public EndingScore Score { get; }
        internal EpilogueModel(string[] paragraphs, ComparisonRow[] comparison, EndingScore score)
        { Paragraphs = Array.AsReadOnly(paragraphs); Comparison = Array.AsReadOnly(comparison); Score = score; }
    }
    public static class EpilogueCore
    {
        public static EpilogueModel Build(EpilogueClueDocument clues, EpilogueCompareDocument compare,
            int bark, int drift, ReadingResult[] readings, float totalSeconds)
        {
            if (clues?.clues == null || compare?.rows == null || readings == null)
                throw new ArgumentException("結局需要 clues、compare 與閱讀成績。");
            EpilogueClue ending = null;
            var ids = new HashSet<int>();
            var readingIds = new HashSet<int>();
            foreach (var clue in clues.clues)
            {
                if (clue == null || clue.id < 0 || !ids.Add(clue.id)) throw new ArgumentException("線索 id 無效或重複。");
                if (clue.isEnding)
                {
                    if (ending != null) throw new ArgumentException("結局全文必須恰有一筆。");
                    ending = clue;
                }
                else readingIds.Add(clue.id);
            }
            if (ending == null || string.IsNullOrWhiteSpace(ending.original)) throw new ArgumentException("缺少結局全文。");
            var paragraphs = ending.original.Replace("\r\n", "\n").Replace('\r', '\n')
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int correct = 0;
            var seen = new HashSet<int>();
            foreach (var result in readings)
            {
                if (!readingIds.Contains(result.Id) || !seen.Add(result.Id)) throw new ArgumentException("閱讀成績 id 無效或重複。");
                if (result.FirstTryCorrect) correct++;
            }
            var rows = new ComparisonRow[compare.rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                var row = compare.rows[i];
                if (row == null || string.IsNullOrWhiteSpace(row.aspect) || string.IsNullOrWhiteSpace(row.liuruan) || string.IsNullOrWhiteSpace(row.taohua))
                    throw new ArgumentException("比較表欄位不可空白。");
                rows[i] = new ComparisonRow(row);
            }
            return new EpilogueModel(paragraphs, rows, new EndingScore(bark, drift, correct, readingIds.Count, totalSeconds));
        }
    }
}
