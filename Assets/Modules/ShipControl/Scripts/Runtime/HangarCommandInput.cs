using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastFleet.ShipControl
{
    /// <summary>
    /// Hangar commands (GDD: Controls). H returns the selected small ships to a hangar, Ctrl+H recalls
    /// every small ship (GDD: recall all), L launches the selected docked ships. Ships land in their
    /// home hangar if it has room, otherwise in any hangar with a free slot.
    /// </summary>
    public sealed class HangarCommandInput : MonoBehaviour
    {
        [SerializeField] ShipCommandInput _commandInput;
        [SerializeField] ShipRegistry _registry;

        readonly List<ShipAgent> _ships = new List<ShipAgent>();

        /// <summary>Short feedback for the player about what an input did.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_commandInput == null || _registry == null)
            {
                Debug.LogError("HangarCommandInput needs the ship command input and the ship registry.", this);
                enabled = false;
            }
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.hKey.wasPressedThisFrame)
            {
                ReturnToHangar(keyboard.ctrlKey.isPressed ? _registry.Ships : _commandInput.Selection.Ships);
            }
            else if (keyboard.lKey.wasPressedThisFrame)
            {
                LaunchSelected();
            }
        }

        /// <summary>Sends small ships that are not docked to land; capital ships are skipped.</summary>
        public void ReturnToHangar(IReadOnlyList<ShipAgent> ships)
        {
            _ships.Clear();
            foreach (ShipAgent ship in ships)
            {
                if (!ship.IsCapital && !ship.IsDocked)
                {
                    _ships.Add(ship);
                }
            }

            int withoutHangar = 0;
            foreach (ShipAgent ship in _ships)
            {
                Hangar hangar = _registry.FindHangarFor(ship);
                if (hangar == null)
                {
                    withoutHangar++;
                    continue;
                }
                ship.Order(ShipOrder.DockAt(hangar), false);
            }
            Notice?.Invoke(ShipControlTexts.ReturningToHangar.Format(("count", _ships.Count - withoutHangar), ("withoutHangar", withoutHangar)));
        }

        void LaunchSelected()
        {
            LaunchShips(_commandInput.Selection.Ships);
        }

        /// <summary>Queues the docked ones among the given ships for launch.</summary>
        public void LaunchShips(IReadOnlyList<ShipAgent> ships)
        {
            int launching = 0;
            foreach (ShipAgent ship in ships)
            {
                if (ship.IsDocked)
                {
                    ship.DockedIn.RequestLaunch(ship);
                    launching++;
                }
            }
            Notice?.Invoke(launching > 0 ? ShipControlTexts.Launching.Format(("count", launching)) : ShipControlTexts.NoDockedShipsSelected);
        }
    }
}
