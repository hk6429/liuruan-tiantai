"""執行：uv run --no-project --with pillow python staging/art/R3/check.py"""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageChops, ImageDraw

ROOT = Path(__file__).resolve().parent
MODELS = 'npc_woman_a npc_woman_b npc_grandson peach_tree_v2 chu_tree_v2 drift_leaf drift_rice'.split()
PORTRAITS = 'portrait_liuchen portrait_ruanzhao portrait_woman_a portrait_woman_b portrait_grandson'.split()
ICONS = 'icon_help icon_arrow icon_restart icon_skip icon_home'.split()
EXPECTED = {**{n: (1024, 1024) for n in MODELS}, **{n: (512, 512) for n in PORTRAITS}, **{f'story_{i}': (1920, 1080) for i in range(1, 4)}, **{n: (256, 256) for n in ICONS}, 'card_frame': (600, 360)}


def inspect(path, size, grey=False, transparent=False):
    assert path.is_file(), '檔案不存在'
    with Image.open(path) as im:
        assert im.format == 'PNG', '非 PNG'
        assert im.size == size, f'尺寸 {im.size} != {size}'
        rgba = im.convert('RGBA')
        if transparent:
            alpha = rgba.getchannel('A')
            assert alpha.getextrema() == (0, 255), '缺少透明或不透明像素'
            bounds = alpha.getbbox()
            assert bounds and bounds[0] > 0 and bounds[1] > 0 and bounds[2] < size[0] and bounds[3] < size[1], '主體碰邊'
            assert alpha.getpixel((0, 0)) == 0, '角落不透明'
        if grey:
            rgb = im.convert('RGB')
            bg = Image.new('RGB', size, '#D9D9D9')
            bounds = ImageChops.difference(rgb, bg).getbbox()
            assert bounds, '空白灰圖'
            margins = (bounds[0], bounds[1], size[0]-bounds[2], size[1]-bounds[3])
            assert min(margins) >= 0.07 * min(size), f'留白不足 {margins}'
            # 整個外圍 7% 必須精確同色，不能只取四角平均。
            edge = round(min(size)*.07)
            for box in [(0, 0, size[0], edge), (0, size[1]-edge, size[0], size[1]), (0, 0, edge, size[1]), (size[0]-edge, 0, size[0], size[1])]:
                assert all(lo == hi == 217 for lo, hi in rgb.crop(box).getextrema()), '背景不是純 #D9D9D9'
    return hashlib.sha256(path.read_bytes()).hexdigest()


def self_check():
    # 測試驗收程式能攔下錯尺寸與假透明，暫存也只在 R3。
    path = ROOT / '_check_fixture.png'
    try:
        Image.new('RGB', (8, 8), 'white').save(path)
        for size, alpha in [((9, 9), False), ((8, 8), True)]:
            try:
                inspect(path, size, transparent=alpha)
            except AssertionError:
                pass
            else:
                raise AssertionError('驗收程式未攔下錯誤輸入')
    finally:
        path.unlink(missing_ok=True)


if __name__ == '__main__':
    self_check()
    results, errors = [], []
    for name, size in EXPECTED.items():
        try:
            digest = inspect(ROOT / f'{name}.png', size, name in MODELS, name in PORTRAITS + ICONS + ['card_frame'])
            results.append({'file': f'{name}.png', 'size': size, 'sha256': digest})
            print(f'PASS {name}.png {size[0]}x{size[1]}')
        except (AssertionError, OSError) as exc:
            errors.append(name)
            print(f'FAIL {name}.png: {exc}')
    (ROOT / 'checks.json').write_text(json.dumps(results, ensure_ascii=False, indent=2)+'\n')
    print(f'TOTAL {len(results)}/{len(EXPECTED)} PASS')
    assert not errors, f'未通過：{errors}'
    sheet = Image.new('RGB', (1400, 6*245), '#e8e6df')
    draw = ImageDraw.Draw(sheet)
    for i, name in enumerate(EXPECTED):
        x, y = i%4*350, i//4*245
        im = Image.open(ROOT / f'{name}.png').convert('RGBA')
        im.thumbnail((330, 212))
        # 淺深雙背景呈現透明圖示與髮絲邊緣。
        tile = Image.new('RGB', (330, 212), '#f5f0e5')
        ImageDraw.Draw(tile).rectangle((165, 0, 329, 211), fill='#313d48')
        tile.paste(im, ((330-im.width)//2, (212-im.height)//2), im)
        sheet.paste(tile, (x+10, y))
        draw.text((x+10, y+218), name+'.png', fill='#252525')
    sheet.save(ROOT / 'contact.png')
