using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastFleet.Ships.Tests
{
    public class SquadRosterTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void DestroyShips()
        {
            foreach (GameObject shipObject in _created)
            {
                Object.DestroyImmediate(shipObject);
            }
            _created.Clear();
        }

        [Test]
        public void Form_NumbersSquadsPerClass()
        {
            SquadRoster roster = new SquadRoster();

            Squad first = roster.Form(new List<ShipAgent> { CreateShip() });
            Squad second = roster.Form(new List<ShipAgent> { CreateShip() });
            Squad fighters = roster.Form(new List<ShipAgent> { CreateShip("Fighter") });

            Assert.AreEqual(1, first.Number);
            Assert.AreEqual(2, second.Number);
            Assert.AreEqual(1, fighters.Number);
            Assert.AreEqual("Harvester Squad 2", second.Name);
        }

        [Test]
        public void Form_TakesShipsOutOfOldSquadsAndDissolvesEmptyOnes()
        {
            SquadRoster roster = new SquadRoster();
            ShipAgent a = CreateShip();
            ShipAgent b = CreateShip();
            ShipAgent c = CreateShip();
            Squad old = roster.Form(new List<ShipAgent> { a, b });
            Squad single = roster.Form(new List<ShipAgent> { c });

            Squad merged = roster.Form(new List<ShipAgent> { b, c });

            CollectionAssert.AreEqual(new ShipAgent[] { a }, old.Members);
            CollectionAssert.DoesNotContain(roster.Squads, single);
            CollectionAssert.AreEqual(new ShipAgent[] { b, c }, merged.Members);
            Assert.IsTrue(roster.TryGetSquad(c, out Squad squadOfC));
            Assert.AreSame(merged, squadOfC);
        }

        [Test]
        public void Remove_DropsLostShipAndDissolvesEmptySquad()
        {
            SquadRoster roster = new SquadRoster();
            ShipAgent leader = CreateShip();
            ShipAgent wingman = CreateShip();
            Squad squad = roster.Form(new List<ShipAgent> { leader, wingman });

            roster.Remove(leader);
            Assert.AreSame(wingman, squad.Leader);

            roster.Remove(wingman);
            CollectionAssert.IsEmpty(roster.Squads);
            Assert.IsFalse(roster.TryGetSquad(wingman, out Squad _));
        }

        [Test]
        public void Form_RejectsMixedClassesAndEmptyLists()
        {
            SquadRoster roster = new SquadRoster();

            Assert.Throws<ArgumentException>(() => roster.Form(new List<ShipAgent> { CreateShip(), CreateShip("Fighter") }));
            Assert.Throws<ArgumentException>(() => roster.Form(new List<ShipAgent>()));
            CollectionAssert.IsEmpty(roster.Squads);
        }

        ShipAgent CreateShip(string className = "Harvester")
        {
            GameObject shipObject = new GameObject(className);
            _created.Add(shipObject);
            ShipAgent ship = shipObject.AddComponent<ShipAgent>();

            SerializedObject serialized = new SerializedObject(ship);
            serialized.FindProperty("_className").stringValue = className;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return ship;
        }
    }
}
