using System;
using LastFleet.FleetJump;
using LastFleet.Localization;
using LastFleet.ShipControl;
using LastFleet.Shared.Text;
using UnityEngine;

namespace LastFleet.Hud
{
    /// <summary>
    /// Jump status at the top of the screen (GDD: HUD, jump status): cooldown and charge of the large
    /// jump drive. A ready drive shows a "jump ready" button that asks for the star map; a running
    /// charge has a button to cancel it. Prototype UI in IMGUI below the threat display.
    /// </summary>
    public sealed class JumpStatusIndicator : MonoBehaviour
    {
        const float TopOffsetPixels = 76f;
        const float ButtonSpacingPixels = 6f;

        [SerializeField] FleetJumpService _fleetJump;
        [SerializeField] ShipCommandInput _shipInput;

        string _resolvedStatus;
        string _resolvedCancel;
        FleetJumpState _state;

        /// <summary>The player pressed "jump ready": whoever owns the views opens the star map.</summary>
        public event Action JumpMapRequested;

        void Awake()
        {
            if (_fleetJump == null || _shipInput == null)
            {
                Debug.LogError("JumpStatusIndicator needs the fleet jump service and the ship command input to block clicks behind it.", this);
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
            GUILayout.BeginHorizontal(GUI.skin.box);
            Color previousColor = GUI.contentColor;
            GUI.contentColor = ColorOf(_state);
            // Once the drive is ready the status itself is the way to the star map.
            bool mapRequested = _state == FleetJumpState.Ready ? GUILayout.Button(_resolvedStatus) : DrawLabel(_resolvedStatus);
            GUI.contentColor = previousColor;
            if (mapRequested)
            {
                JumpMapRequested?.Invoke();
            }
            if (_state == FleetJumpState.Charging)
            {
                GUILayout.Space(ButtonSpacingPixels);
                if (GUILayout.Button(_resolvedCancel))
                {
                    _fleetJump.CancelCharge();
                }
            }
            GUILayout.EndHorizontal();

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

        static bool DrawLabel(string text)
        {
            GUILayout.Label(text);
            return false;
        }

        void ResolveStatus()
        {
            FleetJumpDrive drive = _fleetJump.Drive;
            if (drive == null)
            {
                _resolvedStatus = null;
                return;
            }

            _state = drive.State;
            _resolvedCancel = GameTextResolver.Resolve(FleetJumpTexts.CancelButton);
            _resolvedStatus = GameTextResolver.Resolve(drive.State switch
            {
                FleetJumpState.CoolingDown => FleetJumpTexts.CoolingDownStatus.Format(("remaining", ClockFormat.MinutesSeconds(drive.CooldownRemainingSeconds))),
                FleetJumpState.Charging => FleetJumpTexts.ChargingStatus.Format(
                    ("system", _fleetJump.ChargingTargetName),
                    ("remaining", ClockFormat.MinutesSeconds(drive.ChargeRemainingSeconds))),
                _ => (GameText)FleetJumpTexts.ReadyStatus
            });
        }

        static Color ColorOf(FleetJumpState state)
        {
            return state switch
            {
                FleetJumpState.CoolingDown => new Color(0.7f, 0.7f, 0.75f),
                FleetJumpState.Ready => new Color(0.5f, 1f, 0.6f),
                _ => new Color(0.5f, 0.85f, 1f)
            };
        }
    }
}
