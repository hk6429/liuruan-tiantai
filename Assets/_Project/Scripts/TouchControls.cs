using UnityEngine;
using UnityEngine.EventSystems;

namespace Liuruan
{
    /// 手機／平板觸控輸入：左下搖桿移動、拖曳畫面轉視角、右下互動鍵（等同 E）。
    public static class TouchInput
    {
        /// 手機平台、瀏覽器判斷為觸控平板（hotfix1：iPadOS 回報 Macintosh 但 maxTouchPoints > 1；Android 平板），
        /// 或桌機上實際摸過螢幕（保留原本「第一次觸控就切換」）
        #if UNITY_EDITOR
        public static bool ForceTouch;
#else
        const bool ForceTouch = false;
#endif
        public static bool Enabled => ForceTouch || Application.isMobilePlatform || sawTouch || Tablet;
        static bool sawTouch;
        static int tablet = -1;
        static bool Tablet { get { if (tablet < 0) tablet = TabletJs() ? 1 : 0; return tablet == 1; } }
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern int LiuruanTouchTablet();
        static bool TabletJs() { try { return LiuruanTouchTablet() == 1; } catch { return false; } }
#else
        static bool TabletJs() => false;
#endif
        public static void Poll() { if (Input.touchCount > 0) sawTouch = true; }
        public static Vector2 Move;
        public static Vector2 Look;
        public static bool InteractHeld;
        static int downFrame = -10;
        static bool used = true;

        public static void Press() { downFrame = Time.frameCount; used = false; }

        /// 本幀或上一幀按下且尚未被取用
        public static bool ConsumeDown()
        {
            if (used || Time.frameCount - downFrame > 1) return false;
            used = true;
            return true;
        }

        public static Vector2 ConsumeLook() { var d = Look; Look = Vector2.zero; return d; }
    }

    /// 虛擬搖桿：拖曳範圍為自身半徑
    public class TouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform knob;
        RectTransform rt;

        void Awake() => rt = (RectTransform)transform;

        public void OnPointerDown(PointerEventData e) => OnDrag(e);

        public void OnDrag(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var p);
            float r = rt.rect.width * 0.5f;
            var v = Vector2.ClampMagnitude(p / r, 1f);
            TouchInput.Move = v;
            knob.anchoredPosition = v * r;
        }

        public void OnPointerUp(PointerEventData e)
        {
            TouchInput.Move = Vector2.zero;
            knob.anchoredPosition = Vector2.zero;
        }

        void OnDisable() => OnPointerUp(null);
    }

    /// 拖曳空白畫面轉視角
    public class TouchLook : MonoBehaviour, IDragHandler
    {
        public void OnDrag(PointerEventData e) => TouchInput.Look += e.delta;
    }

    /// 互動鍵：按下觸發一次，按住可持續（磨墨用）
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public void OnPointerDown(PointerEventData e) { TouchInput.InteractHeld = true; TouchInput.Press(); }
        public void OnPointerUp(PointerEventData e) => TouchInput.InteractHeld = false;
        void OnDisable() => TouchInput.InteractHeld = false;
    }
}
