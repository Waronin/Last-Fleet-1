using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Zones.Tests
{
    public class WreckDepositTests
    {
        [Test]
        public void Wreck_YieldsMetal_IsKnownOnSight_AndHasNoHazard()
        {
            ResourceDeposit wreck = ResourceDeposit.Wreck(new Vector3(10f, 0f, 5f), 25f, 30f);

            Assert.AreEqual(DepositShape.Wreck, wreck.Shape);
            Assert.AreEqual(ResourceKind.Metal, wreck.Resource);
            Assert.AreEqual(25f, wreck.RemainingAmount);
            Assert.IsTrue(wreck.IsScanned);
            Assert.AreEqual(DepositHazard.None, wreck.Hazard);
            Assert.GreaterOrEqual(wreck.MiningSlotCount, 1);
        }

        [Test]
        public void OnlyAsteroids_CarryMiningHazards()
        {
            Assert.Throws<ArgumentException>(() =>
                new ResourceDeposit(DepositShape.Wreck, ResourceKind.Metal, Vector3.zero, 1f, 10f, DepositHazard.Brittle, 0f));
        }

        [Test]
        public void AddDeposit_AppendsToTheZoneContents()
        {
            ZoneContents contents = new ZoneContents(50f, default, new List<ResourceDeposit>(), new List<ZoneStation>());
            ResourceDeposit wreck = ResourceDeposit.Wreck(Vector3.zero, 25f, 0f);

            contents.AddDeposit(wreck);

            Assert.AreEqual(1, contents.Deposits.Count);
            Assert.AreSame(wreck, contents.Deposits[0]);
        }
    }
}
