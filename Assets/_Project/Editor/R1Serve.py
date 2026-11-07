"""在專案根目錄執行：python3 Assets/_Project/Editor/R1Serve.py。"""
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class Handler(SimpleHTTPRequestHandler):
    def guess_type(self, path):
        for suffix in (".unityweb", ".br"):
            if path.endswith(suffix):
                path = path[:-len(suffix)]
                break
        if path.endswith(".wasm"):
            return "application/wasm"
        if path.endswith(".js"):
            return "application/javascript"
        return super().guess_type(path)

    def end_headers(self):
        if self.path.split("?", 1)[0].endswith((".unityweb", ".br")):
            self.send_header("Content-Encoding", "br")
        self.send_header("Cache-Control", "no-store")
        super().end_headers()


if __name__ == "__main__":
    root = Path(__file__).resolve().parents[3] / "Build/WebGL"
    if not (root / ".bundle-done").is_file():
        raise SystemExit("尚無成功的 WebGL 建置：找不到 Build/WebGL/.bundle-done")
    print("http://127.0.0.1:8765", flush=True)
    ThreadingHTTPServer(("127.0.0.1", 8765), partial(Handler, directory=str(root))).serve_forever()
