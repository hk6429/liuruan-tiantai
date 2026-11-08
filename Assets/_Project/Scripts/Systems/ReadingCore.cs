using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Liuruan
{
    [Serializable] public sealed class SentenceDocument { public ReadingParagraph[] paragraphs = null; }
    [Serializable] public sealed class ReadingParagraph { public int p = 0; public ReadingSentence[] sentences = null; }
    [Serializable] public sealed class ReadingSentence { public string orig = null, plain = null; public string[] terms = null; }
    [Serializable] public sealed class GlossaryDocument { public GlossaryCard[] terms = null; }
    [Serializable] public sealed class GlossaryCard { public string term = null, reading = null, meaning = null; }
    public enum ReadingMode { Original, OriginalAndPlain }
    public sealed class ReadingCore
    {
        readonly Dictionary<int, ReadingParagraph> paragraphs = new Dictionary<int, ReadingParagraph>();
        readonly Dictionary<string, GlossaryCard> terms = new Dictionary<string, GlossaryCard>();
        public ReadOnlyCollection<string> MissingTerms { get; }
        public ReadingMode Mode { get; private set; }
        public event Action<ReadingMode> OnModeChanged;
        public ReadingCore(SentenceDocument sentences, GlossaryDocument glossary)
        {
            foreach (var card in R3Data.Required(glossary?.terms, "glossary.terms"))
            {
                R3Data.Text(card.term, "term"); R3Data.Text(card.meaning, "meaning");
                if (terms.ContainsKey(card.term)) throw new ArgumentException("詞卡重複：" + card.term);
                terms.Add(card.term, card);
            }
            var missing = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var paragraph in R3Data.Required(sentences?.paragraphs, "sentences.paragraphs"))
            {
                if (paragraph.p < 1 || paragraphs.ContainsKey(paragraph.p)) throw new ArgumentException("段落編號無效或重複。");
                foreach (var sentence in R3Data.Required(paragraph.sentences, "paragraph.sentences"))
                {
                    R3Data.Text(sentence.orig, "sentence.orig"); R3Data.Text(sentence.plain, "sentence.plain");
                    if (sentence.terms == null) throw new ArgumentException("sentence.terms 不可缺少，可用空陣列。");
                    foreach (string term in sentence.terms)
                    { R3Data.Text(term, "sentence.term"); if (!terms.ContainsKey(term)) missing.Add(term); }
                }
                paragraphs.Add(paragraph.p, paragraph);
            }
            MissingTerms = Array.AsReadOnly(new List<string>(missing).ToArray());
        }
        public ReadingParagraph GetParagraph(int p) => paragraphs.TryGetValue(p, out var value) ? value : null;
        public GlossaryCard FindTerm(string term) => term != null && terms.TryGetValue(term, out var value) ? value : null;
        public void SetMode(ReadingMode mode)
        {
            if (mode != ReadingMode.Original && mode != ReadingMode.OriginalAndPlain) throw new ArgumentOutOfRangeException(nameof(mode));
            if (mode == Mode) return;
            Mode = mode; OnModeChanged?.Invoke(mode);
        }
        public string Markup(int paragraph, int index, bool expanded)
        {
            var entry = GetParagraph(paragraph);
            if (entry == null || index < 0 || index >= entry.sentences.Length) throw new ArgumentOutOfRangeException(nameof(index));
            var sentence = entry.sentences[index];
            string key = "s:" + paragraph + ":" + index;
            var result = new StringBuilder();
            int cursor = 0, run = 0;
            while (cursor < sentence.orig.Length)
            {
                string found = null;
                foreach (string term in sentence.terms)
                    if (cursor + term.Length <= sentence.orig.Length && string.CompareOrdinal(sentence.orig, cursor, term, 0, term.Length) == 0 && (found == null || term.Length > found.Length)) found = term;
                if (found == null) { cursor++; continue; }
                if (cursor > run) result.Append("<link=\"").Append(key).Append("\">").Append(sentence.orig,run,cursor-run).Append("</link>");
                result.Append("<link=\"t:").Append(found).Append("\"><u>").Append(found).Append("</u></link>");
                cursor += found.Length; run = cursor;
            }
            if (cursor > run) result.Append("<link=\"").Append(key).Append("\">").Append(sentence.orig,run,cursor-run).Append("</link>");
            result.Append(" <link=\"").Append(key).Append("\"><color=#256C70>〔白話〕</color></link>");
            if (expanded) result.Append("\n<color=#256C70>").Append(sentence.plain).Append("</color>");
            return result.ToString();
        }
        public string Display(ReadingSentence sentence)
        {
            if (sentence == null) throw new ArgumentNullException(nameof(sentence));
            return Mode == ReadingMode.Original ? sentence.orig : sentence.orig + "\n" + sentence.plain;
        }
    }
}
