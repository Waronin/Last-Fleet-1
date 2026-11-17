using System;
using System.Collections.Generic;
using LastFleet.Ships;

namespace LastFleet.FleetModules
{
    /// <summary>
    /// What each fleet module does (designer's list of 2026-11-17): stat changes as shares of the base
    /// value, and abilities. Values are balancing starting points.
    /// </summary>
    public static class FleetModuleCatalog
    {
        static readonly Dictionary<FleetModuleKind, FleetStatBonus[]> BonusesByKind = new Dictionary<FleetModuleKind, FleetStatBonus[]>
        {
            { FleetModuleKind.GasMining, new[] { new FleetStatBonus(FleetStat.GasMiningRate, 0.2f) } },
            { FleetModuleKind.OreMining, new[] { new FleetStatBonus(FleetStat.OreMiningRate, 0.2f) } },
            { FleetModuleKind.IceMining, new[] { new FleetStatBonus(FleetStat.IceMiningRate, 0.2f) } },
            { FleetModuleKind.CargoBays, new[] { new FleetStatBonus(FleetStat.CargoCapacity, 0.3f), new FleetStatBonus(FleetStat.MaximumHull, -0.1f) } },
            { FleetModuleKind.HullPlating, new[] { new FleetStatBonus(FleetStat.MaximumHull, 0.3f), new FleetStatBonus(FleetStat.RepairRate, -0.1f) } },
            { FleetModuleKind.RadiationShielding, Array.Empty<FleetStatBonus>() },
            { FleetModuleKind.StationDiplomacy, Array.Empty<FleetStatBonus>() },
            { FleetModuleKind.CapitalShipDrives, new[] { new FleetStatBonus(FleetStat.CapitalShipSpeed, 0.5f), new FleetStatBonus(FleetStat.CapitalShipFuelUse, 0.1f) } },
            { FleetModuleKind.SmallShipDrives, new[] { new FleetStatBonus(FleetStat.SmallShipSpeed, 0.4f), new FleetStatBonus(FleetStat.SmallShipFuelUse, 0.08f) } },
            { FleetModuleKind.SignalAmplifier, new[] { new FleetStatBonus(FleetStat.LateSignalDelay, -0.5f) } }
        };

        static readonly FleetModuleKind[] AllKinds = (FleetModuleKind[])Enum.GetValues(typeof(FleetModuleKind));

        public static IReadOnlyList<FleetModuleKind> All => AllKinds;

        public static IReadOnlyList<FleetStatBonus> BonusesOf(FleetModuleKind kind)
        {
            if (!BonusesByKind.TryGetValue(kind, out FleetStatBonus[] bonuses))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "The fleet module has no catalog entry.");
            }
            return bonuses;
        }

        /// <summary>The ability the module gives, if it gives one.</summary>
        public static bool TryGetAbility(FleetModuleKind kind, out FleetAbility ability)
        {
            switch (kind)
            {
                case FleetModuleKind.RadiationShielding:
                    ability = FleetAbility.RadiationImmunity;
                    return true;
                case FleetModuleKind.StationDiplomacy:
                    ability = FleetAbility.PacifyHostileStations;
                    return true;
                default:
                    ability = default;
                    return false;
            }
        }

        /// <summary>Gives the fleet the summed effects of the installed modules, replacing the previous ones.</summary>
        public static void ApplyTo(IEnumerable<FleetModuleKind> installed, FleetEffects effects)
        {
            if (installed == null)
            {
                throw new ArgumentNullException(nameof(installed));
            }
            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            List<FleetStatBonus> bonuses = new List<FleetStatBonus>();
            List<FleetAbility> abilities = new List<FleetAbility>();
            foreach (FleetModuleKind kind in installed)
            {
                bonuses.AddRange(BonusesOf(kind));
                if (TryGetAbility(kind, out FleetAbility ability))
                {
                    abilities.Add(ability);
                }
            }
            effects.Replace(bonuses, abilities);
        }
    }
}
