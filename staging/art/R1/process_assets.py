"""本輪指定後製；重跑使用 raw/ 原稿，validate_assets.json 保留磁碟驗證。"""
from pathlib import Path
import hashlib
import json
import shutil

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent
GROUND = ['grass', 'dirt_path', 'moss_rock', 'riverbed_pebble', 'village_stone']
FOLIAGE = ['tall_grass', 'fern', 'wildflowers', 'shrub', 'bamboo', 'hanging_vine']
UI = ['peach_full', 'peach_half', 'peach_empty', 'bark', 'skill_bark',
      'skill_water', 'skill_call', 'portrait_ruanzhao', 'portrait_frame', 'minimap_frame']


def source(relative):
    original = ROOT / 'raw' / relative
    if not original.exists():
        original.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT / relative, original)
    return Image.open(original)


def save(image, relative):
    path = ROOT / relative
    image.save(path)
    with Image.open(path) as check:
        check.load()
        assert check.size == image.size and check.mode == image.mode, relative
    assert path.stat().st_size > 1000, relative


def seam_blend(image, vertical=True, width=64):
    a = np.asarray(image.convert('RGB'), dtype=np.float32).copy()
    axes = [1, 0] if vertical else [1]
    for axis in axes:
        b = np.swapaxes(a, 0, axis)
        for i in range(width):
            weight = .5 * (1 - i / width) ** 2
            left, right = b[i].copy(), b[-1-i].copy()
            b[i] = left * (1-weight) + right * weight
            b[-1-i] = right * (1-weight) + left * weight
    return Image.fromarray(np.rint(a).clip(0, 255).astype('uint8'))


def normal(image):
    # shortcut: 亮度近似高度，不代表實測幾何；需要精準凹凸時改用專用高度圖。
    h = np.asarray(image.convert('L').filter(ImageFilter.GaussianBlur(1.2)), dtype=float) / 255
    gx = np.zeros_like(h)
    gy = np.zeros_like(h)
    for dy, weight in [(-1, 1), (0, 2), (1, 1)]:
        gx += weight * (np.roll(h, (dy, -1), (0, 1)) - np.roll(h, (dy, 1), (0, 1))) / 8
    for dx, weight in [(-1, 1), (0, 2), (1, 1)]:
        gy += weight * (np.roll(h, (-1, dx), (0, 1)) - np.roll(h, (1, dx), (0, 1))) / 8
    n = np.stack([-gx * 4, gy * 4, np.ones_like(h)], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    # UV 的 +V 向上，因此 G 與影像列座標相反；OpenGL / Unity tangent normal。
    encoded = Image.fromarray(np.rint((n * .5 + .5) * 255).astype('uint8'))
    return seam_blend(encoded, width=8)


def quilt_axis(a, overlap=128):
    length = a.shape[1] - overlap
    left, right = a[:, :overlap].astype(float), a[:, length:].astype(float)
    cost = ((left-right) ** 2).mean(axis=2)
    cost[:, :8] = cost[:, -8:] = 1e9
    parent = np.zeros(cost.shape, dtype=np.int16)
    for row in range(1, len(cost)):
        prev = np.pad(cost[row-1], (1, 1), constant_values=np.inf)
        candidates = np.stack([prev[:-2], prev[1:-1], prev[2:]])
        step = candidates.argmin(axis=0)
        parent[row] = np.arange(overlap) + step - 1
        cost[row] += candidates.min(axis=0)
    seam = np.zeros(len(cost), dtype=int)
    seam[-1] = cost[-1].argmin()
    for row in range(len(cost)-1, 0, -1):
        seam[row-1] = parent[row, seam[row]]
    result = a[:, :length].copy()
    for row, split in enumerate(seam):
        result[row, :split] = a[row, length:length+split]
    return result


def ground_tile(image):
    a = np.asarray(image.convert('RGB').resize((1152, 1152), Image.Resampling.LANCZOS))
    a = quilt_axis(a)
    a = np.swapaxes(quilt_axis(np.swapaxes(a, 0, 1)), 0, 1)
    return seam_blend(Image.fromarray(a), width=4)


def cutout(image, key=False):
    a = np.asarray(image.convert('RGBA')).copy()
    rgb = a[..., :3].astype(float)
    if key:
        # 純洋紅底不含於植物配色；去掉混色邊緣，再縮一像素消除色溢。
        spill = np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1]
        alpha = Image.fromarray(np.where(spill <= 0, 255, 0).astype('uint8'))
        a[..., 3] = np.asarray(alpha.filter(ImageFilter.MinFilter(3)))
    else:
        # 生成透明稿的低 alpha 光暈與離散雜點不留在卡片邊緣。
        a[..., 3] = np.where(a[..., 3] >= 180, 255, 0)
    a[a[..., 3] == 0, :3] = 0
    return Image.fromarray(a)


