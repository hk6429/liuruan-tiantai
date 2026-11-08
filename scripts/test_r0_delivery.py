"""R0 搬移檢查與部署補丁測試；不啟動 Unity、不連網。"""
from pathlib import Path
import hashlib
import importlib.util
import json
import re
import subprocess
import sys
import tempfile
import unittest

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent.parent


def module(name):
    spec = importlib.util.spec_from_file_location(name, ROOT / 'scripts' / (name + '.py'))
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


class DeliveryTests(unittest.TestCase):
    def test_asset_guids_and_migration(self):
        guids = []
        for p in (ROOT / 'Assets').rglob('*'):
            if p.suffix == '.meta':
                guids.append(re.search(r'^guid: (\w+)$', p.read_text(), re.M)[1])
                continue
            self.assertTrue(p.with_name(p.name + '.meta').exists(), str(p))
            if p.is_file():
                try:
                    self.assertNotRegex(p.read_text().lower(), r'liu(?:lao){2}', str(p))
                except UnicodeDecodeError:
                    pass
        self.assertEqual(len(guids), len(set(guids)), 'Unity GUID 重複')
        # R3 起 Systems/EpilogueDirector.cs 為本專案自寫（Liuruan 命名空間，非劉姥姥複製），自禁名表移除
        forbidden = {'EggMinigame.cs', 'LaughMatch.cs', 'BanquetGate.cs', 'PortraitReadingLayout.cs'}
        for p in (ROOT / 'Assets/_Project/Scripts').rglob('*'):
            self.assertNotIn(p.name, forbidden)
            self.assertNotIn(p.name, {'Quest', 'Performances'})
            self.assertFalse(p.name.startswith('Hotfix1'))
        self.assertTrue(json.loads((ROOT / 'Packages/manifest.json').read_text())['dependencies'])
        settings = (ROOT / 'ProjectSettings/ProjectSettings.asset').read_text()
        # Unity 重新序列化後會寫成 \uXXXX 轉義，兩種寫法都接受
        unescaped = re.sub(r'\\u([0-9A-Fa-f]{4})', lambda m: chr(int(m[1], 16)), settings)
        self.assertIn('productName: "天台山・流杯記"', unescaped)
        self.assertEqual(settings.count('tw.naicheng.liuruan'), 3)

    def test_html_patches_are_idempotent(self):
        with tempfile.TemporaryDirectory() as d:
            html = Path(d) / 'index.html'
            html.write_text((ROOT / 'Assets/WebGLTemplates/Liuruan/index.html').read_text())
            subprocess.run([sys.executable, str(ROOT / 'scripts/patch_index.py'), str(html)], check=True)
            for name in ['patch_mobile_entry', 'patch_loading_recovery']:
                patch = module(name).patch
                self.assertTrue(patch(html))
                after = html.read_bytes()
                self.assertFalse(patch(html))
                self.assertEqual(html.read_bytes(), after)
            self.assertIn('https://liuruan-tiantai.pages.dev/', html.read_text())
            self.assertNotRegex(html.read_text().lower(), r'liu(?:lao){2}')

    def test_wasm_delivery_preserves_bytes_and_rejects_bad_headers(self):
        with tempfile.TemporaryDirectory() as d:
            root = Path(d)
            (root / 'Build').mkdir()
            content = b'R0 wasm fixture'
            (root / 'Build/test.wasm.unityweb').write_bytes(content)
            (root / 'index.html').write_text('test.wasm.unityweb')
            headers = root / '_headers'
            headers.write_text('/Build/test.wasm.unityweb\n  Content-Type: text/html\n')
            prepare = module('prepare_wasm_delivery').prepare
            with self.assertRaises(ValueError):
                prepare(root)
            self.assertEqual((root / 'Build/test.wasm.unityweb').read_bytes(), content)
            headers.write_text('/Build/test.wasm.unityweb\n  Content-Type: application/wasm\n  Content-Encoding: br\n  Cache-Control: immutable\n')
            result = prepare(root)
            self.assertEqual(result['md5'], hashlib.md5(content).hexdigest())
            self.assertEqual(prepare(root), result)
            self.assertIn('Cache-Control: no-cache', headers.read_text())


if __name__ == '__main__':
    unittest.main()
