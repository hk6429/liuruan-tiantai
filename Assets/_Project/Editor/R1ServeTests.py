"""不開網路連接埠，檢查本機伺服器的回應標頭。"""
import io
import runpy
import unittest
from pathlib import Path

Handler = runpy.run_path(str(Path(__file__).with_name("R1Serve.py")))["Handler"]


class ServerTests(unittest.TestCase):
    def test_brotli_and_mime_headers(self):
        for name, mime in (("game.wasm.unityweb", "application/wasm"),
                           ("game.framework.js.unityweb", "application/javascript"),
                           ("game.data.unityweb", "application/octet-stream"),
                           ("game.wasm.br", "application/wasm"),
                           ("index.html", "text/html")):
            with self.subTest(name=name):
                handler = Handler.__new__(Handler)
                handler.path = "/" + name + "?v=1"
                handler.request_version = "HTTP/1.1"
                handler._headers_buffer = []
                handler.wfile = io.BytesIO()
                self.assertEqual(handler.guess_type(name), mime)
                handler.end_headers()
                headers = handler.wfile.getvalue().decode()
                self.assertEqual("Content-Encoding: br" in headers, name != "index.html")
                self.assertIn("Cache-Control: no-store", headers)


if __name__ == "__main__":
    unittest.main()
