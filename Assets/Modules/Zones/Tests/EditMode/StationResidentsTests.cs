using LastFleet.Shared.Randomness;
using NUnit.Framework;

namespace LastFleet.Zones.Tests
{
    public class StationResidentsTests
    {
        static StationProfile Profile(int occupants, StationAttitude attitude, StationAtmosphere atmosphere = StationAtmosphere.Breathable,
            StationRadiation radiation = StationRadiation.None)
        {
            return new StationProfile(atmosphere, occupants, attitude, StationCondition.Intact, radiation);
        }

        [Test]
        public void OnASafeStation_AboutATenthWantsToCome()
        {
            Assert.AreEqual(200, new StationResidents(Profile(2000, StationAttitude.Friendly)).Willing);
            Assert.AreEqual(5, new StationResidents(Profile(50, StationAttitude.Wary)).Willing);
            Assert.AreEqual(1, new StationResidents(Profile(3, StationAttitude.Friendly)).Willing, "At least one person asks to come.");
        }

        [Test]
        public void OnAHazardousStation_EveryoneWantsToCome_UnlessHostile()
        {
            Assert.AreEqual(40, new StationResidents(Profile(40, StationAttitude.Wary, StationAtmosphere.Toxic)).Willing);
            Assert.AreEqual(12, new StationResidents(Profile(12, StationAttitude.Friendly, radiation: StationRadiation.Elevated)).Willing);
            Assert.AreEqual(0, new StationResidents(Profile(40, StationAttitude.Hostile, StationAtmosphere.Toxic)).Willing);
            Assert.AreEqual(0, new StationResidents(Profile(400, StationAttitude.Hostile)).Willing);
        }

        [Test]
        public void Boarding_TakesOnlyTheWilling()
        {
            StationResidents residents = new StationResidents(Profile(100, StationAttitude.Friendly));

            Assert.AreEqual(10, residents.Board(40));
            Assert.AreEqual(0, residents.Willing);
            Assert.AreEqual(90, residents.Count);
            Assert.AreEqual(10, residents.Rescued);
            Assert.AreEqual(0, residents.Board(5));
        }

        [Test]
        public void Survivors_DieOffOverTime()
        {
            StationResidents residents = new StationResidents(Profile(100, StationAttitude.Friendly, StationAtmosphere.Toxic));

            Assert.AreEqual(10, residents.Perish(0.1f, 60f));
            Assert.AreEqual(90, residents.Count);
            Assert.AreEqual(90, residents.Willing);
            Assert.AreEqual(10, residents.Died);
        }

        [Test]
        public void HazardousStations_AreGeneratedWithFewSurvivors()
        {
            DeterministicRandom random = new DeterministicRandom(11UL);
            int hazardous = 0;
            for (int i = 0; i < 4000; i++)
            {
                StationProfile profile = StationGeneration.RollProfile(random, StationType.Habitat);
                if (profile.IsInhabited && profile.IsHazardous)
                {
                    hazardous++;
                    Assert.LessOrEqual(profile.Occupants, 3000 / StationGeneration.HazardousOccupantDivisor);
                }
            }
            Assert.Greater(hazardous, 0, "The sample should contain hazardous habitats.");
        }
    }
}
