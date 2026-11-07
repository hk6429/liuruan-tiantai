"""Run with: uv run --no-project --with pillow python staging/art/R2/check.py."""
from pathlib import Path
from PIL import Image, ImageChops, ImageStat

ROOT = Path(__file__).resolve().parent
NAMES = ('house_tongwa pavilion_luozhang peach_tree chu_tree drift_items '
         'home_house vine_cliff village_board').split()


def measure(im):
    im = im.convert('RGB')
    w, h = im.size
    boxes = [(0, 0, 32, 32), (w-32, 0, w, 32),
             (0, h-32, 32, h), (w-32, h-32, w, h)]
    means = [ImageStat.Stat(im.crop(b)).mean for b in boxes]
    spread = max(max(c[k] for c in means)-min(c[k] for c in means) for k in range(3))
    bg = tuple(round(sum(c[k] for c in means)/4) for k in range(3))
    diff = ImageChops.difference(im, Image.new('RGB', im.size, bg))
    r, g, b = diff.split()
    # shortcut: contrast segmentation includes dark contact shadows; near-grey subjects need visual review.
    mask = ImageChops.lighter(ImageChops.lighter(r, g), b).point(lambda p: 255 if p > 18 else 0)
    box = mask.getbbox()
    margins = (box[0]/w, box[1]/h, (w-box[2])/w, (h-box[3])/h) if box else (0, 0, 0, 0)
    near_grey = max(abs(c-217) for c in bg) <= 12 and max(bg)-min(bg) < 8
    passed = im.size == (1024, 1024) and spread < 8 and min(margins) >= .06 and near_grey and box is not None
    return passed, spread, bg, margins


def main():
    sample = Image.new('RGB', (1024, 1024), '#D9D9D9')
    sample.paste('#608020', (128, 128, 896, 896))
    assert measure(sample)[0]
    sample.paste('#608020', (0, 128, 50, 896))
    assert not measure(sample)[0]
    assert not measure(Image.new('RGB', (1024, 1024), '#D9D9D9'))[0]
    assert not measure(Image.new('RGB', (512, 512), '#D9D9D9'))[0]
    sample.paste('#808080', (0, 0, 32, 32))
    assert measure(sample)[1] >= 8
    failed = 0
    for name in NAMES:
        path = ROOT / (name+'.png')
        if not path.exists():
            print(f'FAIL {path.name}: missing')
            failed += 1
            continue
        with Image.open(path) as im:
            ok, spread, bg, margins = measure(im)
            ok = ok and im.format == 'PNG' and ('A' not in im.getbands() or im.getchannel('A').getextrema() == (255, 255))
            print(f'{"PASS" if ok else "FAIL"} {path.name} {im.width}x{im.height} '
                  f'corner_delta={spread:.2f} bg={bg} '
                  f'margins_LTRB={tuple(round(v*100, 2) for v in margins)}%')
            failed += not ok
    print(f'TOTAL {8-failed}/8 PASS')
    return bool(failed)


if __name__ == '__main__':
    raise SystemExit(main())
