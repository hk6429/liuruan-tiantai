// Tools/Liuruan/Rebuild Font Subset
// 重新產生靜態字型「NotoSerifTC Static SDF」的字集：
//   clues.json 全部非 ASCII 字元 + Scripts/**/*.cs 字串常值（文案集中在 Strings.cs）中的非 ASCII 字元 + ASCII 可列印字元 + 常用全形標點。
// 作法（依 com.unity.ugui 2.0 內建 TMP 原始碼查證）：
//   暫時切成 Dynamic（編輯器會由 m_SourceFontFileGUID 找回 NotoSerifTC-Regular.otf）
//   → ClearFontAssetData(false) 保留 2048 圖集尺寸 → TryAddCharacters → 切回 Static → 存檔。
// 原地改寫同一個 .asset，GUID 不變，ScrollUI 等既有引用不受影響。
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Liuruan.EditorTools
{
    public static class FontSubset
    {
        const string FontAssetPath = "Assets/_Project/Fonts/NotoSerifTC Static SDF.asset";
        const string CluesPath = "Assets/_Project/Data/clues.json";
        const string ScriptsDir = "Assets/_Project/Scripts";
        static readonly string[] SceneDirs = { "Assets/_Project/Scenes", "Assets/Scenes" };

        const string CommonPunctuation =
            "，。、；：？！「」『』（）《》〈〉【】〔〕—…·～　＋－＝％／＊＃＠＆" +
            "０１２３４５６７８９" +
            "○●◎◇◆□■△▲▽▼☆★→←↑↓✓✔✗✘";

        static readonly Regex StringLiteral = new Regex(
            "@\"(?:[^\"]|\"\")*\"|\\$?\"(?:[^\"\\\\\\n]|\\\\.)*\"|'(?:[^'\\\\\\n]|\\\\.)'",
            RegexOptions.Compiled);
        static readonly Regex UnicodeEscape = new Regex(@"\\u([0-9a-fA-F]{4})", RegexOptions.Compiled);

        [MenuItem("Tools/Liuruan/Rebuild Font Subset")]
        static void Rebuild()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null) { Debug.LogError("[FontSubset] 找不到 " + FontAssetPath); return; }

            string chars = CollectCharacters(out int fromJson, out int fromCs);
            if (!EditorUtility.DisplayDialog("Rebuild Font Subset",
                    $"將以 {chars.Length} 個字元重建「{fontAsset.name}」\n" +
                    $"（clues.json {fromJson}、Scripts {fromCs}、ASCII/標點補充）\n原有字集會被清空。繼續？",
                    "重建", "取消"))
                return;
            DoRebuild(fontAsset, chars);
        }

        /// R17：不跳對話框的重建（給 UnityMCP execute_code 呼叫；選單版的 DisplayDialog 會卡住自動化）。回傳摘要。
        public static string RebuildNoDialog()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null) return "找不到 " + FontAssetPath;
            string chars = CollectCharacters(out int fromJson, out int fromCs);
            string missing = DoRebuild(fontAsset, chars);
            return $"chars={chars.Length} json={fromJson} cs={fromCs} table={fontAsset.characterTable.Count} missing={missing}";
        }

        /// 檢查字型是否收了這些字（回傳缺的字）
        public static string MissingIn(string text)
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            var sb = new StringBuilder();
            foreach (char c in text ?? "")
                if (c > 0x20 && !char.IsWhiteSpace(c) && !fontAsset.HasCharacter(c)) sb.Append(c);
            return sb.ToString();
        }

        static string DoRebuild(TMP_FontAsset fontAsset, string chars)
        {
            string missingOut = "";
            int before = fontAsset.characterTable.Count;
            var oldMode = fontAsset.atlasPopulationMode;
            try
            {
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                fontAsset.ClearFontAssetData(false);

                fontAsset.TryAddCharacters(chars, out missingOut);
                string missing = missingOut;

                fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
                EditorUtility.SetDirty(fontAsset);
                foreach (var tex in fontAsset.atlasTextures)
                    if (tex != null) EditorUtility.SetDirty(tex);
                AssetDatabase.SaveAssets();

                int atlasCount = fontAsset.atlasTextures.Count(t => t != null && t.width > 1);
                Debug.Log($"[FontSubset] 完成：字元 {before} → {fontAsset.characterTable.Count}，圖集 {atlasCount} 張 " +
                          $"({fontAsset.atlasWidth}x{fontAsset.atlasHeight})。");
                if (!string.IsNullOrEmpty(missing))
                    Debug.LogWarning($"[FontSubset] 來源字型缺字 {missing.Length} 個：{missing}");
                if (atlasCount > 1)
                    Debug.LogWarning("[FontSubset] 字集超過一張圖集，build 會多約 4MB；可考慮刪減或降低取樣點數。");
            }
            catch (System.Exception e)
            {
                fontAsset.atlasPopulationMode = oldMode;
                Debug.LogException(e);
            }
            return missingOut;
        }

        static string CollectCharacters(out int fromJson, out int fromCs)
        {
            var set = new SortedSet<char>();

            // ASCII 可列印字元
            for (char c = (char)0x20; c <= (char)0x7E; c++) set.Add(c);
            foreach (char c in CommonPunctuation) set.Add(c);

            // clues.json：整份檔案的非 ASCII 字元（含 \uXXXX 跳脫）
            var jsonSet = new HashSet<char>();
            if (File.Exists(CluesPath))
                AddNonAscii(DecodeEscapes(File.ReadAllText(CluesPath, Encoding.UTF8)), jsonSet);
            foreach (var data in Directory.Exists("Assets/_Project/Data") ? Directory.GetFiles("Assets/_Project/Data", "*.json", SearchOption.AllDirectories) : new string[0])
                AddNonAscii(DecodeEscapes(File.ReadAllText(data, Encoding.UTF8)), jsonSet);
            // 場景內序列化字串（Interactable 的 prompt／toast 等，YAML 以 \uXXXX 跳脫）
            foreach (var dir in SceneDirs)
                if (Directory.Exists(dir))
                    foreach (var scene in Directory.GetFiles(dir, "*.unity", SearchOption.AllDirectories))
                        AddNonAscii(DecodeEscapes(File.ReadAllText(scene, Encoding.UTF8)), jsonSet);
            if (!File.Exists(CluesPath))
                Debug.LogWarning("[FontSubset] 找不到 " + CluesPath);

            // Scripts/*.cs：只取字串／字元常值（排除註解）
            var csSet = new HashSet<char>();
            foreach (var file in Directory.Exists(ScriptsDir) ? Directory.GetFiles(ScriptsDir, "*.cs", SearchOption.AllDirectories) : new string[0])
            {
                string src = File.ReadAllText(file, Encoding.UTF8);
                src = Regex.Replace(src, @"/\*.*?\*/", "", RegexOptions.Singleline);
                var sb = new StringBuilder();
                foreach (var line in src.Split('\n'))
                {
                    // 去掉行註解（避開字串內的 //：只在常值以外截斷）
                    sb.Append(StripLineComment(line)).Append('\n');
                }
                foreach (Match m in StringLiteral.Matches(sb.ToString()))
                    AddNonAscii(DecodeEscapes(m.Value), csSet);
            }

            fromJson = jsonSet.Count;
            fromCs = csSet.Count;
            set.UnionWith(jsonSet);
            set.UnionWith(csSet);
            set.RemoveWhere(c => char.IsControl(c) || char.IsSurrogate(c) || c == '﻿');
            return new string(set.ToArray());
        }

        static string StripLineComment(string line)
        {
            bool inStr = false, verbatim = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inStr)
                {
                    if (!verbatim && c == '\\') { i++; continue; }
                    if (c == '"')
                    {
                        if (verbatim && i + 1 < line.Length && line[i + 1] == '"') { i++; continue; }
                        inStr = false;
                    }
                }
                else if (c == '"') { inStr = true; verbatim = i > 0 && line[i - 1] == '@'; }
                else if (c == '\'' && i + 2 < line.Length) { i += line[i + 1] == '\\' ? 3 : 2; }
                else if (c == '/' && i + 1 < line.Length && line[i + 1] == '/') return line.Substring(0, i);
            }
            return line;
        }

        static string DecodeEscapes(string s) =>
            UnicodeEscape.Replace(s, m => ((char)System.Convert.ToInt32(m.Groups[1].Value, 16)).ToString());

        static void AddNonAscii(string s, HashSet<char> target)
        {
            foreach (char c in s)
                if (c > 0x7E && !char.IsWhiteSpace(c) || c == '　') target.Add(c);
        }
    }
}
