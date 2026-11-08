"""等比尺寸輸出與 PNG 灰底合成；主體與去背均由內建生圖完成。"""
from pathlib import Path
import sys
from PIL import Image, ImageOps

root = Path(__file__).resolve().parent
name, width, height = sys.argv[1:]
with Image.open(root / 'raw' / f'{name}.png') as image:
    size = (int(width), int(height))
    if name.startswith('story_'):
        # 生圖構圖以 16:9 為準，僅等比縮放並補邊，絕不裁掉故事內容。
        output = ImageOps.pad(image.convert('RGB'), size, color='#eee8da')
    else:
        image = image.convert('RGBA')
        assert image.getchannel('A').getextrema()[0] == 0, f'{name}: 原稿沒有透明背景'
        bounds = image.getchannel('A').getbbox()
        assert bounds, f'{name}: 空白圖'
        subject = image.crop(bounds)
        is_model = name.startswith('npc_') or name in ('peach_tree_v2', 'chu_tree_v2', 'drift_leaf', 'drift_rice')
        fraction = .84 if is_model else .9
        subject.thumbnail((round(size[0]*fraction), round(size[1]*fraction)), Image.Resampling.LANCZOS)
        output = Image.new('RGBA', size, '#D9D9D9' if is_model else (0, 0, 0, 0))
        output.alpha_composite(subject, ((size[0]-subject.width)//2, (size[1]-subject.height)//2))
        if is_model:
            output = output.convert('RGB')
    output.save(root / f'{name}.png')
