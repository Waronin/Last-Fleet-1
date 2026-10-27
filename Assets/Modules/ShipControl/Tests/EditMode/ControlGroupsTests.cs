using System;
using System.Collections.Generic;
using LastFleet.Ships;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastFleet.ShipControl.Tests
{
    public class ControlGroupsTests
    {
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
        public void Store_KeepsACopyOfTheSelection()
        {
            ControlGroups groups = new ControlGroups();
            List<ShipAgent> selection = new List<ShipAgent> { CreateShip(), CreateShip() };

            groups.Store(1, selection);
            selection.Clear();

            Assert.AreEqual(2, groups.Get(1).Count);
            Assert.AreEqual(0, groups.Get(2).Count);
        }

        [Test]
        public void Store_ReplacesThePreviousGroup()
        {
            ControlGroups groups = new ControlGroups();
            ShipAgent replacement = CreateShip();
            groups.Store(3, new List<ShipAgent> { CreateShip(), CreateShip() });

            groups.Store(3, new List<ShipAgent> { replacement });

            CollectionAssert.AreEqual(new ShipAgent[] { replacement }, groups.Get(3));
        }

        [Test]
        public void LostShips_LeaveEveryGroup()
        {
            ControlGroups groups = new ControlGroups();
            ShipAgent removed = CreateShip();
            ShipAgent destroyed = CreateShip();
            ShipAgent kept = CreateShip();
            groups.Store(1, new List<ShipAgent> { removed, destroyed, kept });
            groups.Store(2, new List<ShipAgent> { removed });

            groups.Remove(removed);
            Object.DestroyImmediate(destroyed.gameObject);

            CollectionAssert.AreEqual(new ShipAgent[] { kept }, groups.Get(1));
            Assert.AreEqual(0, groups.Get(2).Count);
        }

        [Test]
        public void InvalidGroup_IsRejected()
        {
            ControlGroups groups = new ControlGroups();

            Assert.Throws<ArgumentOutOfRangeException>(() => groups.Get(ControlGroups.GroupCount));
            Assert.Throws<ArgumentOutOfRangeException>(() => groups.Store(-1, new List<ShipAgent>()));
        }

        ShipAgent CreateShip()
        {
            GameObject shipObject = new GameObject("Ship");
            _created.Add(shipObject);
            return shipObject.AddComponent<ShipAgent>();
        }
    }
}
