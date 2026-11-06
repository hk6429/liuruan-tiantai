import os, sys
d = sys.argv[1]
types = {'.wasm.unityweb': 'application/wasm', '.framework.js.unityweb': 'application/javascript', '.data.unityweb': 'application/octet-stream'}
out = ["/index.html\n  Cache-Control: no-cache\n"]
for f in sorted(os.listdir(os.path.join(d, 'Build'))):
    p = '/Build/' + f
    lines = ["  Cache-Control: public, max-age=31536000, immutable"]
    if f.endswith('.unityweb'):
        lines.insert(0, "  Content-Encoding: br")
        ct = next((v for k, v in types.items() if f.endswith(k)), 'application/octet-stream')
        lines.insert(1, "  Content-Type: " + ct)
    out.append(p + "\n" + "\n".join(lines) + "\n")
open(os.path.join(d, '_headers'), 'w').write("".join(out))
print("".join(out))
