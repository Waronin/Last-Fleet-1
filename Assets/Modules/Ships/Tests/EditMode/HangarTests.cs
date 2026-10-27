using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastFleet.Ships.Tests
{
    public class HangarTests
    {
        const float LaunchInterval = 1.5f;

        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void DestroyObjects()
        {
            foreach (GameObject created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }
            _created.Clear();
        }

        [Test]
        public void Bay_RespectsCapacity()
        {
            HangarBay bay = new HangarBay(2, LaunchInterval);

            Assert.IsTrue(bay.TryDock(CreateShip()));
            Assert.IsTrue(bay.TryDock(CreateShip()));
            Assert.IsFalse(bay.TryDock(CreateShip()));
            Assert.IsFalse(bay.HasSpace);
        }

        [Test]
        public void Bay_LaunchesOneShipPerIntervalInRequestOrder()
        {
            HangarBay bay = new HangarBay(4, LaunchInterval);
            ShipAgent first = CreateShip();
            ShipAgent second = CreateShip();
            bay.TryDock(first);
            bay.TryDock(second);

            bay.RequestLaunch(second);
            bay.RequestLaunch(first);
            bay.RequestLaunch(second);

            Assert.AreEqual(2, bay.LaunchQueueCount);
            Assert.AreSame(second, bay.Tick(0.1f, true));
            Assert.IsNull(bay.Tick(LaunchInterval * 0.5f, true));
            Assert.AreSame(first, bay.Tick(LaunchInterval, true));
            Assert.AreEqual(0, bay.Docked.Count);
        }

        [Test]
        public void Bay_HoldsLaunchesWhileNotAllowed()
        {
            HangarBay bay = new HangarBay(4, LaunchInterval);
            ShipAgent ship = CreateShip();
            bay.TryDock(ship);
            bay.RequestLaunch(ship);

            Assert.IsNull(bay.Tick(10f, false));
            Assert.AreSame(ship, bay.Tick(0f, true));
        }

        [Test]
        public void Bay_IgnoresLaunchRequestsOfShipsNotDocked()
        {
            HangarBay bay = new HangarBay(4, LaunchInterval);
            bay.RequestLaunch(CreateShip());

            Assert.AreEqual(0, bay.LaunchQueueCount);
        }

        [Test]
        public void Bay_DropsDestroyedShips()
        {
            HangarBay bay = new HangarBay(1, LaunchInterval);
            ShipAgent ship = CreateShip();
            bay.TryDock(ship);
            bay.RequestLaunch(ship);

            Object.DestroyImmediate(ship.gameObject);

            Assert.IsNull(bay.Tick(1f, true));
            Assert.IsTrue(bay.HasSpace);
        }

        [Test]
        public void Bay_RejectsInvalidConfiguration()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HangarBay(0, LaunchInterval));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HangarBay(2, -1f));
        }

        [Test]
        public void Selection_PrefersHomeHangarThenAnyWithRoom()
        {
            Hangar full = CreateHangar();
            Hangar home = CreateHangar();
            Hangar other = CreateHangar();
            ShipAgent ship = CreateShip();
            ship.SetHomeHangar(home);
            List<Hangar> hangars = new List<Hangar> { full, other, home };

            Assert.AreSame(home, HangarSelection.FindFor(ship, hangars));

            FillHangar(home);
            FillHangar(full);
            Assert.AreSame(other, HangarSelection.FindFor(ship, hangars));

            FillHangar(other);
            Assert.IsNull(HangarSelection.FindFor(ship, hangars));
        }

        void FillHangar(Hangar hangar)
        {
            while (hangar.HasSpace)
            {
                ShipAgent filler = CreateShip();
                hangar.TryDock(filler);
            }
        }

        ShipAgent CreateShip()
        {
            GameObject shipObject = new GameObject("Ship");
            _created.Add(shipObject);
            return shipObject.AddComponent<ShipAgent>();
        }

        Hangar CreateHangar()
        {
            GameObject carrierObject = new GameObject("Carrier");
            _created.Add(carrierObject);
            return carrierObject.AddComponent<Hangar>();
        }
    }
}
