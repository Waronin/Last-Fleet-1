using LastFleet.Localization;
using LastFleet.Shared.Text;
using UnityEngine;

namespace LastFleet.Hud
{
    /// <summary>
    /// Small success popups at the top of the screen, below the threat display: people rescued, a new ship,
    /// water plentiful again and other small wins. Prototype UI in IMGUI. The toasts only show; clicks pass
    /// through to the ships behind them. They run on real time, so time compression does not rush them.
    /// </summary>
    public sealed class SuccessToasts : MonoBehaviour
    {
        // Below the supply bar, threat display, jump status and the fleet module button.
        const float TopOffsetPixels = 145f;
        const float ToastWidthPixels = 460f;
        const float SpacingPixels = 4f;
        const int FontSize = 16;

        static readonly Color ToastTextColor = new Color(1f, 0.86f, 0.45f);
        static readonly Color ToastBackgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.85f);

        [SerializeField, Min(1)] int _maximumVisible = 3;
        [SerializeField, Min(0.5f)] float _displaySeconds = 4f;
        [SerializeField, Min(0f)] float _fadeSeconds = 0.4f;

        readonly GUIContent _content = new GUIContent();
        SuccessToastQueue _queue;
        GUIStyle _style;
        Texture2D _background;

        SuccessToastQueue Queue => _queue ??= new SuccessToastQueue(_maximumVisible, _displaySeconds, _fadeSeconds);

        /// <summary>Shows a success message, resolved in the current language now.</summary>
        public void Show(GameText message)
        {
            Queue.Enqueue(GameTextResolver.Resolve(message));
        }

        void Update()
        {
            Queue.Advance(Time.unscaledDeltaTime);
        }

        void OnDestroy()
        {
            if (_background != null)
            {
                Destroy(_background);
            }
        }

        void OnGUI()
        {
            if (Queue.Visible.Count == 0)
            {
                return;
            }

            _style ??= CreateStyle();

            Color previousColor = GUI.color;
            Color previousContentColor = GUI.contentColor;
            GUI.contentColor = ToastTextColor;
            float top = TopOffsetPixels;
            float width = Mathf.Min(ToastWidthPixels, Screen.width);
            foreach (SuccessToast toast in Queue.Visible)
            {
                GUIContent content = _content;
                content.text = toast.Message;
                float height = _style.CalcHeight(content, width);
                GUI.color = new Color(1f, 1f, 1f, Queue.OpacityOf(toast));
                GUI.Label(new Rect((Screen.width - width) * 0.5f, top, width, height), content, _style);
                top += height + SpacingPixels;
            }
            GUI.color = previousColor;
            GUI.contentColor = previousContentColor;
        }

        /// <summary>An opaque dark plate: the default box is too see-through over the bright zone grid.</summary>
        GUIStyle CreateStyle()
        {
            _background = new Texture2D(1, 1);
            _background.SetPixel(0, 0, ToastBackgroundColor);
            _background.Apply();
            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                fontSize = FontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                padding = new RectOffset(12, 12, 8, 8)
            };
            style.normal.background = _background;
            style.normal.textColor = Color.white;
            return style;
        }
    }
}
