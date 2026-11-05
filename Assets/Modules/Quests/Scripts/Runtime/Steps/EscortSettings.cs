using System;
using LastFleet.Dialogue;
using LastFleet.Ships;
using LastFleet.Zones;

namespace LastFleet.Quests
{
    /// <summary>Numbers and texts of an <see cref="EscortStep"/>.</summary>
    public sealed class EscortSettings
    {
        public EscortSettings(QuestStationSpec station, int passengers, int attackerCount, float attackDelaySeconds,
            Func<ZoneStation, Action, DialogueScript> offer, Func<ShipAgent, DialogueScript> arrivedMessage)
        {
            Station = station ?? throw new ArgumentNullException(nameof(station));
            if (passengers < 0 || attackerCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(passengers), "Passengers and attackers must not be negative.");
            }
            if (attackDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(attackDelaySeconds), "The attack cannot come before the launch.");
            }
            Passengers = passengers;
            AttackerCount = attackerCount;
            AttackDelaySeconds = attackDelaySeconds;
            Offer = offer ?? throw new ArgumentNullException(nameof(offer));
            ArrivedMessage = arrivedMessage;
        }

        /// <summary>The station the shuttle starts from; its crew offers the escort when hailed.</summary>
        public QuestStationSpec Station { get; }

        /// <summary>People aboard the shuttle, who join the fleet with it.</summary>
        public int Passengers { get; }

        /// <summary>Pronin fighters that go for the shuttle; 0 for a quiet trip.</summary>
        public int AttackerCount { get; }

        public float AttackDelaySeconds { get; }

        /// <summary>The conversation on a hail; it must call the given action when the player accepts.</summary>
        public Func<ZoneStation, Action, DialogueScript> Offer { get; }

        /// <summary>The radio call once the shuttle has landed, or null.</summary>
        public Func<ShipAgent, DialogueScript> ArrivedMessage { get; }
    }
}
