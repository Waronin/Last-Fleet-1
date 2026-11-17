using System;
using LastFleet.Localization;
using LastFleet.ShipControl;
using UnityEngine;

namespace LastFleet.Tutorial
{
    /// <summary>
    /// The prologue's current objective at the top middle, below the jump status, with a way out: skip the
    /// step (when the game cannot see it done, e.g. a station that cannot be searched) or end the prologue.
    /// Prototype UI in IMGUI.
    /// </summary>
    public sealed class PrologueObjectivePanel : MonoBehaviour
    {
        const float WidthPixels = 400f;
        const float TopPixels = 150f;

        [SerializeField] PrologueDirector _prologue;
        [SerializeField] ShipCommandInput _shipInput;

        // Styles are rebuilt lazily; serialized GUIStyle fields come back empty after a reload.
        [NonSerialized] GUIStyle _wrapStyle;

        void Awake()
        {
            if (_prologue == null || _shipInput == null)
            {
                Debug.LogError("PrologueObjectivePanel needs the prologue director and the ship command input.", this);
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

        void OnGUI()
        {
            TutorialSequence sequence = _prologue.Sequence;
            if (sequence == null || sequence.Current == null)
            {
                _shipInput.ClearBlockedScreenArea(this);
                return;
            }

            _wrapStyle ??= new GUIStyle(GUI.skin.label) { wordWrap = true };
            Rect area = new Rect((Screen.width - WidthPixels) * 0.5f, TopPixels, WidthPixels, Screen.height - TopPixels);
            GUILayout.BeginArea(area);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(GameTextResolver.Resolve(PrologueTexts.PanelHeader));
            GUILayout.Label(GameTextResolver.Resolve(PrologueTexts.PanelStep.Format(("step", sequence.CurrentNumber), ("count", sequence.StepCount))));
            GUILayout.Label(GameTextResolver.Resolve(sequence.Current.Objective), _wrapStyle);
            GUILayout.BeginHorizontal();
            bool skipStep = GUILayout.Button(GameTextResolver.Resolve(PrologueTexts.NextStepButton));
            bool endPrologue = GUILayout.Button(GameTextResolver.Resolve(PrologueTexts.EndPrologueButton));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {
                Rect box = GUILayoutUtility.GetLastRect();
                _shipInput.SetBlockedScreenArea(this, new Rect(area.x + box.x, Screen.height - area.y - box.yMax, box.width, box.height));
            }
            GUILayout.EndArea();

            if (skipStep)
            {
                _prologue.SkipStep();
            }
            else if (endPrologue)
            {
                _prologue.EndEarly();
            }
        }
    }
}
