using LastFleet.Localization;
using LastFleet.Pursuit;
using LastFleet.ShipControl;
using LastFleet.Shared.Text;
using UnityEngine;

namespace LastFleet.Hud
{
    /// <summary>
    /// Threat display at the top of the screen (GDD: HUD, threat display): the countdown until the
    /// Pronin arrive. Prototype UI in IMGUI below the supply bar; it turns red with the warning.
    /// </summary>
    public sealed class ThreatIndicator : MonoBehaviour
    {
        const float TopOffsetPixels = 44f;

        [SerializeField] PursuitClock _pursuit;
        [SerializeField] ShipCommandInput _shipInput;

        string _resolvedStatus;
        PursuitPhase _phase;

        void Awake()
        {
            if (_pursuit == null || _shipInput == null)
            {
                Debug.LogError("ThreatIndicator needs the pursuit clock and the ship command input to block clicks behind it.", this);
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
            // Refresh only on Layout: IMGUI needs the same controls in every event of a frame.
            if (Event.current.type == EventType.Layout)
            {
                ResolveStatus();
            }
            if (_resolvedStatus == null)
            {
                _shipInput.ClearBlockedScreenArea(this);
                return;
            }

            GUILayout.BeginArea(new Rect(0f, TopOffsetPixels, Screen.width, Screen.height - TopOffsetPixels));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Color previousColor = GUI.contentColor;
            GUI.contentColor = ColorOf(_phase);
            GUILayout.Label(_resolvedStatus, GUI.skin.box);
            GUI.contentColor = previousColor;

            if (Event.current.type == EventType.Repaint)
            {
                Rect box = GUILayoutUtility.GetLastRect();
                // GUI coordinates start top left, Input System screen coordinates bottom left.
                _shipInput.SetBlockedScreenArea(this, new Rect(box.x, Screen.height - TopOffsetPixels - box.yMax, box.width, box.height));
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void ResolveStatus()
        {
            PursuitCountdown countdown = _pursuit.Countdown;
            if (countdown == null)
            {
                _resolvedStatus = null;
                return;
            }

            _phase = countdown.Phase;
            _resolvedStatus = GameTextResolver.Resolve(StatusOf(countdown));
        }

        static GameText StatusOf(PursuitCountdown countdown)
        {
            string remaining = ClockFormat.MinutesSeconds(countdown.SecondsUntilArrival);
            return countdown.Phase switch
            {
                PursuitPhase.Counting => PursuitTexts.CountingStatus.Format(("remaining", remaining)),
                PursuitPhase.Warning => PursuitTexts.WarningStatus.Format(("remaining", remaining)),
                _ => PursuitTexts.ArrivedStatus
            };
        }

        static Color ColorOf(PursuitPhase phase)
        {
            return phase == PursuitPhase.Counting ? new Color(0.75f, 0.85f, 1f) : new Color(1f, 0.35f, 0.3f);
        }
    }
}
