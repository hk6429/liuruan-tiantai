#!/usr/bin/env python3
"""移除 UnityFS 內層 LZ4，再以 Unity marker Brotli 無損重封 WebGL。"""

import argparse
import ctypes
import ctypes.util
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import struct
import subprocess
import sys
import tempfile


MARKER = "VW5pdHlXZWIgQ29tcHJlc3NlZCBDb250ZW50IChicm90bGkp"
LIMIT = 1 << 30


def require(condition, message):
    if not condition:
        raise ValueError(message)


class Reader:
    def __init__(self, data):
        self.data, self.pos = data, 0

    def take(self, size):
        require(0 <= size <= len(self.data) - self.pos, "資料範圍超出檔案")
        value = self.data[self.pos:self.pos + size]
        self.pos += size
        return value

    def unpack(self, fmt):
        values = struct.unpack(fmt, self.take(struct.calcsize(fmt)))
        return values[0] if len(values) == 1 else values

    def string(self):
        end = self.data.find(b"\0", self.pos)
        require(end >= 0, "字串缺少結尾")
        return self.take(end - self.pos + 1)[:-1]

    def align(self):
        require(not any(self.take((-self.pos) % 16)), "對齊填充不是零")


def digest(data):
    return hashlib.sha256(data).hexdigest()


def decode_block(data, size, flags):
    require(0 <= size <= LIMIT, "解壓大小超過 1 GiB 上限")
    codec = flags & 63
    if codec == 0:
        require(len(data) == size, "未壓縮區塊大小不符")
        return data
    require(codec in (2, 3), "僅支援未壓縮或 LZ4 UnityFS")
    library = ctypes.util.find_library("lz4")
    require(library is not None, "找不到系統 liblz4")
    lz4 = ctypes.CDLL(library)
    lz4.LZ4_decompress_safe.argtypes = [ctypes.c_char_p, ctypes.c_void_p,
                                        ctypes.c_int, ctypes.c_int]
    lz4.LZ4_decompress_safe.restype = ctypes.c_int
    output = ctypes.create_string_buffer(size)
    count = lz4.LZ4_decompress_safe(data, output, len(data), size)
    require(count == size, "LZ4 解壓失敗或大小不符")
    return output.raw


def web_entries(data):
    reader = Reader(data)
    require(reader.take(16) == b"UnityWebData1.0\0", "UnityWebData 標頭錯誤")
    header = reader.unpack("<I")
    require(20 <= header <= len(data), "UnityWebData 目錄範圍錯誤")
    entries, names = [], set()
    while reader.pos < header:
        require(header - reader.pos >= 12, "UnityWebData 目錄不完整")
        offset, size, length = reader.unpack("<III")
        require(length <= header - reader.pos, "檔名超出目錄範圍")
        name = reader.take(length)
        require(name and name not in names, "檔名重複或為空")
        require(header <= offset <= len(data) and size <= len(data) - offset,
                "UnityWebData 檔案範圍錯誤")
        names.add(name)
        entries.append((name, data[offset:offset + size], offset))
    require(reader.pos == header, "UnityWebData 目錄長度錯誤")
    cursor = header
    for _, payload, offset in entries:
        require(offset == cursor, "UnityWebData 有重疊、空隙或非順序檔案")
        cursor += len(payload)
    require(cursor == len(data), "UnityWebData 有未列入目錄的資料")
    return [(name, payload) for name, payload, _ in entries]


