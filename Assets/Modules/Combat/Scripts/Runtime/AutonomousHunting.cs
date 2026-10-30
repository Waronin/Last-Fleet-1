using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Combat
{
    /// <summary>
    /// Lets an enemy fighter pick its own prey (GDD: Pronin fighters look for targets in the system and
    /// jump to them). It only acts for enemy ships: a captured fighter on the player's side waits for
    /// orders like every other ship of the fleet. Targets are chosen by <see cref="HuntingRules"/>.
    /// </summary>
    [RequireComponent(typeof(ShipAgent), typeof(FighterCombat))]
    public sealed class AutonomousHunting : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float _searchIntervalSeconds = 1f;

        ShipAgent _ship;
        FighterCombat _fighter;
        CombatService _combat;
        float _secondsUntilSearch;

        void Awake()
        {
            _ship = GetComponent<ShipAgent>();
            _fighter = GetComponent<FighterCombat>();
        }

        public void Initialize(CombatService combat)
        {
            _combat = combat;
        }

        void Update()
        {
            if (_combat == null || _ship.Allegiance == Allegiance.HumanFleet || _ship.IsDerelict ||
                _fighter.State == CombatState.Engaging)
            {
                return;
            }

            _secondsUntilSearch -= Time.deltaTime;
            if (_secondsUntilSearch > 0f)
            {
                return;
            }
            _secondsUntilSearch = _searchIntervalSeconds;

            ShipAgent prey = _combat.FindHuntTarget(_ship);
            if (prey != null)
            {
                _fighter.Engage(prey);
            }
        }
    }
}
