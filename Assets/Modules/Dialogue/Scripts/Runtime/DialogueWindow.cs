using LastFleet.Localization;
using LastFleet.ShipControl;
using UnityEngine;

namespace LastFleet.Dialogue
{
    /// <summary>
    /// Shows the open conversation in the lower middle of the screen: speaker, line and one button per
    /// answer, or a button to continue. The game keeps running behind it; only clicks on the window are
    /// kept from reaching the ships. Answers have no keys, since the number keys select control groups.
    /// Prototype UI in IMGUI.
    /// </summary>
    public sealed class DialogueWindow : MonoBehaviour
    {
        const float WidthPixels = 520f;
        const float BottomMarginPixels = 200f;

        [SerializeField] DialogueService _dialogue;
        [SerializeField] ShipCommandInput _shipInput;

        GUIStyle _speakerStyle;
        GUIStyle _lineStyle;

        void Awake()
        {
            if (_dialogue == null || _shipInput == null)
            {
                Debug.LogError("DialogueWindow needs the dialogue service and the ship command input to block clicks behind it.", this);
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
            DialogueConversation conversation = _dialogue.Current;
            if (conversation == null || conversation.IsFinished)
            {
                _shipInput.ClearBlockedScreenArea(this);
                return;
            }

            _speakerStyle ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _lineStyle ??= new GUIStyle(GUI.skin.label) { wordWrap = true };

            DialogueNode node = conversation.Current;
            Rect area = new Rect((Screen.width - WidthPixels) * 0.5f, 0f, WidthPixels, Screen.height - BottomMarginPixels);
            GUILayout.BeginArea(area);
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(GameTextResolver.Resolve(node.Speaker), _speakerStyle);
            GUILayout.Label(GameTextResolver.Resolve(node.Line), _lineStyle);
            if (_dialogue.WaitingCount > 0)
            {
                GUILayout.Label(GameTextResolver.Resolve(DialogueTexts.WaitingLine.Format(("count", _dialogue.WaitingCount))));
            }

            int chosen = -1;
            bool continued = false;
            if (node.HasChoices)
            {
                for (int i = 0; i < node.Choices.Count; i++)
                {
                    if (GUILayout.Button(GameTextResolver.Resolve(node.Choices[i].Label)))
                    {
                        chosen = i;
                    }
                }
            }
            else
            {
                continued = GUILayout.Button(GameTextResolver.Resolve(node.NextNodeId != null ? DialogueTexts.ContinueButton : DialogueTexts.EndButton));
            }
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                Rect box = GUILayoutUtility.GetLastRect();
                // GUI coordinates start top left, Input System screen coordinates bottom left.
                _shipInput.SetBlockedScreenArea(this, new Rect(area.x + box.x, Screen.height - area.y - box.yMax, box.width, box.height));
            }
            GUILayout.EndArea();

            // Acted on after the layout is closed, since the next node may have another shape.
            if (chosen >= 0)
            {
                _dialogue.Choose(chosen);
            }
            else if (continued)
            {
                _dialogue.Continue();
            }
        }
    }
}
