#!/usr/bin/env python3
"""C-R2：逐項核對手冊與學習單引文；不改空白、標點或字形。"""
import argparse
from html.parser import HTMLParser
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = ("guide.md", "worksheet.md", "worksheet.html")


class VisibleText(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.parts = []
        self.hidden = 0

    def handle_starttag(self, tag, attrs):
        if tag in ("style", "script"):
            self.hidden += 1

    def handle_endtag(self, tag):
        if tag in ("style", "script"):
            self.hidden -= 1

    def handle_data(self, data):
        if not self.hidden:
            self.parts.append(data)


def visible_html(raw):
    parser = VisibleText()
    parser.feed(raw)
    parser.close()
    return "".join(parser.parts)


def quotations(text):
    """保留每層引號內容；不完整引號一律失敗。"""
    stack, items = [], []
    for pos, char in enumerate(text):
        if char == "「":
            stack.append(pos)
        elif char == "」":
            if not stack:
                raise ValueError(f"第 {text.count(chr(10), 0, pos) + 1} 行有未配對的右引號")
            start = stack.pop()
            items.append((start, text[start + 1:pos]))
    if stack:
        raise ValueError("有未配對的左引號")
    return sorted(items)


def origin(quote, source, variants):
    if quote and quote in source:
        return "正文"
    # shortcut: 異文僅採表內引號明列的完整字串，新增異文時先更新異文表。
    if quote and quote in variants:
        return "異文表（非現行正文）"
    return None


def self_test():
    assert quotations("「甲『乙』」「丙」") == [(0, "甲『乙』"), (6, "丙")]
    assert quotations("「甲「乙」」") == [(0, "甲「乙」"), (2, "乙")]
    assert origin("胡麻糝", "有胡麻糝。", set()) == "正文"
    assert origin("遂留半年", "遂停半年", {"遂留半年"}).startswith("異文表")
    assert origin("遂留", "遂停半年", {"遂留半年"}) is None
    assert origin("胡麻飯糝", "有胡麻糝。", set()) is None
    assert origin("有胡麻糝，", "有胡麻糝。", set()) is None
    assert origin("有 胡麻糝", "有胡麻糝。", set()) is None
    assert origin("", "正文", {""}) is None
    assert visible_html('<style>「假」</style><!--「假」--><p>「胡麻&#31965;」</p>') == "「胡麻糝」"
    for broken in ("「缺右", "缺左」", "「甲」「"):
        try:
            quotations(broken)
        except ValueError:
            pass
        else:
            raise AssertionError("未攔截不完整引號")
    print("PASS 自檢：正文、異文、錯字、標點、空白、空引文、HTML、巢狀及未配對引號")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true", help="只執行內建正反例檢查")
    args = parser.parse_args()
    if args.self_test:
        self_test()
        return 0
    source = (ROOT / "sources/liuruan_tw.txt").read_text(encoding="utf-8")
    variants = {q for _, q in quotations((ROOT / "sources/variants.md").read_text(encoding="utf-8"))}
    passed = failed = 0
    for name in FILES:
        path = ROOT / "docs/teacher" / name
        raw = path.read_text(encoding="utf-8")
        text = visible_html(raw) if path.suffix == ".html" else raw
        try:
            quotes = quotations(text)
        except ValueError as error:
            print(f"FAIL {name}: {error}")
            failed += 1
            continue
        if not quotes:
            print(f"FAIL {name}: 未找到引文，請檢查檔案內容")
            failed += 1
        for index, (pos, quote) in enumerate(quotes, 1):
            location = f"{name} 引文#{index} 文字行{text.count(chr(10), 0, pos) + 1}"
            found = origin(quote, source, variants)
            if found:
                passed += 1
                print(f"PASS {location} [{found}] 「{quote}」")
            else:
                failed += 1
                print(f"FAIL {location} [查無逐字出處] 「{quote}」")
    print(f"{'FAIL' if failed else 'PASS'} 總計：{passed} 項通過，{failed} 項失敗；{len(FILES)} 個檔案")
    return int(failed > 0)


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError) as error:
        print(f"FAIL {error}", file=sys.stderr)
        sys.exit(1)
