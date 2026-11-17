using System;
using System.Collections.Generic;
using LastFleet.Zones;

namespace LastFleet.Ships
{
    /// <summary>
    /// The effects that apply to every ship of one side, e.g. those of the player's fleet modules. Each
    /// side's registry owns one; ships read it through <see cref="ShipAgent.Effects"/>, so a ship that
    /// changes sides takes on the effects of its new side. Shares of the same stat add up
    /// (+30 % and -10 % make +20 %); a multiplier never drops below <see cref="MinimumMultiplier"/>.
    /// </summary>
    public sealed class FleetEffects
    {
        public const float MinimumMultiplier = 0.1f;

        static readonly int StatCount = Enum.GetValues(typeof(FleetStat)).Length;

        readonly float[] _shares = new float[StatCount];
        readonly HashSet<FleetAbility> _abilities = new HashSet<FleetAbility>();

        /// <summary>Raised after the effects were replaced.</summary>
        public event Action Changed;

        /// <summary>The summed share of a stat, e.g. 0.2 for 20 % more.</summary>
        public float ShareOf(FleetStat stat)
        {
            return _shares[(int)stat];
        }

        /// <summary>The factor on the base value of a stat, 1 without effects.</summary>
        public float Multiplier(FleetStat stat)
        {
            return Math.Max(MinimumMultiplier, 1f + _shares[(int)stat]);
        }

        public bool Has(FleetAbility ability)
        {
            return _abilities.Contains(ability);
        }

        public float MiningRateMultiplier(ResourceKind resource)
        {
            return resource switch
            {
                ResourceKind.Gas => Multiplier(FleetStat.GasMiningRate),
                ResourceKind.Ore => Multiplier(FleetStat.OreMiningRate),
                ResourceKind.Ice => Multiplier(FleetStat.IceMiningRate),
                _ => 1f
            };
        }

        public float SpeedMultiplier(ShipSize size)
        {
            return Multiplier(size == ShipSize.Capital ? FleetStat.CapitalShipSpeed : FleetStat.SmallShipSpeed);
        }

        public float FuelUseMultiplier(ShipSize size)
        {
            return Multiplier(size == ShipSize.Capital ? FleetStat.CapitalShipFuelUse : FleetStat.SmallShipFuelUse);
        }

        /// <summary>Replaces all effects with the given ones and reports the change.</summary>
        public void Replace(IEnumerable<FleetStatBonus> bonuses, IEnumerable<FleetAbility> abilities)
        {
            if (bonuses == null)
            {
                throw new ArgumentNullException(nameof(bonuses));
            }
            if (abilities == null)
            {
                throw new ArgumentNullException(nameof(abilities));
            }

            Array.Clear(_shares, 0, _shares.Length);
            foreach (FleetStatBonus bonus in bonuses)
            {
                _shares[(int)bonus.Stat] += bonus.Share;
            }
            _abilities.Clear();
            _abilities.UnionWith(abilities);
            Changed?.Invoke();
        }
    }
}
