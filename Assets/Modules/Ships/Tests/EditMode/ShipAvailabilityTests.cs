using System.Collections.Generic;
using NUnit.Framework;

namespace LastFleet.Ships.Tests
{
    public class ShipAvailabilityTests
    {
        // Candidates as (name, free, cost) to test the ranking without scene objects.
        static readonly List<(string Name, bool Free, float Cost)> Candidates = new List<(string Name, bool Free, float Cost)>
        {
            ("busy near", false, 5f),
            ("free far", true, 900f),
            ("free near", true, 20f),
            ("busy far", false, 800f)
        };

        [Test]
        public void FreeShips_ComeFirst_NearestFirst()
        {
            List<string> names = Pick(2);

            CollectionAssert.AreEqual(new string[] { "free near", "free far" }, names);
        }

        [Test]
        public void BusyShips_FillUpByDistance_WhenFreeOnesRunOut()
        {
            List<string> names = Pick(3);

            CollectionAssert.AreEqual(new string[] { "free near", "free far", "busy near" }, names);
        }

        [Test]
        public void CountZeroOrNoCandidates_PicksNothing()
        {
            Assert.AreEqual(0, Pick(0).Count);

            List<(string Name, bool Free, float Cost)> result = new List<(string Name, bool Free, float Cost)>();
            ShipAvailability.PickFreeThenNearest(new List<(string Name, bool Free, float Cost)>(), c => c.Free, c => c.Cost, 3, result);
            Assert.AreEqual(0, result.Count);
        }

        static List<string> Pick(int count)
        {
            List<(string Name, bool Free, float Cost)> result = new List<(string Name, bool Free, float Cost)>();
            ShipAvailability.PickFreeThenNearest(Candidates, c => c.Free, c => c.Cost, count, result);

            List<string> names = new List<string>();
            foreach ((string name, bool _, float _) in result)
            {
                names.Add(name);
            }
            return names;
        }
    }
}
