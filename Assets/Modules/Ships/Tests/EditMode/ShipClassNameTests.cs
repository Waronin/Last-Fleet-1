using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastFleet.Ships.Tests
{
    public class ShipClassNameTests
    {
        const string ModulesFolder = "Assets/Modules";

        [Test]
        public void EveryShipPrefab_HasADeclaredDisplayName()
        {
            List<string> undeclared = new List<string>();
            int ships = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new string[] { ModulesFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || !prefab.TryGetComponent(out ShipAgent ship))
                {
                    continue;
                }
                ships++;
                if (!ShipTexts.Classes.Contains(ship.ClassName))
                {
                    undeclared.Add($"{path}: {ship.ClassName}");
                }
            }

            Assert.Greater(ships, 0, "No ship prefabs were found.");
            Assert.IsEmpty(undeclared, "Add these classes to ShipTexts.Classes:\n" + string.Join("\n", undeclared));
        }
    }
}
