using System.Collections.Generic;
using LastFleet.Zones;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Inspection.Tests
{
    public class DepositInspectionTests
    {
        [Test]
        public void UnscannedDeposit_GivesNothingAway()
        {
            ResourceDeposit deposit = CreateDeposit(ResourceKind.Ice, DepositHazard.Radioactive);
            List<string> lines = new List<string>();

            DepositInspection.DescribeDeposit(deposit, 0, lines);

            string text = string.Join("\n", lines);
            StringAssert.DoesNotContain("Ice", text);
            StringAssert.DoesNotContain("Radioactive", text);
            StringAssert.DoesNotContain(deposit.InitialAmount.ToString("0"), text);
        }

        [Test]
        public void ScannedDeposit_ShowsResourceAmountHazardAndSlots()
        {
            ResourceDeposit deposit = CreateDeposit(ResourceKind.Ice, DepositHazard.Radioactive);
            deposit.Scan();
            deposit.Extract(25f);
            List<string> lines = new List<string>();

            DepositInspection.DescribeDeposit(deposit, 1, lines);

            string text = string.Join("\n", lines);
            StringAssert.Contains("Ice", text);
            StringAssert.Contains("Radioactive", text);
            StringAssert.Contains("75 of 100", text);
            StringAssert.Contains($"1/{deposit.MiningSlotCount}", text);
        }

        [Test]
        public void DepletedDeposit_SaysSo()
        {
            ResourceDeposit deposit = CreateDeposit(ResourceKind.Ore, DepositHazard.None);
            deposit.Extract(deposit.InitialAmount);
            List<string> lines = new List<string>();

            DepositInspection.DescribeDeposit(deposit, 0, lines);

            CollectionAssert.AreEqual(new string[] { "Depleted." }, lines);
        }

        static ResourceDeposit CreateDeposit(ResourceKind resource, DepositHazard hazard)
        {
            return new ResourceDeposit(DepositShape.Asteroid, resource, Vector3.zero, 2f, 100f, hazard, 0f);
        }
    }
}
