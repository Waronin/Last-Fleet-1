using System;
using System.Collections.Generic;
using LastFleet.Localization;
using LastFleet.Shared.Text;
using LastFleet.ShipControl;
using UnityEngine;

namespace LastFleet.Inspection
{
    /// <summary>
    /// Small info window in the top right corner for the inspected target, with its interactions as
    /// buttons. Prototype UI in IMGUI; texts are resolved in the selected language once per frame. The
    /// real HUD will use the project's UI framework.
    /// </summary>
    public sealed class InspectionPanel : MonoBehaviour
    {
        const float PanelWidth = 300f;
        const float Margin = 10f;
        const int TitleFontSize = 16;

        [SerializeField] ShipCommandInput _shipInput;
        [Tooltip("Top right for inspected objects, bottom right for the ship window.")]
        [SerializeField] bool _anchorBottom;

        readonly List<GameText> _lines = new List<GameText>();
        readonly List<string> _resolvedLines = new List<string>();
        readonly List<InspectionAction> _actions = new List<InspectionAction>();
        readonly List<string> _resolvedActionLabels = new List<string>();
        readonly List<string> _resolvedDisabledReasons = new List<string>();
        string _resolvedTitle;
        string _resolvedClose;
        GUIStyle _titleStyle;
        GUIStyle _hintStyle;

        public IInspectionTarget Target { get; private set; }

        /// <summary>Feedback from an executed interaction.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_shipInput == null)
            {
                Debug.LogError("InspectionPanel needs the ship command input to block clicks behind the panel.", this);
                enabled = false;
            }
        }

        public void Show(IInspectionTarget target)
        {
            Target = target;
        }

        public void Clear()
        {
            Target = null;
            _shipInput.ClearBlockedScreenArea(this);
        }

        void OnDisable()
        {
            if (_shipInput != null)
            {
                _shipInput.ClearBlockedScreenArea(this);
            }
        }

        void OnGUI()
        {
            if (Target == null || !Target.IsValid)
            {
                if (Target != null)
                {
                    Clear();
                }
                return;
            }

            EnsureStyles();
            IInspectionTarget target = Target;
            // Refresh only on Layout: IMGUI needs the same controls in every event of a frame.
            if (Event.current.type == EventType.Layout)
            {
                target.Describe(_lines);
                target.CollectActions(_actions);
                ResolveTexts(target);
            }

            GUILayout.BeginArea(new Rect(Screen.width - PanelWidth - Margin, Margin, PanelWidth, Screen.height - Margin * 2f));
            if (_anchorBottom)
            {
                GUILayout.FlexibleSpace();
            }
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(_resolvedTitle, _titleStyle);
            foreach (string line in _resolvedLines)
            {
                GUILayout.Label(line);
            }

            GUILayout.Space(6f);
            for (int i = 0; i < _actions.Count; i++)
            {
                DrawAction(_actions[i], _resolvedActionLabels[i], _resolvedDisabledReasons[i]);
            }
            if (!_anchorBottom && GUILayout.Button(_resolvedClose))
            {
                Clear();
            }
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint && Target != null)
            {
                Rect panel = GUILayoutUtility.GetLastRect();
                // GUI coordinates start top left, Input System screen coordinates bottom left.
                Rect screen = new Rect(Screen.width - PanelWidth - Margin + panel.x, Screen.height - Margin - panel.yMax, panel.width, panel.height);
                _shipInput.SetBlockedScreenArea(this, screen);
            }
            GUILayout.EndArea();
        }

        // Resolved only on Layout, so every IMGUI event of the frame draws the same strings.
        void ResolveTexts(IInspectionTarget target)
        {
            _resolvedTitle = GameTextResolver.Resolve(target.Title);
            _resolvedClose = GameTextResolver.Resolve(InspectionTexts.Close);
            _resolvedLines.Clear();
            foreach (GameText line in _lines)
            {
                _resolvedLines.Add(GameTextResolver.Resolve(line));
            }

            _resolvedActionLabels.Clear();
            _resolvedDisabledReasons.Clear();
            foreach (InspectionAction action in _actions)
            {
                string label = GameTextResolver.Resolve(action.Label);
                _resolvedActionLabels.Add(action.ShortcutHint != null ? $"{label}   [{GameTextResolver.Resolve(action.ShortcutHint)}]" : label);
                _resolvedDisabledReasons.Add(action.IsEnabled ? null : GameTextResolver.Resolve(action.DisabledReason));
            }
        }

        void DrawAction(InspectionAction action, string label, string disabledReason)
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = action.IsEnabled;
            bool clicked = GUILayout.Button(label);
            GUI.enabled = wasEnabled;

            if (!string.IsNullOrEmpty(disabledReason))
            {
                GUILayout.Label(disabledReason, _hintStyle);
            }
            if (clicked)
            {
                GameText feedback = action.Execute();
                if (feedback != null)
                {
                    Notice?.Invoke(feedback);
                }
            }
        }

        void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }
            // The skin's font size is 0 ("default"), so an explicit size is needed for a larger title.
            _titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = TitleFontSize };
            _hintStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic };
        }
    }
}
