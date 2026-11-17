using System;
using System.Collections.Generic;
using LastFleet.Shared.Randomness;
using LastFleet.Ships;
using LastFleet.Zones;
using NUnit.Framework;

namespace LastFleet.FleetModules.Tests
{
    public class FleetModuleTests
    {
        [Test]
        public void Slots_FillUpAndThenOnlyReplace()
        {
            FleetModuleSlots slots = new FleetModuleSlots(3);
            slots.Install(FleetModuleKind.GasMining);
            slots.Install(FleetModuleKind.CargoBays);
            slots.Install(FleetModuleKind.HullPlating);

            Assert.IsFalse(slots.HasFreeSlot);
            Assert.Throws<InvalidOperationException>(() => slots.Install(FleetModuleKind.OreMining));

            FleetModuleKind removed = slots.Replace(1, FleetModuleKind.OreMining);

            Assert.AreEqual(FleetModuleKind.CargoBays, removed);
            Assert.IsFalse(slots.IsInstalled(FleetModuleKind.CargoBays));
            CollectionAssert.AreEqual(new[] { FleetModuleKind.GasMining, FleetModuleKind.OreMining, FleetModuleKind.HullPlating }, slots.Installed);
        }

        [Test]
        public void Slots_RejectTheSameModuleTwice()
        {
            FleetModuleSlots slots = new FleetModuleSlots(4);
            slots.Install(FleetModuleKind.SignalAmplifier);

            Assert.Throws<InvalidOperationException>(() => slots.Install(FleetModuleKind.SignalAmplifier));
        }

        [Test]
        public void Offer_HasThreeDifferentModulesThatAreNotInstalled()
        {
            FleetModuleSlots slots = new FleetModuleSlots(5);
            slots.Install(FleetModuleKind.GasMining);
            slots.Install(FleetModuleKind.OreMining);
            slots.Install(FleetModuleKind.IceMining);
            slots.Install(FleetModuleKind.CargoBays);
            slots.Install(FleetModuleKind.HullPlating);
            List<FleetModuleKind> offer = new List<FleetModuleKind>();

            for (ulong seed = 1; seed <= 50; seed++)
            {
                FleetModuleOffer.Roll(new DeterministicRandom(seed), slots, offer);

                Assert.AreEqual(FleetModuleOffer.Size, offer.Count);
                CollectionAssert.AllItemsAreUnique(offer);
                foreach (FleetModuleKind kind in offer)
                {
                    Assert.IsFalse(slots.IsInstalled(kind), $"{kind} is installed already.");
                }
            }
        }

        [Test]
        public void EveryModule_HasACatalogEntry()
        {
            foreach (FleetModuleKind kind in FleetModuleCatalog.All)
            {
                bool hasEffect = FleetModuleCatalog.BonusesOf(kind).Count > 0 || FleetModuleCatalog.TryGetAbility(kind, out FleetAbility _);
                Assert.IsTrue(hasEffect, $"{kind} does nothing.");
            }
        }

        [Test]
        public void InstalledModules_GiveTheFleetTheirSummedEffects()
        {
            FleetEffects effects = new FleetEffects();

            FleetModuleCatalog.ApplyTo(new[] { FleetModuleKind.CargoBays, FleetModuleKind.HullPlating, FleetModuleKind.RadiationShielding,
                FleetModuleKind.CapitalShipDrives, FleetModuleKind.SignalAmplifier }, effects);

            Assert.AreEqual(1.3f, effects.Multiplier(FleetStat.CargoCapacity), 1e-5f);
            // +30 % plating and -10 % for the cargo bays.
            Assert.AreEqual(1.2f, effects.Multiplier(FleetStat.MaximumHull), 1e-5f);
            Assert.AreEqual(0.9f, effects.Multiplier(FleetStat.RepairRate), 1e-5f);
            Assert.AreEqual(1.5f, effects.SpeedMultiplier(ShipSize.Capital), 1e-5f);
            Assert.AreEqual(1.1f, effects.FuelUseMultiplier(ShipSize.Capital), 1e-5f);
            Assert.AreEqual(0.5f, effects.Multiplier(FleetStat.LateSignalDelay), 1e-5f);
            Assert.AreEqual(1f, effects.MiningRateMultiplier(ResourceKind.Gas));
            Assert.IsTrue(effects.Has(FleetAbility.RadiationImmunity));
            Assert.IsFalse(effects.Has(FleetAbility.PacifyHostileStations));
        }
    }
}
