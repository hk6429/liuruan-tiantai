#!/usr/bin/env python3
"""C-R1 聚焦檢查：從任意工作目錄執行，失敗時回傳非零結束碼。"""
import json
from pathlib import Path
import re
import sys
import unicodedata

ROOT = Path(__file__).resolve().parents[1]


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read_json(path):
    return json.loads((ROOT / path).read_text(encoding="utf-8"))


def validate_clues(data, source):
    clues = data["clues"]
    require(len(clues) == 7, "須有五幕卷軸、第三幕加題及結局，共7筆")
    require([c["id"] for c in clues] == list(range(7)), "id 須依序為0–6且唯一")
    require([c["act"] for c in clues] == [1, 2, 3, 3, 4, 5, 5], "幕次分配錯誤")
    for c in clues:
        label = f"id={c['id']}"
        require(type(c["act"]) is int, f"{label} act 必須是整數")
        for field in ("title", "original", "plain", "question", "feedback"):
            require(isinstance(c.get(field), str) and c[field].strip(), f"{label} {field} 缺漏")
        require(c["original"] in source, f"{label} original 不是正文嚴格子字串")
        options = c["options"]
        require(isinstance(options, list) and len(options) == 4, f"{label} 須有4選項")
        require(all(isinstance(o, str) and o.strip() for o in options), f"{label} 有空選項")
        require(len(set(options)) == 4, f"{label} 有重複選項")
        require(type(c["answer"]) is int and 0 <= c["answer"] < 4, f"{label} answer 須為0–3整數")
        hints = c["wrongHints"]
        require(isinstance(hints, list) and len(hints) == 2 and all(isinstance(h, str) and h.strip() for h in hints), f"{label} 須有2條提示")
        require(all(word not in c["original"] for word in ("各就一帳宿", "女往就之", "宿緣所牽")), f"{label} 洩漏刪節段")
        require(all(type(i) is int and 0 <= i < c["id"] for i in c.get("requires", [])), f"{label} 前置依賴無效或成環")
    ending = [c for c in clues if c.get("isEnding")]
    require(len(ending) == 1 and ending[0]["id"] == 6, "唯一結局須為id=6")
    require(ending[0]["original"] == source.rstrip("\n"), "結局不是刪節整理本全文")
    require(ending[0]["original"].count("……") == 1, "結局刪節符號須恰有1處")
    require("infer" not in data, "第三幕加題不應另放結局infer")


def check_source(source):
    # 電子見證保留工具連結標記，先還原標記中的本文。
    witness = (ROOT / "sources/evidence/yulan.txt").read_text(encoding="utf-8").strip()
    witness = re.sub(r"【\d+†([^】]+)】", r"\1", witness).removeprefix("《幽明錄》曰：")
    start = witness.index("至暮，令各就一帳宿")
    end = witness.index("遂留半年", start)
    witness = witness[:start] + witness[end:]
    for old, new in (("便敕雲", "便敕云"), ("交幷", "交並"), ("全异", "全異"), ("遂留半年", "遂停半年")):
        witness = witness.replace(old, new)
    def letters(text):
        return "".join(c for c in text if not c.isspace() and not unicodedata.category(c).startswith("P"))
    require(letters(source) == letters(witness), "正文與御覽證據不符（已扣除4處校改、刪節、標點）")
    require(len(source.splitlines()) == 7, "正文須一段一行，共7段")
    require(not re.search(r"[A-Za-z0-9,.:;!?]", source), "正文含非全形標點或拉丁文字")


def check_support(source):
    compare = read_json("Assets/_Project/Data/compare.json")["rows"]
    require([r["aspect"] for r in compare] == ["入口", "異境樣貌", "居民", "停留", "離開方式", "回去後", "能否重返", "作者寓意"], "比較須有指定8個面向")
    taohua = (ROOT / "sources/evidence/taohuayuan.txt").read_text(encoding="utf-8")
    doc = (ROOT / "docs/content/compare_taohuayuan.md").read_text(encoding="utf-8")
    for row in compare:
        require(f"| {row['aspect']} | {row['liuruan']} | {row['taohua']} |" in doc, "比較文件與JSON不一致")
        for field, original in (("liuruan", source), ("taohua", taohua)):
            quotes = re.findall(r"「([^」]+)」", row[field])
            require(quotes and all(q in original for q in quotes), f"比較{row['aspect']}/{field}引文不符")
    acts = read_json("Assets/_Project/Data/narration.json")["acts"]
    require([a["act"] for a in acts] == list(range(1, 6)), "旁白須含五幕且不重複")
    for a in acts:
        require(1 <= len(a["sentences"]) <= 3, f"旁白第{a['act']}幕超過3句")
        for sentence in a["sentences"]:
            require(isinstance(sentence, str) and 1 <= len(sentence) <= 25, "旁白單句超過25字")
            require(sentence.endswith("。") and sentence.count("。") == 1, "旁白元素須為單句")
            require(not any(c in sentence for c in "「」『』"), "旁白不可加入人物台詞")
    for filename in ("acts.md", "glossary.md"):
        text = (ROOT / "docs/content" / filename).read_text(encoding="utf-8")
        for line in text.splitlines():
            if line.startswith("> "):
                require(line[2:] in source, "五幕引文不是正文子字串")
            if filename == "glossary.md" and line.startswith("| ") and "「" in line:
                require(all(q in source for q in re.findall(r"「([^」]+)」", line.split("|")[-2])), "詞注例句不是正文子字串")


def main():
    source = (ROOT / "sources/liuruan_tw.txt").read_text(encoding="utf-8")
    check_source(source)
    print("PASS 底本：7段；與御覽證據字序一致（4處校改＋指定刪節）")
    validate_clues(read_json("Assets/_Project/Data/clues.json"), source)
    print("PASS clues.json：JSON解析、7題格式、act、4選項、答案索引、2提示、依賴")
    print("PASS original：7/7 嚴格連續子字串（不去空白、不去標點）")
    print("PASS 結局：唯一isEnding、刪節全文、1處……、未含留宿段")
    check_support(source)
    print("PASS compare.json：8列；每格原文引句相符；Markdown與JSON一致")
    print("PASS narration.json：5幕、每幕≤3句、每句含標點≤25字")
    print("PASS acts.md／glossary.md：原文與例句逐字相符")
    invalid = read_json("Assets/_Project/Data/clues.json")
    invalid["clues"][0]["original"] = source + "非原文"
    try:
        validate_clues(invalid, source)
    except ValueError:
        print("PASS 檢查器自檢：成功攔截不屬於正文的original")
    else:
        raise ValueError("檢查器自檢失敗")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, KeyError, TypeError, OSError) as error:
        print(f"FAIL {error}", file=sys.stderr)
        sys.exit(1)
