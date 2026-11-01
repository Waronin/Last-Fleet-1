using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Combat
{
    /// <summary>
    /// Whom an escort protects (GDD stance: escort, e.g. harvesters on their way home): one ship, or a
    /// whole squad, which is led by its current leader and protected as long as any member is left.
    /// While the protected ship sits in a hangar, the escort waits at its carrier.
    /// </summary>
    public sealed class EscortDuty
    {
        readonly ShipAgent _ship;
        readonly Squad _squad;

        public EscortDuty(ShipAgent ship, Squad squad, float slotAngleDegrees)
        {
            _ship = ship;
            _squad = squad;
            SlotAngleDegrees = slotAngleDegrees;
        }

        /// <summary>Where around the followed ship this escort keeps station, 0 being straight ahead.</summary>
        public float SlotAngleDegrees { get; }

        public GameText DisplayName => _squad != null ? _squad.DisplayName : _ship.DisplayName;

        /// <summary>The protected ship that leads; null once nothing is left to protect.</summary>
        public ShipAgent Lead
        {
            get
            {
                if (_squad != null)
                {
                    return _squad.Leader;
                }
                // Unity's overloaded equality treats a destroyed ship as null.
                return _ship != null ? _ship : null;
            }
        }

        public bool IsOver => Lead == null;

        /// <summary>The ship to follow: the lead, or its carrier while the lead sits in a hangar.</summary>
        public ShipAgent Anchor
        {
            get
            {
                ShipAgent lead = Lead;
                if (lead == null)
                {
                    return null;
                }
                return lead.IsDocked ? lead.DockedIn.Carrier : lead;
            }
        }

        /// <summary>The protected ships that are flying, whose attackers the escort goes for.</summary>
        public void CollectGuarded(List<ShipAgent> result)
        {
            result.Clear();
            if (_squad != null)
            {
                foreach (ShipAgent member in _squad.Members)
                {
                    if (member != null && !member.IsDocked)
                    {
                        result.Add(member);
                    }
                }
                return;
            }
            if (_ship != null && !_ship.IsDocked)
            {
                result.Add(_ship);
            }
        }

        /// <summary>Offset from the followed ship in its own frame: on the slot's bearing, just outside its hull.</summary>
        public Vector3 SlotOffset(ShipAgent anchor, float distanceUnits)
        {
            float radians = SlotAngleDegrees * Mathf.Deg2Rad;
            float distance = anchor.ObstacleRadiusUnits + distanceUnits;
            return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * distance;
        }
    }
}
