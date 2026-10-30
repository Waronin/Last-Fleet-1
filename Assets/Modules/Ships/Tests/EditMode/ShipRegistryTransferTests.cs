using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastFleet.Ships.Tests
{
    public class ShipRegistryTransferTests
    {
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
        public void Register_GivesTheRegistrysSide()
        {
            ShipRegistry pronin = CreateRegistry(Allegiance.Pronin);
            ShipAgent ship = CreateShip();

            pronin.Register(ship);

            Assert.AreEqual(Allegiance.Pronin, ship.Allegiance);
            Assert.IsTrue(pronin.Contains(ship));
        }

        [Test]
        public void TransferTo_MovesTheShipAndReportsItOnBothSides()
        {
            ShipRegistry fleet = CreateRegistry(Allegiance.HumanFleet);
            ShipRegistry pronin = CreateRegistry(Allegiance.Pronin);
            ShipAgent ship = CreateShip();
            fleet.Register(ship);
            ShipAgent removed = null;
            ShipAgent added = null;
            fleet.ShipRemoved += s => removed = s;
            pronin.ShipSpawned += s => added = s;

            fleet.TransferTo(ship, pronin);

            Assert.AreSame(ship, removed);
            Assert.AreSame(ship, added);
            Assert.IsFalse(fleet.Contains(ship));
            Assert.IsTrue(pronin.Contains(ship));
            Assert.AreEqual(Allegiance.Pronin, ship.Allegiance);
        }

        [Test]
        public void TransferTo_TakesTheShipOutOfItsSquadAndRenumbersIt()
        {
            ShipRegistry fleet = CreateRegistry(Allegiance.HumanFleet);
            ShipRegistry pronin = CreateRegistry(Allegiance.Pronin);
            ShipAgent first = CreateShip();
            ShipAgent second = CreateShip();
            fleet.Register(first);
            fleet.Register(second);
            fleet.Squads.Form(new List<ShipAgent> { first, second });

            fleet.TransferTo(second, pronin);

            Assert.IsFalse(fleet.Squads.TryGetSquad(second, out Squad _));
            Assert.IsTrue(fleet.Squads.TryGetSquad(first, out Squad squad));
            Assert.AreEqual(1, squad.Members.Count);
            Assert.AreEqual(1, second.SerialNumber);
        }

        [Test]
        public void TransferTo_DropsOrdersSoAutomationsStop()
        {
            ShipRegistry fleet = CreateRegistry(Allegiance.HumanFleet);
            ShipRegistry pronin = CreateRegistry(Allegiance.Pronin);
            ShipAgent ship = CreateShip();
            fleet.Register(ship);
            bool ordersReplaced = false;
            ship.OrdersReplaced += s => ordersReplaced = true;

            fleet.TransferTo(ship, pronin);

            Assert.IsTrue(ordersReplaced);
        }

        [Test]
        public void TransferTo_RejectsShipsOfOtherRegistries()
        {
            ShipRegistry fleet = CreateRegistry(Allegiance.HumanFleet);
            ShipRegistry pronin = CreateRegistry(Allegiance.Pronin);
            ShipAgent ship = CreateShip();
            pronin.Register(ship);

            Assert.Throws<InvalidOperationException>(() => fleet.TransferTo(ship, pronin));
            Assert.Throws<ArgumentException>(() => pronin.TransferTo(ship, pronin));
        }

        ShipRegistry CreateRegistry(Allegiance allegiance)
        {
            GameObject registryObject = new GameObject($"{allegiance} Registry");
            _created.Add(registryObject);
            ShipRegistry registry = registryObject.AddComponent<ShipRegistry>();
            SerializedObject serialized = new SerializedObject(registry);
            serialized.FindProperty("_allegiance").enumValueIndex = (int)allegiance;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return registry;
        }

        ShipAgent CreateShip()
        {
            GameObject shipObject = new GameObject("Ship");
            _created.Add(shipObject);
            return shipObject.AddComponent<ShipAgent>();
        }
    }
}
