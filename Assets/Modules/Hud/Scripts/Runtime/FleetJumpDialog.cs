using System.Collections.Generic;
using LastFleet.FleetJump;
using LastFleet.Localization;
using LastFleet.ShipControl;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Hud
{
    /// <summary>
    /// Warning before a fleet jump (GDD: the player is warned before the jump and must confirm it):
    /// what each capital ship pays, who stays behind for lack of fuel and which small ships are not in
    /// a hangar. Enter or the button confirms and starts the charge; Esc or the other button cancels.
    /// Prototype UI in IMGUI in the middle of the screen; the plan is refreshed while it is open.
    /// </summary>
    public sealed class FleetJumpDialog : MonoBehaviour
    {
        const float WidthPixels = 460f;
        const int MaximumListedSmallShips = 8;

        [SerializeField] FleetJumpService _fleetJump;
        [SerializeField] ShipCommandInput _shipInput;

        readonly List<string> _lines = new List<string>();
        int _targetSystemIndex = -1;
        bool _canJump;
        string _title;
        string _confirm;
        string _cancel;

        public bool IsOpen => _targetSystemIndex >= 0;

        void Awake()
        {
            if (_fleetJump == null || _shipInput == null)
            {
                Debug.LogError("FleetJumpDialog needs the fleet jump service and the ship command input to block clicks behind it.", this);
                enabled = false;
            }
        }

        void OnDisable()
        {
            Close();
        }

        public void Open(int targetSystemIndex)
        {
            _targetSystemIndex = targetSystemIndex;
        }

        public void Close()
        {
            _targetSystemIndex = -1;
            if (_shipInput != null)
            {
                _shipInput.ClearBlockedScreenArea(this);
            }
        }

        void OnGUI()
        {
            if (!IsOpen)
            {
                return;
            }
            if (Event.current.type == EventType.Layout)
            {
                Resolve(_fleetJump.Plan(_targetSystemIndex));
            }
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    Event.current.Use();
                    Close();
                    return;
                }
                if ((Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) && _canJump)
                {
                    Event.current.Use();
                    Confirm();
                    return;
                }
            }

            Rect area = new Rect((Screen.width - WidthPixels) * 0.5f, Screen.height * 0.25f, WidthPixels, Screen.height * 0.6f);
            GUILayout.BeginArea(area);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(_title);
            foreach (string line in _lines)
            {
                GUILayout.Label(line);
            }
            GUILayout.BeginHorizontal();
            GUI.enabled = _canJump;
            bool confirmed = GUILayout.Button(_confirm);
            GUI.enabled = true;
            bool cancelled = GUILayout.Button(_cancel);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                Rect box = GUILayoutUtility.GetLastRect();
                // GUI coordinates start top left, Input System screen coordinates bottom left.
                _shipInput.SetBlockedScreenArea(this, new Rect(area.x + box.x, Screen.height - area.y - box.yMax, box.width, box.height));
            }
            GUILayout.EndArea();

            if (confirmed)
            {
                Confirm();
            }
            else if (cancelled)
            {
                Close();
            }
        }

        void Confirm()
        {
            int target = _targetSystemIndex;
            Close();
            _fleetJump.TryStartCharge(target);
        }

        void Resolve(FleetJumpPlan plan)
        {
            _canJump = plan.CanJump;
            _title = GameTextResolver.Resolve(FleetJumpTexts.DialogTitle.Format(("system", plan.TargetName), ("distance", Mathf.RoundToInt(plan.DistanceLightYears))));
            _confirm = GameTextResolver.Resolve(FleetJumpTexts.ConfirmButton);
            _cancel = GameTextResolver.Resolve(FleetJumpTexts.CancelButton);
            _lines.Clear();
            foreach (FleetJumpPassage passage in plan.Jumping)
            {
                _lines.Add(GameTextResolver.Resolve(FleetJumpTexts.DialogJumpingLine.Format(("ship", passage.Ship.DisplayName),
                    ("cost", Mathf.CeilToInt(passage.FuelCost)), ("left", Mathf.FloorToInt(passage.FuelAfterJump)), ("jumps", passage.FullRangeJumpsLeftAfter))));
            }
            foreach (FleetJumpPassage passage in plan.OutOfFuel)
            {
                _lines.Add(GameTextResolver.Resolve(FleetJumpTexts.DialogOutOfFuelLine.Format(("ship", passage.Ship.DisplayName),
                    ("cost", Mathf.CeilToInt(passage.FuelCost)), ("left", Mathf.FloorToInt(passage.Ship.Fuel.Tank.Amount)))));
            }
            if (!plan.CanJump)
            {
                _lines.Add(GameTextResolver.Resolve(FleetJumpTexts.DialogNobodyCanJump));
            }

            if (plan.SmallShipsLeftBehind.Count == 0)
            {
                _lines.Add(GameTextResolver.Resolve(FleetJumpTexts.DialogNobodyLeftBehind));
                return;
            }
            _lines.Add(GameTextResolver.Resolve(FleetJumpTexts.DialogSmallShipsHeader.Format(("count", plan.SmallShipsLeftBehind.Count))));
            for (int i = 0; i < plan.SmallShipsLeftBehind.Count && i < MaximumListedSmallShips; i++)
            {
                _lines.Add("   " + GameTextResolver.Resolve(plan.SmallShipsLeftBehind[i].DisplayName));
            }
            if (plan.SmallShipsLeftBehind.Count > MaximumListedSmallShips)
            {
                _lines.Add("   …");
            }
        }
    }
}
