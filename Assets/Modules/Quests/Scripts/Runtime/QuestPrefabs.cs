using LastFleet.Combat;
using LastFleet.Ships;

namespace LastFleet.Quests
{
    /// <summary>Ships and effects quest steps spawn.</summary>
    public sealed class QuestPrefabs
    {
        public QuestPrefabs(ShipAgent shuttle, ShipAgent proninFighter, ExplosionEffect explosion)
        {
            Shuttle = shuttle;
            ProninFighter = proninFighter;
            Explosion = explosion;
        }

        public ShipAgent Shuttle { get; }
        public ShipAgent ProninFighter { get; }
        public ExplosionEffect Explosion { get; }
    }
}
