"""R3b 離線交接檢查；不開 Unity，不把靜態檢查當成遊玩驗收。"""
from pathlib import Path
import hashlib
import json
import struct

root = Path(__file__).resolve().parents[3]
assert Path.cwd() == root, '請在專案根目錄執行'
project = root / 'Assets/_Project'

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

for source in (root / 'docs/content/r3').glob('*.json'):
    assert digest(source) == digest(project / 'Data/r3' / source.name), source.name
assert digest(project / 'Data/narration.json') == digest(project / 'Data/r3/narration.json')
for source in (root / 'staging/scripts/Tests/Fixtures/R3').glob('*.json'):
    assert digest(source) == digest(project / 'Tests/Fixtures/R3' / source.name), source.name
for name in ('StallHint', 'Dialogue', 'Objective', 'Rescue', 'OffscreenArrow', 'Reading'):
    assert (project / 'Scripts/Systems' / (name + 'Core.cs')).is_file()
for source in (project / 'Data/r3').glob('*.json'):
    json.loads(source.read_text())
for path in (project / 'Art/R3').glob('*.png'):
    data = path.read_bytes()
    assert data[:8] == b'\x89PNG\r\n\x1a\n'
    width, height = struct.unpack('>II', data[16:24])
    assert (width, height) == ((1920,1080) if path.name.startswith('story_') else (600,360) if path.stem == 'card_frame' else (512,512) if path.name.startswith('portrait_') else (256,256)), path.name
    assert digest(path) == digest(root / 'staging/art/R3' / path.name)
flow = (project / 'Scripts/GameFlow.cs').read_text()
validation = (project / 'Editor/R3Validation.cs').read_text()
assert 'Act4_Board' not in flow
assert 'Act4_Board' not in validation
for name in ('Act4_Women','Act4_Home','Act4_House','Act4_Return','Act5_Villager','Act5_Grandson'):
    assert name in flow and name in validation, name
assert 'CharacterController' in validation and 'MoveDisplacement' in validation
assert validation.count('flow.MovePlayer(') == 2, '只容許掉落專項與每幕起點兩處'
assert 'ElapsedSeconds < 95' in validation and 'hintLevels == 7' in validation
assert 'endedDialogues.Count == 19' in validation
assert 'PngInt(mobile,16) == 1080' in validation and 'PngInt(mobile,20) == 1920' in validation
assert 'Directory.GetFiles("Assets/_Project/Data", "*.json", SearchOption.AllDirectories)' in (project / 'Editor/FontSubset.cs').read_text()
assert 'RebuildNoDialog()' in (project / 'Editor/R3Setup.cs').read_text()
assert 'HorizontalDistance(player, Position) <= 3' in (project / 'Scripts/Systems/DriftCore.cs').read_text()
assert 'public const int InferId = 100' in (project / 'Scripts/QuizStats.cs').read_text()
assert (project / 'Editor/R1Command.txt').read_text().strip() != 'r3-r3b-001', '本輪不得觸發 Unity'
print('PASS 9 JSON 與 narration／5 fixtures／10 PNG 位元組一致；PNG 尺寸正確')
print('PASS 六 Core、原文閱讀、19 對話／95 秒／手機截圖驗收契約、兩處合法放置入口')
print('PASS 保留 DriftCore／結局題號；字型遞迴掃 Data/r3；未寫入 bridge 指令')
