#!/usr/bin/env python3
"""Validate the C-R3 delivery without Unity or third-party packages."""
import argparse
import copy
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
NAMES = ('story_card', 'objectives', 'hints', 'dialogue', 'narration',
         'sentences', 'glossary_cards', 'questions_r3', 'epilogue_guide')
BANNED = ('銅瓦', '涉水', '啼鳴', '告示板')


def validate(data, source, variants):
    errors = []
    counts = {'original': 0, 'plain': 0, 'variant': 0}

    def require(ok, path, message):
        if not ok:
            errors.append(f'{path}: {message}')

    def original(value, path):
        counts['original'] += 1
        valid = isinstance(value, str) and bool(value) and (
            value in source or value in variants)
        require(valid, path, '引文未逐字見於底本或異文表')
        if valid and value not in source:
            counts['variant'] += 1

    def plain(value, path, limit=20):
        counts['plain'] += 1
        require(isinstance(value, str) and 0 < len(value) <= limit,
                path, f'文字須為非空字串且 ≤{limit} 字（含標點）')

    def walk(value, path):
        if isinstance(value, dict):
            if 'type' in value:
                require(value['type'] in ('original', 'plain'), path, '未知 type')
                if value['type'] == 'original':
                    original(value.get('text'), path + '.text')
                elif value['type'] == 'plain':
                    plain(value.get('text'), path + '.text')
            for key, item in value.items():
                if key in ('orig', 'original'):
                    original(item, path + '.' + key)
                elif key == 'plain':
                    plain(item, path + '.' + key)
                walk(item, path + '.' + key)
        elif isinstance(value, list):
            for i, item in enumerate(value):
                walk(item, f'{path}[{i}]')
        elif isinstance(value, str):
            require(not any(word in value for word in BANNED), path, '含禁用字')
            if '洞中方七日' in value or '世上已千年' in value:
                require('後人俗語' in value, path, '俗語須就地標註後人俗語')

    for name, value in data.items():
        walk(value, name)
    cards = data['story_card']['cards']
    require(3 <= len(cards) <= 4, 'story_card', '須有 3–4 張')
    for i, card in enumerate(cards):
        plain(card['text'], f'story_card.cards[{i}]', 45)
    require('你是劉晨，阮肇在你身旁' in cards[-1]['text'], 'story_card', '末卡缺角色定位')
    pages = data['epilogue_guide']['pages']
    require(3 <= len(pages) <= 4, 'epilogue_guide', '須有 3–4 頁')
    for i, page in enumerate(pages):
        plain(page['text'], f'epilogue_guide.pages[{i}]', 60)

    objectives = data['objectives']['acts']
    hints = data['hints']['acts']
    for name in ('objectives', 'hints', 'dialogue', 'narration'):
        require([a['act'] for a in data[name]['acts']] == list(range(1, 6)),
                name, '幕序須為 1–5')
    for act, hint_act in zip(objectives, hints):
        ids = [s['id'] for s in act['steps']]
        require(len(ids) == len(set(ids)), 'objectives', '步驟 id 重複')
        require(ids == [s['id'] for s in hint_act['steps']], 'hints', '提示步驟須與任務同序同名')
        for step in act['steps']:
            for key in ('goal', 'why', 'where'):
                plain(step[key], f'objectives.{act["act"]}.{step["id"]}.{key}')
            require(type(step['count']) is int and step['count'] > 0, 'objectives', 'count 須為正整數')
        for step in hint_act['steps']:
            for level in ('l1', 'l2', 'l3'):
                for device in ('desktop', 'touch'):
                    plain(step[level][device], f'hints.{act["act"]}.{step["id"]}.{level}.{device}')
            require('互動鈕' in step['l2']['touch'], 'hints', '觸控操作須提互動鈕')
    scenes = [s for a in data['dialogue']['acts'] for s in a['scenes']]
    require(len({s['id'] for s in scenes}) == len(scenes), 'dialogue', 'scene id 重複')
    for s in scenes:
        require(bool(s['trigger']) and bool(s['lines']), s['id'], '缺觸發事件或台詞')
        for line in s['lines']:
            require('type' in line and bool(line['speaker']), s['id'], '台詞缺 type 或 speaker')
    for act in data['narration']['acts']:
        require([l['type'] for l in act['lines']] == ['original', 'plain'], 'narration', '須先原文再白話')

    terms = data['glossary_cards']['terms']
    term_set = {t['term'] for t in terms}
    require(len(terms) == len(term_set) == 40, 'glossary_cards', '須為 40 條不重複詞卡')
    require(any(t['term'] == '要' and t['reading'] == 'ㄧㄠ' for t in terms), 'glossary_cards', '要讀ㄧㄠ')
    paragraphs = data['sentences']['paragraphs']
    require([p['p'] for p in paragraphs] == list(range(1, 8)), 'sentences', '須為七段')
    require([''.join(s['orig'] for s in p['sentences']) for p in paragraphs]
            == source.splitlines(), 'sentences', '逐句重組須完整等於底本七段（含刪節符號）')
    for p in paragraphs:
        for s in p['sentences']:
            require(set(s['terms']) <= term_set, 'sentences', '詞卡缺漏')
            require(all(t in s['orig'] for t in s['terms']), 'sentences', '注釋詞未出現於本句')
    questions = data['questions_r3']['clues']
    require(len(questions) == 4 and len({q['id'] for q in questions}) == 4, 'questions_r3', '須有四題獨立 id')
    for q in questions:
        require(len(q['options']) == 3 and type(q['answer']) is int and 0 <= q['answer'] < 3,
                'questions_r3', '須三選一且答案為 0 起算')
        require(len(q['wrongHints']) == 2, 'questions_r3', '須有兩條錯答提示')
    return errors, counts


