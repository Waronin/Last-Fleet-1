using System;
using LastFleet.Localization;
using LastFleet.ShipControl;
using LastFleet.Shared.Text;
using UnityEngine;

namespace LastFleet.RunEnd
{
    /// <summary>
    /// The end screen of a run: won or lost, why, and what is left of the fleet. It covers the whole screen
    /// and blocks clicks to the ships; the only way on is a new run. Whoever owns the run shows it, stops
    /// the game behind it and starts the new run. Prototype UI in IMGUI.
    /// </summary>
    public sealed class RunEndWindow : MonoBehaviour
    {
        const float WidthPixels = 460f;

        /// <summary>Lower depth draws on top; the other panels keep the default of 0.</summary>
        const int BackdropDepth = -100;

        static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.85f);

        [SerializeField] ShipCommandInput _shipInput;

        // Not serialized: Unity would otherwise restore empty default styles (black, size 0) after a reload,
        // and the lazy creation below would keep them.
        [NonSerialized] GUIStyle _titleStyle;
        [NonSerialized] GUIStyle _textStyle;
        [NonSerialized] GUIStyle _backdropStyle;
        Texture2D _backdropTexture;

        /// <summary>The summary shown, or null while the run goes on.</summary>
        public RunSummary Summary { get; private set; }

        public bool IsOpen => Summary != null;

        /// <summary>The player asked for a new run.</summary>
        public event Action NewRunRequested;

        void Awake()
        {
            if (_shipInput == null)
            {
                Debug.LogError("RunEndWindow needs the ship command input to block clicks behind it.", this);
                enabled = false;
            }
        }

        void OnDisable()
        {
            if (_shipInput != null)
            {
                _shipInput.ClearBlockedScreenArea(this);
            }
        }

        void OnDestroy()
        {
            if (_backdropTexture != null)
            {
                Destroy(_backdropTexture);
            }
        }

        GUIStyle BackdropStyle()
        {
            if (_backdropStyle == null)
            {
                _backdropTexture = new Texture2D(1, 1);
                _backdropTexture.SetPixel(0, 0, BackdropColor);
                _backdropTexture.Apply();
                _backdropStyle = new GUIStyle();
                _backdropStyle.normal.background = _backdropTexture;
            }
            return _backdropStyle;
        }

        public void Show(RunSummary summary)
        {
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        }

        public void Close()
        {
            Summary = null;
            _shipInput.ClearBlockedScreenArea(this);
        }

        void OnGUI()
        {
            if (Summary == null)
            {
                return;
            }
            _shipInput.SetBlockedScreenArea(this, new Rect(0f, 0f, Screen.width, Screen.height));
            _titleStyle ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 22, alignment = TextAnchor.MiddleCenter };
            _textStyle ??= new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.MiddleCenter };

            // Drawn above the other IMGUI panels, on a dark veil so the HUD behind it does not distract.
            GUI.depth = BackdropDepth;
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, BackdropStyle());
            GUILayout.BeginArea(new Rect((Screen.width - WidthPixels) * 0.5f, 0f, WidthPixels, Screen.height));
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(GameTextResolver.Resolve(RunEndTexts.Titles.For(Summary.Outcome)), _titleStyle);
            if (Summary.Reason != null)
            {
                GUILayout.Label(GameTextResolver.Resolve(Summary.Reason), _textStyle);
            }
            GUILayout.Space(8f);
            GUILayout.Label(GameTextResolver.Resolve(RunEndTexts.People.Format(("people", Summary.People))), _textStyle);
            GUILayout.Label(GameTextResolver.Resolve(RunEndTexts.Ships.Format(("ships", Summary.Ships))), _textStyle);
            GUILayout.Label(GameTextResolver.Resolve(RunEndTexts.Time.Format(("time", ClockFormat.MinutesSeconds(Summary.ElapsedSeconds)))), _textStyle);
            GUILayout.Space(8f);
            bool newRun = GUILayout.Button(GameTextResolver.Resolve(RunEndTexts.NewRunButton));
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();

            if (newRun)
            {
                NewRunRequested?.Invoke();
            }
        }
    }
}
