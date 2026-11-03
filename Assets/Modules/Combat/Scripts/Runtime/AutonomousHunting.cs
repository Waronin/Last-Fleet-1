using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Combat
{
    /// <summary>How an autonomous hunter picks its prey.</summary>
    public enum HuntTargetChoice
    {
        /// <summary>By <see cref="HuntingRules"/>: small, isolated and near ships first.</summary>
        Rules,
        /// <summary>Any ship of the opponent in space, small or capital, picked at random.</summary>
        Random
    }

    /// <summary>
    /// Lets an enemy fighter pick its own prey (GDD: Pronin fighters look for targets in the system and
    /// jump to them). It only acts for enemy ships: a captured fighter on the player's side waits for
    /// orders like every other ship of the fleet.
    /// </summary>
    [RequireComponent(typeof(ShipAgent), typeof(FighterCombat))]
    public sealed class AutonomousHunting : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float _searchIntervalSeconds = 1f;
        [SerializeField] HuntTargetChoice _targetChoice = HuntTargetChoice.Rules;

        ShipAgent _ship;
        FighterCombat _fighter;
        CombatService _combat;
        float _secondsUntilSearch;

        public HuntTargetChoice TargetChoice => _targetChoice;

        void Awake()
        {
            _ship = GetComponent<ShipAgent>();
            _fighter = GetComponent<FighterCombat>();
        }

        public void Initialize(CombatService combat)
        {
            _combat = combat;
        }

        /// <summary>E.g. fighters of an attack wave spread over random targets instead of following the rules.</summary>
        public void SetTargetChoice(HuntTargetChoice choice)
        {
            _targetChoice = choice;
        }

        void Update()
        {
            if (_combat == null || _ship.Allegiance == Allegiance.HumanFleet || _ship.IsDerelict || _ship.IsDocked ||
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

            ShipAgent prey = _targetChoice == HuntTargetChoice.Random ? _combat.FindRandomHuntTarget(_ship) : _combat.FindHuntTarget(_ship);
            if (prey != null)
            {
                _fighter.Attack(prey);
            }
        }
    }
}
