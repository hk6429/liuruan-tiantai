# S 線 R1：MinimapCam、ClimbZone、DriftPickup

先讀 AGENTS.md、docs/plan.md 第三、四、六節、你上一輪的 staging/scripts/logs/S-R0.md 與 INTEGRATION.md。只能寫 staging/scripts/。不開 Unity。延續 R0 的模式：純 C# Core（不引用 UnityEngine）＋ MonoBehaviour 包裝＋測試。交件報告寫 staging/scripts/logs/S-R1.md。

1. **MinimapCam**：正交俯視攝影機跟隨玩家（不旋轉、北朝上），輸出 256² RenderTexture；圖示層（玩家箭頭、阮肇點、當前目標點）；**第一幕迷霧**：MinimapFogCore 以 32×32 格記錄走過的格子，未探索格蓋霧（輸出 Texture2D 遮罩），Act2 起取消迷霧。Core 測試：揭露半徑、座標轉格、邊界。
2. **ClimbZone**：攀藤段。玩家進入觸發區按互動 → 鎖定在藤蔓平面上，上下左右移動（速度 1.2m/s），到頂端自動翻上平台、到底自動放開；飢餓值 <1 格時攀爬速度減半。ClimbCore 測試狀態：Grounded→Attaching→Climbing→TopOut／Release。
3. **DriftPickup**：溪中漂流物沿一條 waypoint 曲線緩慢往下游漂（循環），玩家靠近 2m 顯示提示、互動撿起，觸發事件帶 id（1 蕪菁葉／2 杯／3 胡麻飯糝）；三件都撿到發 OnAllCollected。DriftCore 測試：沿曲線取樣、循環、撿起後不再出現、全收集事件只發一次。
4. 更新 INTEGRATION.md：三個元件掛哪裡、和 GameFlow 的介面（事件名稱與參數）。U 線本輪正在寫 GameFlow.cs，介面請寫成事件（C# event Action<int> 等），不要直接引用 GameFlow。
5. run_core_tests.sh 加入新測試，實際跑過全綠，輸出貼報告。
