using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Quests.Tests
{
    public class QuestCargoTests
    {
        static readonly QuestItem Sample = new QuestItem("test.sample", new GameTextKey("Quests", "test.sample"));
        static readonly QuestItem Other = new QuestItem("test.other", new GameTextKey("Quests", "test.other"));

        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void DestroyShips()
        {
            foreach (GameObject shipObject in _created)
            {
                if (shipObject != null)
                {
                    Object.DestroyImmediate(shipObject);
                }
            }
            _created.Clear();
        }

        [Test]
        public void LoadedItem_RidesAboardItsShip_UntilUnloaded()
        {
            QuestCargo cargo = new QuestCargo();
            ShipAgent ship = CreateShip();

            cargo.Load(Sample, ship);

            Assert.IsTrue(cargo.IsCarriedBy(Sample, ship));
            Assert.IsFalse(cargo.TryGetCarrier(Other, out ShipAgent _));
            Assert.IsTrue(cargo.Unload(Sample));
            Assert.IsFalse(cargo.TryGetCarrier(Sample, out ShipAgent _));
            Assert.IsFalse(cargo.Unload(Sample));
        }

        [Test]
        public void AnItemCanOnlyBeAboardOneShip()
        {
            QuestCargo cargo = new QuestCargo();
            cargo.Load(Sample, CreateShip());

            Assert.Throws<System.InvalidOperationException>(() => cargo.Load(Sample, CreateShip()));
        }

        [Test]
        public void RemovedShip_LosesWhatItCarries_AndNothingElse()
        {
            QuestCargo cargo = new QuestCargo();
            ShipAgent carrier = CreateShip();
            ShipAgent bystander = CreateShip();
            cargo.Load(Sample, carrier);
            cargo.Load(Other, bystander);
            List<QuestItem> lost = new List<QuestItem>();
            cargo.Lost += (item, ship) =>
            {
                lost.Add(item);
                Assert.AreSame(carrier, ship);
            };

            cargo.ShipRemoved(carrier);

            CollectionAssert.AreEqual(new[] { Sample }, lost);
            Assert.IsFalse(cargo.TryGetCarrier(Sample, out ShipAgent _));
            Assert.IsTrue(cargo.IsCarriedBy(Other, bystander));
        }

        ShipAgent CreateShip()
        {
            GameObject shipObject = new GameObject("Ship");
            _created.Add(shipObject);
            return shipObject.AddComponent<ShipAgent>();
        }
    }
}
