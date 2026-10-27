using System;
using System.Collections.Generic;
using LastFleet.ShipControl;
using UnityEngine;

namespace LastFleet.Inspection
{
    /// <summary>
    /// Small info window in the top right corner for the inspected target, with its interactions as
    /// buttons. Prototype UI in IMGUI with English text; the real HUD will use the project's UI
    /// framework and Unity Localization.
    /// </summary>
    public sealed class InspectionPanel : MonoBehaviour
    {
        const float PanelWidth = 300f;
        const float Margin = 10f;
        const int TitleFontSize = 16;

        [SerializeField] ShipCommandInput _shipInput;

        readonly List<string> _lines = new List<string>();
        readonly List<InspectionAction> _actions = new List<InspectionAction>();
        GUIStyle _titleStyle;
        GUIStyle _hintStyle;

        public IInspectionTarget Target { get; private set; }

        /// <summary>Feedback from an executed interaction.</summary>
        public event Action<string> Notice;

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
            }

            GUILayout.BeginArea(new Rect(Screen.width - PanelWidth - Margin, Margin, PanelWidth, Screen.height - Margin * 2f));
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(target.Title, _titleStyle);
            foreach (string line in _lines)
            {
                GUILayout.Label(line);
            }

            GUILayout.Space(6f);
            foreach (InspectionAction action in _actions)
            {
                DrawAction(action);
            }
            if (GUILayout.Button("Close"))
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

        void DrawAction(InspectionAction action)
        {
            string label = action.ShortcutHint != null ? $"{action.Label}   [{action.ShortcutHint}]" : action.Label;
            bool wasEnabled = GUI.enabled;
            GUI.enabled = action.IsEnabled;
            bool clicked = GUILayout.Button(label);
            GUI.enabled = wasEnabled;

            if (!action.IsEnabled && !string.IsNullOrEmpty(action.DisabledReason))
            {
                GUILayout.Label(action.DisabledReason, _hintStyle);
            }
            if (clicked)
            {
                string feedback = action.Execute();
                if (!string.IsNullOrEmpty(feedback))
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
