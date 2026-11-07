# S 線 R2：SeasonSwap、EpilogueDirector、CharacterBob

先讀 AGENTS.md、docs/plan.md、staging/scripts/INTEGRATION.md、你上一輪的 staging/scripts/logs/S-R1.md、Assets/_Project/Data/clues.json、compare.json、narration.json（只讀）。也讀 Assets/_Project/Scripts/GameFlow.cs 與 HudUI.cs（只讀，了解 U 線現有介面；不准改）。只能寫 staging/scripts/。不開 Unity。延續純 C# Core＋MonoBehaviour 包裝＋測試的模式。交件報告寫 staging/scripts/logs/S-R2.md。

1. **SeasonSwap**（第四幕「停半年」與第五幕「歸鄉換景」）：
   - SeasonCore：季節序列 春→夏→秋→冬→春，給一個 0–1 的進度回傳目前季節與兩季間的混合權重；Act4 的「停半年」演出用 6 秒跑完一輪（春回春），最後停在春並發 `OnBirdsSing` 一次（對應「春時百鳥啼鳴」）。
   - SeasonSwap 元件：Inspector 掛四組季節顏色（樹冠色、地面色、霧色、光色）與兩組場景根物件 `immortalVillageRoot`／`homeVillageRoot`；`PlayHalfYear()` 跑季節；`SwapToHome()` 關仙村、開歸鄉村、天空改用 sky_return 材質（欄位給 U 指定）、光色轉灰冷。不 Find 字串，全部用欄位。
   - 測試：季節邊界、權重連續、鳥鳴只發一次、重複呼叫冪等。
2. **EpilogueDirector**（結局）：讀 clues.json（isEnding 那條為全文）、compare.json，產出結局頁的資料模型（純 C#）：①全文分段 ②推論題「為什麼找到的是七世孫？」三選一（題目、選項、解析只能引用 sources/liuruan_tw.txt 原文與 docs/content/acts.md 已有的白話；不准自編情節） ③桃花源記比較表列資料 ④成績卡（剝皮數、漂流物、閱讀卡答對率、總時間）。MonoBehaviour 包裝只發事件與提供資料，不建 UI。測試：比較表列數與 compare.json 一致、成績計算、題目答案索引正確。
3. **CharacterBob**：人物模型在綁骨前的程序化動畫。依父物件水平速度做上下彈跳（步頻隨速度）、左右微擺、前傾；靜止時呼吸縮放（±1.5%）。參數可調，速度由外部餵（`SetSpeed(float)`）或自動量測 transform 位移。Core 測試：速度 0 時無彈跳、步頻與速度單調、無 NaN。
4. 更新 INTEGRATION.md 加三個元件的掛法與事件。run_core_tests.sh 加新測試，實際跑全綠，輸出貼報告。
