#!/usr/bin/env python3
"""更新 WASM 資源網址並要求重新驗證，避免沿用舊 MIME／失敗回應快取。"""
from pathlib import Path
import hashlib
import sys


def prepare(root):
    root = Path(root)
    files = list((root / 'Build').glob('*.wasm.unityweb'))
    if len(files) != 1:
        raise ValueError('需要唯一 WASM 檔案')
    wasm = files[0]
    name = wasm.name if wasm.name.startswith('delivery2-') else 'delivery2-' + wasm.name
    dest = wasm.with_name(name)
    html_path, headers_path = root / 'index.html', root / '_headers'
    html, headers = html_path.read_text(), headers_path.read_text()
    if html.count(wasm.name) != 1 or headers.count('/Build/' + wasm.name + '\n') != 1:
        raise ValueError('WASM 必須在首頁與標頭各引用一次')
    lines = headers.splitlines()
    start = lines.index('/Build/' + wasm.name)
    end = next((i for i in range(start + 1, len(lines)) if lines[i].startswith('/')), len(lines))
    block = lines[start:end]
    if '  Content-Type: application/wasm' not in block or '  Content-Encoding: br' not in block:
        raise ValueError('WASM 標頭必須有 application/wasm 與 br')
    cache = [i for i in range(start + 1, end) if lines[i].strip().startswith('Cache-Control:')]
    if len(cache) != 1 or (dest != wasm and dest.exists()):
        raise ValueError('WASM 快取設定或目標檔名有衝突')
    lines[start] = '/Build/' + name
    lines[cache[0]] = '  Cache-Control: no-cache'
    if dest != wasm:
        wasm.rename(dest)
    html_path.write_text(html.replace(wasm.name, name))
    headers_path.write_text('\n'.join(lines) + '\n')
    return {'wasm': name, 'bytes': dest.stat().st_size, 'md5': hashlib.md5(dest.read_bytes()).hexdigest()}


if __name__ == '__main__':
    print(prepare(sys.argv[1]))
