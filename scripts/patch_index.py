import sys
p=sys.argv[1]
s=open(p).read()
if 'mobile-patch' in s or 'wuliu-mobile' in s: sys.exit(0)
s=s.replace("if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {",
 "if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent) || (navigator.maxTouchPoints > 1 && /Macintosh/.test(navigator.userAgent))) { // mobile-patch",1)
s=s.replace("        // config.devicePixelRatio = 1;","        config.devicePixelRatio = Math.min(window.devicePixelRatio, 1.5);\n        var rot = document.createElement('div');\n        rot.id = 'rotate-hint';\n        rot.textContent = '請把手機轉成橫向來玩';\n        document.body.appendChild(rot);",1)
s=s.replace("</head>","<style>#rotate-hint{display:none}@media (orientation:portrait){#rotate-hint{display:flex;position:fixed;inset:0;z-index:99;align-items:center;justify-content:center;background:#2b2116;color:#f5e8c8;font-size:22px}}</style>\n</head>",1)
assert s.count('mobile-patch')==1 and 'rotate-hint' in s
open(p,'w').write(s)