def fit(image, size, margin=16):
    bbox = image.getchannel('A').getbbox()
    assert bbox, '空白圖像'
    image = image.crop(bbox)
    image.thumbnail((size[0] - margin*2, size[1] - margin*2), Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', size)
    canvas.alpha_composite(image, ((size[0]-image.width)//2, (size[1]-image.height)//2))
    return canvas


def contact_sheet(paths, filename, columns, cell):
    sheet = Image.new('RGB', (columns*cell, ((len(paths)+columns-1)//columns)*cell), '#263c32')
    for index, path in enumerate(paths):
        image = Image.open(ROOT / path).convert('RGBA')
        image.thumbnail((cell-12, cell-30), Image.Resampling.LANCZOS)
        x, y = index % columns*cell, index // columns*cell
        sheet.paste(image, (x+(cell-image.width)//2, y+20), image)
        ImageDraw.Draw(sheet).text((x+6, y+4), Path(path).stem, fill='white')
    save(sheet, filename)


def process():
    for name in ['sky_day', 'sky_return']:
        sky = source(f'sky/{name}.png').convert('RGB').resize((2048, 1024), Image.Resampling.LANCZOS)
        sky = seam_blend(sky, vertical=False, width=96)
        a = np.asarray(sky, dtype=float).copy()
        # 等距柱狀上下極點收斂為單色，避免映射球頂／球底時產生放射接縫。
        for start, direction in [(0, 1), (1023, -1)]:
            pole = a[start].mean(axis=0)
            for distance in range(64):
                weight = (1-distance/64) ** 2
                row = start + direction*distance
                a[row] = a[row]*(1-weight) + pole*weight
        sky = Image.fromarray(np.rint(a).clip(0, 255).astype('uint8'))
        save(sky, f'sky/{name}.png')
        wrap = Image.new('RGB', (4096, 1024))
        wrap.paste(sky, (0, 0))
        wrap.paste(sky, (2048, 0))
        save(wrap, f'sky/{name}_wrapcheck.png')
    for name in GROUND:
        albedo = ground_tile(source(f'ground/{name}.png'))
        save(albedo, f'ground/{name}.png')
        tile = Image.new('RGB', (2048, 2048))
        for x in [0, 1024]:
            for y in [0, 1024]:
                tile.paste(albedo, (x, y))
        save(tile, f'ground/{name}_tilecheck.png')
        save(normal(albedo), f'ground/{name}_n.png')
    atlas = Image.new('RGBA', (2048, 2048))
    mapping = []
    for index, name in enumerate(FOLIAGE):
        plant = cutout(source(f'foliage/{name}.png'), key=name != 'tall_grass')
        plant = fit(plant, (1024, 1024), margin=48)
        save(plant, f'foliage/{name}.png')
        x = index % 2 * 1024
        top, bottom = index // 2 * 2048 // 3, (index // 2+1) * 2048 // 3
        card = fit(plant, (1024, bottom-top), margin=32)
        atlas.alpha_composite(card, (x, top))
        mapping.append({'name': name, 'pixels_top_left': [x, top, 1024, bottom-top],
                        'uv_bottom_left': [x/2048, (2048-bottom)/2048, .5, (bottom-top)/2048]})
    save(atlas, 'foliage/foliage_atlas.png')
    (ROOT / 'foliage/atlas_layout.json').write_text(json.dumps(mapping, indent=2))
    for name in UI:
        icon = cutout(source(f'ui/{name}.png'))
        # 三顆桃子保留原畫布對位；其他圖示完整等比收進 256 正方形。
        if name.startswith('peach_'):
            icon = icon.resize((256, 256), Image.Resampling.LANCZOS)
        else:
            icon = fit(icon, (256, 256), margin=12)
        save(icon, f'ui/{name}.png')
    contact_sheet([f'ui/{n}.png' for n in UI], 'ui/ui_contact.png', 5, 280)
    contact_sheet([f'foliage/{n}.png' for n in FOLIAGE], 'foliage/foliage_contact.png', 3, 400)


def validate():
    records = []
    paths = list((ROOT/'sky').glob('*.png')) + list((ROOT/'ground').glob('*.png'))
    paths += list((ROOT/'foliage').glob('*.png')) + list((ROOT/'ui').glob('*.png'))
    for path in sorted(paths):
        image = Image.open(path)
        image.load()
        a = np.asarray(image)
        relative = str(path.relative_to(ROOT))
        record = {'path': relative, 'size': list(image.size), 'mode': image.mode,
                  'bytes': path.stat().st_size, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
        assert record['bytes'] > 1000, relative
        if path.parent.name == 'ground':
            expected = (2048, 2048) if '_tilecheck' in path.stem else (1024, 1024)
            assert image.size == expected, relative
            if '_tilecheck' not in path.stem:
                record['seam_max_lr'] = int(np.abs(a[:, 0].astype(int)-a[:, -1]).max())
                record['seam_max_tb'] = int(np.abs(a[0].astype(int)-a[-1]).max())
                assert record['seam_max_lr'] == record['seam_max_tb'] == 0, relative
        if path.stem in ['sky_day', 'sky_return']:
            assert image.size == (2048, 1024), relative
            assert np.array_equal(a[:, 0], a[:, -1]), relative
            assert np.ptp(a[0].astype(int), axis=0).max() == 0, relative
            assert np.ptp(a[-1].astype(int), axis=0).max() == 0, relative
        if path.stem in UI + FOLIAGE + ['foliage_atlas']:
            assert image.mode == 'RGBA', relative
            alpha = a[..., 3]
            record['alpha_zero_fraction'] = float(np.mean(alpha == 0))
            assert (alpha == 0).any() and (alpha == 255).any(), relative
            assert not alpha[0].any() and not alpha[-1].any(), relative
            assert not alpha[:, 0].any() and not alpha[:, -1].any(), relative
            if path.stem in UI:
                assert image.size == (256, 256), relative
            if path.stem in FOLIAGE:
                assert image.size == (1024, 1024), relative
            if path.stem == 'foliage_atlas':
                assert image.size == (2048, 2048), relative
            if path.stem in ['portrait_frame', 'minimap_frame']:
                assert alpha[128, 128] == 0, relative
        records.append(record)
    assert len(records) == 38, len(records)
    assert len({r['sha256'] for r in records}) == len(records), '重複檔案'
    (ROOT / 'validate_assets.json').write_text(json.dumps(records, indent=2))
    print(f'PASS: {len(records)} PNG 檔案可解碼、尺寸與透明通道正確；地面雙向與天空水平邊界差值為 0。')


if __name__ == '__main__':
    flat = np.asarray(normal(Image.new('RGB', (32, 32), '#808080')))
    assert (flat == [128, 128, 255]).all(), '平面 Sobel 法線'
    key_test = Image.new('RGB', (32, 32), '#ff00ff')
    ImageDraw.Draw(key_test).rectangle((8, 8, 24, 24), fill='#70a030')
    keyed = np.asarray(cutout(key_test, key=True))
    assert keyed[0, 0, 3] == 0 and keyed[16, 16, 3] == 255, '去背保留前景'
    process()
    validate()