def bundle_contents(data):
    reader = Reader(data)
    require(reader.string() == b"UnityFS", "UnityFS 標頭錯誤")
    version = reader.unpack(">I")
    require(version in (6, 7, 8), "不支援此 UnityFS 版本")
    unity, revision = reader.string(), reader.string()
    size, compressed, unpacked, flags = reader.unpack(">QIII")
    require(size == len(data), "UnityFS 檔案長度錯誤")
    if version >= 7:
        reader.align()
    info_start = len(data) - compressed if flags & 128 else reader.pos
    require(reader.pos <= info_start and compressed <= len(data) - info_start,
            "UnityFS 區塊目錄範圍錯誤")
    info = decode_block(data[info_start:info_start + compressed], unpacked, flags)
    if not flags & 128:
        reader.take(compressed)
    if flags & 512:
        reader.align()
    info_reader = Reader(info)
    content_hash = info_reader.take(16)
    count = info_reader.unpack(">I")
    require(count <= (len(info) - info_reader.pos) // 10, "UnityFS 區塊數錯誤")
    blocks = [info_reader.unpack(">IIH") for _ in range(count)]
    require(sum(block[0] for block in blocks) <= LIMIT, "UnityFS 解壓總量超過 1 GiB")
    tail = info[info_reader.pos:]
    raw = []
    data_end = info_start if flags & 128 else len(data)
    for unpacked_size, compressed_size, block_flags in blocks:
        require(compressed_size <= data_end - reader.pos, "UnityFS 區塊超出資料範圍")
        raw.append(decode_block(reader.take(compressed_size), unpacked_size, block_flags))
    require(reader.pos == data_end, "UnityFS 有未解析的區塊資料")
    stream = b"".join(raw)
    nodes = Reader(tail)
    node_count = nodes.unpack(">I")
    require(node_count <= (len(tail) - nodes.pos) // 21, "UnityFS 節點數錯誤")
    hashes = {}
    for _ in range(node_count):
        offset, length, node_flags = nodes.unpack(">QQI")
        name = nodes.string()
        require(name and name.hex() not in hashes, "UnityFS 節點名稱重複或為空")
        require(offset <= len(stream) and length <= len(stream) - offset,
                "UnityFS 節點範圍錯誤")
        hashes[name.hex()] = {"offset": offset, "size": length, "flags": node_flags,
                              "sha256": digest(stream[offset:offset + length])}
    require(nodes.pos == len(tail), "UnityFS 節點目錄有未解析資料")
    return version, unity, revision, flags, content_hash, blocks, tail, stream, hashes


def repack(data):
    entries = web_entries(data)
    bundles = [(index, bundle_contents(payload)) for index, (name, payload) in enumerate(entries)
               if payload.startswith(b"UnityFS\0")]
    require(len(bundles) <= 1, "UnityFS 封裝超過一個")
    if not bundles:  # R30：主包只剩 Boot 時 data 可能不含 UnityFS，略過不算錯
        return None, {}, {}
    index, original = bundles[0]
    version, unity, revision, flags, content_hash, blocks, tail, stream, hashes = original
    outer_hashes = {name.hex(): digest(payload) for i, (name, payload) in enumerate(entries)
                    if i != index}
    if not flags & (63 | 128) and all(not block[2] & 63 for block in blocks):
        return None, hashes, outer_hashes
    info = content_hash + struct.pack(">I", len(blocks))
    info += b"".join(struct.pack(">IIH", size, size, block_flags & ~63)
                     for size, _, block_flags in blocks) + tail
    prefix = b"UnityFS\0" + struct.pack(">I", version) + unity + b"\0" + revision + b"\0"
    new_flags = flags & ~(63 | 128)
    header = prefix + struct.pack(">QIII", 0, len(info), len(info), new_flags)
    if version >= 7:
        header += bytes((-len(header)) % 16)
    bundle = header + info
    if new_flags & 512:
        bundle += bytes((-len(bundle)) % 16)
    bundle += stream
    bundle = bundle[:len(prefix)] + struct.pack(">Q", len(bundle)) + bundle[len(prefix) + 8:]
    checked = bundle_contents(bundle)
    require(checked[7:] == original[7:], "UnityFS 串流或逐節點 hash 自檢失敗")
    require(checked[3] == new_flags and checked[6] == tail and checked[4] == content_hash,
            "UnityFS 旗標或目錄自檢失敗")
    require(checked[5] == [(size, size, block_flags & ~63) for size, _, block_flags in blocks],
            "UnityFS 非壓縮旗標自檢失敗")
    entries[index] = (entries[index][0], bundle)
    header_size = 20 + sum(12 + len(name) for name, _ in entries)
    output = b"UnityWebData1.0\0" + struct.pack("<I", header_size)
    offset = header_size
    for name, payload in entries:
        output += struct.pack("<III", offset, len(payload), len(name)) + name
        offset += len(payload)
    output += b"".join(payload for _, payload in entries)
    checked_entries = web_entries(output)
    require([name for name, _ in checked_entries] == [name for name, _ in entries], "外層檔名自檢失敗")
    require({name.hex(): digest(payload) for i, (name, payload) in enumerate(checked_entries)
             if i != index} == outer_hashes, "外層逐檔 hash 自檢失敗")
    require(bundle_contents(checked_entries[index][1])[8] == hashes, "逐節點 hash 自檢失敗")
    return output, hashes, outer_hashes


def compact(site, dry_run=False):
    site = site.resolve()
    build, index = site / "Build", site / "index.html"
    sources = list(build.glob("*.data.unityweb"))
    require(len(sources) == 1 and index.is_file(), "需要完整 WebGL 目錄及唯一 data 檔")
    source = sources[0]
    require(not source.is_symlink() and not index.is_symlink(), "不接受符號連結的 data 或 index")
    index_bytes = index.read_bytes()
    index_text = index_bytes.decode("utf-8")
    require(len(re.findall(r"\bdataUrl\s*:", index_text)) == 1, "index 必須只有一個 dataUrl")
    pattern = r'''(\bdataUrl\s*:\s*buildUrl\s*\+\s*["']/)([^"']+)(["'])'''
    matches = list(re.finditer(pattern, index_text))
    require(len(matches) == 1 and matches[0][2] == source.name, "index dataUrl 與 data 檔不符")
    brotli = shutil.which("brotli")
    require(brotli is not None, "找不到 brotli CLI")
    original = source.read_bytes()
    raw = subprocess.run([brotli, "-d", "-c"], input=original, capture_output=True, check=True).stdout
    output, nodes, files = repack(raw)
    report = {"status": "skipped" if output is None else "dry-run" if dry_run else "compacted",
              "original_bytes": len(original), "unityfs_nodes": nodes, "other_files": files}
    if output is None:
        report["reason"] = "沒有 UnityFS 或已全部未壓縮，保留既有 Brotli"
        return report
    encoded = subprocess.run([brotli, "-q", "11", "-w", "24", "-C", MARKER, "-c"],
                             input=output, capture_output=True, check=True).stdout
    decoded = subprocess.run([brotli, "-d", "-C", MARKER, "-c"], input=encoded,
                             capture_output=True, check=True).stdout
    require(decoded == output, "Brotli marker 或解壓逐位元自檢失敗")
    if len(encoded) >= len(original):  # R30：data 變小後重封可能不划算，略過而不是讓 deploy 中斷
        report.update({"status": "skipped", "reason": "重封後沒有變小，保留原 build", "bytes": len(encoded)})
        return report
    filename = hashlib.md5(encoded).hexdigest() + ".data.unityweb"
    destination = build / filename
    require(destination != source and not destination.exists(), "新 data 檔名已存在")
    report.update({"bytes": len(encoded), "saved_bytes": len(original) - len(encoded),
                   "data_filename": filename, "md5": hashlib.md5(encoded).hexdigest(),
                   "sha256": digest(encoded), "decoded_bytes": len(output)})
    if dry_run:
        return report
    # shortcut: 兩檔無法一起原子置換；先留舊 data，確保 index 隨時有可用的檔案。
    require(source.read_bytes() == original and index.read_bytes() == index_bytes,
            "驗證期間 build 已改變，取消寫入")
    match = matches[0]
    new_index = (index_text[:match.start(2)] + filename + index_text[match.end(2):]).encode("utf-8")
    published = False
    with tempfile.TemporaryDirectory(prefix=".compact-", dir=site) as temporary:
        temporary = Path(temporary)
        data_temp, index_temp = temporary / "data", temporary / "index"
        index_backup = temporary / "original-index"
        data_temp.write_bytes(encoded)
        index_temp.write_bytes(new_index)
        index_backup.write_bytes(index_bytes)
        os.chmod(data_temp, source.stat().st_mode & 0o777)
        os.chmod(index_temp, index.stat().st_mode & 0o777)
        os.chmod(index_backup, index.stat().st_mode & 0o777)
        try:
            os.replace(data_temp, destination)
            os.replace(index_temp, index)
            published = True
            try:
                subprocess.run(["/bin/rm", str(source)], check=True)
            except (OSError, subprocess.CalledProcessError):
                os.replace(index_backup, index)
                published = False
                raise
        finally:
            if not published and destination.exists():
                subprocess.run(["/bin/rm", str(destination)], check=True)
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("webgl", type=Path, help="含 index.html 與 Build/ 的完整 WebGL 目錄")
    parser.add_argument("--dry-run", action="store_true", help="完整重封自檢，但不修改輸入目錄")
    args = parser.parse_args()
    try:
        print(json.dumps(compact(args.webgl, args.dry_run), ensure_ascii=False, indent=2))
    except (ValueError, OSError, subprocess.CalledProcessError) as error:
        print("重封失敗：" + str(error), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
