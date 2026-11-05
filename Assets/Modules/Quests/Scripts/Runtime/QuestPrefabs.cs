using LastFleet.Combat;
using LastFleet.Ships;

namespace LastFleet.Quests
{
    /// <summary>Ships and effects quest steps spawn.</summary>
    public sealed class QuestPrefabs
    {
        public QuestPrefabs(ShipAgent shuttle, ShipAgent proninFighter, ShipAgent guardFighter, ExplosionEffect explosion)
        {
            Shuttle = shuttle;
            ProninFighter = proninFighter;
            GuardFighter = guardFighter;
            Explosion = explosion;
        }

        public ShipAgent Shuttle { get; }
        public ShipAgent ProninFighter { get; }

        /// <summary>A station's own fighter: on the enemy side, but it only defends what its quest step tells it to.</summary>
        public ShipAgent GuardFighter { get; }

        public ExplosionEffect Explosion { get; }
    }
}
