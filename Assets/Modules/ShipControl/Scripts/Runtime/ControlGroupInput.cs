using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastFleet.ShipControl
{
    /// <summary>
    /// Keyboard grouping (GDD: Controls, Squads). Ctrl + number stores the selection, number recalls
    /// it, pressing the number twice quickly asks the camera to jump there. G forms the selection into
    /// a new squad. Works on the maps as well as in zones.
    /// </summary>
    public sealed class ControlGroupInput : MonoBehaviour
    {
        const float DoublePressSeconds = 0.35f;

        // Index = group number; digit 0 is group 0.
        static readonly Key[] GroupKeys =
        {
            Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
            Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };

        [SerializeField] ShipCommandInput _commandInput;
        [SerializeField] ShipRegistry _registry;

        readonly ControlGroups _groups = new ControlGroups();
        int _lastRecalledGroup = -1;
        float _lastRecallTime = float.NegativeInfinity;

        public ControlGroups Groups => _groups;

        /// <summary>The player asked to look at a ship, e.g. by double-pressing a group key.</summary>
        public event Action<ShipAgent> FocusRequested;

        /// <summary>Short feedback for the player about what an input did.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_commandInput == null || _registry == null)
            {
                Debug.LogError("ControlGroupInput needs the ship command input and the ship registry.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _registry.ShipRemoved += _groups.Remove;
        }

        void OnDisable()
        {
            _registry.ShipRemoved -= _groups.Remove;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.gKey.wasPressedThisFrame)
            {
                FormSquadFromSelection();
            }

            for (int group = 0; group < GroupKeys.Length; group++)
            {
                if (!keyboard[GroupKeys[group]].wasPressedThisFrame)
                {
                    continue;
                }

                if (keyboard.ctrlKey.isPressed)
                {
                    StoreGroup(group);
                }
                else
                {
                    RecallGroup(group);
                }
            }
        }

        void StoreGroup(int group)
        {
            _groups.Store(group, _commandInput.Selection.Ships);
            Notice?.Invoke(ShipControlTexts.GroupStored.Format(("count", _commandInput.Selection.Count), ("group", group)));
        }

        void RecallGroup(int group)
        {
            IReadOnlyList<ShipAgent> ships = _groups.Get(group);
            if (ships.Count == 0)
            {
                Notice?.Invoke(ShipControlTexts.GroupEmpty.Format(("group", group)));
                return;
            }

            bool isDoublePress = group == _lastRecalledGroup && Time.unscaledTime - _lastRecallTime <= DoublePressSeconds;
            _lastRecalledGroup = group;
            _lastRecallTime = Time.unscaledTime;

            _commandInput.Selection.Select(ships, false);
            if (isDoublePress)
            {
                FocusRequested?.Invoke(ships[0]);
            }
        }

        void FormSquadFromSelection()
        {
            ShipSelection selection = _commandInput.Selection;
            if (selection.Count == 0)
            {
                Notice?.Invoke(ShipControlTexts.SelectShipsForSquad);
                return;
            }

            string className = selection.Ships[0].ClassName;
            foreach (ShipAgent ship in selection.Ships)
            {
                if (ship.ClassName != className)
                {
                    Notice?.Invoke(ShipControlTexts.SquadNeedsOneClass);
                    return;
                }
            }

            Squad squad = _registry.Squads.Form(selection.Ships);
            Notice?.Invoke(ShipControlTexts.SquadFormed.Format(("squad", squad.Name), ("count", squad.Members.Count)));
        }
    }
}