def self_test(data, source, variants):
    def rejects(edit):
        changed = copy.deepcopy(data)
        edit(changed)
        assert validate(changed, source, variants)[0], '錯誤樣本未被攔截'
    rejects(lambda d: d['narration']['acts'][0]['lines'][0].update(text='原文沒有這句話'))
    rejects(lambda d: d['dialogue']['acts'][0]['scenes'][0]['lines'][0].update(text='字' * 21))
    rejects(lambda d: d['story_card']['cards'][0].update(text='字' * 46))
    rejects(lambda d: d['epilogue_guide']['pages'][0].update(text='字' * 61))
    for word in BANNED:
        rejects(lambda d, w=word: d['objectives']['acts'][0]['steps'][0].update(goal=w))
    rejects(lambda d: d['sentences']['paragraphs'][0]['sentences'][0]['terms'].append('不存在的詞'))
    rejects(lambda d: d['sentences']['paragraphs'][0]['sentences'].pop())
    rejects(lambda d: d['hints']['acts'][0]['steps'][0].update(id='wrong'))
    rejects(lambda d: d['epilogue_guide']['pages'][-1].update(text='洞中方七日，世上已千年'))
    assert '此知去人徑不遠' in variants and '此知去人徑不遠' not in source
    print('PASS self-test: 12 個錯誤樣本皆攔截；異文來源已核對')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--self-test', action='store_true', help='另測試錯誤樣本攔截')
    args = parser.parse_args()
    try:
        data = {n: json.loads((ROOT / 'docs/content/r3' / (n + '.json')).read_text(encoding='utf-8')) for n in NAMES}
        source = (ROOT / 'sources/liuruan_tw.txt').read_text(encoding='utf-8').strip()
        variants = (ROOT / 'sources/variants.md').read_text(encoding='utf-8')
        errors, counts = validate(data, source, variants)
    except (OSError, ValueError, KeyError, TypeError) as exc:
        print(f'FAIL 結構或檔案錯誤: {exc}')
        return 1
    if errors:
        print('\n'.join('FAIL ' + e for e in errors))
        return 1
    print(f'PASS 9 JSON；原文 {counts["original"]} 處（異文 {counts["variant"]} 處）；短文 {counts["plain"]} 處')
    print('PASS 五幕任務／提示對齊；七段逐字重組；40 詞卡；4 題三選一；禁用字與俗語標註')
    if args.self_test:
        self_test(data, source, variants)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
