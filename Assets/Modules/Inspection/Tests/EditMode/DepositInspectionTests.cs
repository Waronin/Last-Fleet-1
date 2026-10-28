using System.Collections.Generic;
using LastFleet.Shared.Text;
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
            List<GameText> lines = new List<GameText>();

            DepositInspection.DescribeDeposit(deposit, 0, lines);

            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual(InspectionTexts.DepositUnknown, lines[0].Key);
            Assert.IsEmpty(lines[0].Arguments, "The unknown line must not carry resource, amount or hazard.");
        }

        [Test]
        public void ScannedDeposit_ShowsResourceAmountHazardAndSlots()
        {
            ResourceDeposit deposit = CreateDeposit(ResourceKind.Ice, DepositHazard.Radioactive);
            deposit.Scan();
            deposit.Extract(25f);
            List<GameText> lines = new List<GameText>();

            DepositInspection.DescribeDeposit(deposit, 1, lines);

            Assert.AreEqual(ZoneTexts.Resources.For(ResourceKind.Ice).Key, TextArgument(lines, InspectionTexts.DepositResource, "resource").Key);
            Assert.AreEqual(ZoneTexts.Hazards.For(DepositHazard.Radioactive).Key, TextArgument(lines, InspectionTexts.DepositHazard, "hazard").Key);
            Assert.AreEqual(75f, Argument(lines, InspectionTexts.DepositRemaining, "remaining"));
            Assert.AreEqual(100f, Argument(lines, InspectionTexts.DepositRemaining, "initial"));
            Assert.AreEqual(1, Argument(lines, InspectionTexts.DepositSlots, "used"));
            Assert.AreEqual(deposit.MiningSlotCount, Argument(lines, InspectionTexts.DepositSlots, "total"));
        }

        [Test]
        public void DepletedDeposit_SaysSo()
        {
            ResourceDeposit deposit = CreateDeposit(ResourceKind.Ore, DepositHazard.None);
            deposit.Extract(deposit.InitialAmount);
            List<GameText> lines = new List<GameText>();

            DepositInspection.DescribeDeposit(deposit, 0, lines);

            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual(InspectionTexts.DepositDepleted, lines[0].Key);
        }

        static object Argument(List<GameText> lines, GameTextKey line, string name)
        {
            foreach (GameText text in lines)
            {
                if (!text.Key.Equals(line))
                {
                    continue;
                }
                foreach (GameTextArgument argument in text.Arguments)
                {
                    if (argument.Name == name)
                    {
                        return argument.Value;
                    }
                }
            }
            Assert.Fail($"No line {line} with argument {name}.");
            return null;
        }

        static GameText TextArgument(List<GameText> lines, GameTextKey line, string name)
        {
            object value = Argument(lines, line, name);
            Assert.IsInstanceOf<GameText>(value);
            return (GameText)value;
        }

        static ResourceDeposit CreateDeposit(ResourceKind resource, DepositHazard hazard)
        {
            return new ResourceDeposit(DepositShape.Asteroid, resource, Vector3.zero, 2f, 100f, hazard, 0f);
        }
    }
}
