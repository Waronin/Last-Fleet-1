using System;
using System.Collections.Generic;
using LastFleet.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastFleet.Stations.Tests
{
    /// <summary>
    /// Keeps the station prefabs in line with the station types the zone generator relies on: one
    /// model per type, inside its footprint, with the declared number of reachable docking ports.
    /// </summary>
    public class StationPrefabTests
    {
        const string CatalogPath = "Assets/Modules/Stations/Data/StationPrefabs.asset";
        const float PlaneToleranceUnits = 0.01f;

        [Test]
        public void Catalog_HasOnePrefabPerTypeAndStrandedShipClass()
        {
            StationPrefabCatalog catalog = LoadCatalog();
            foreach (StationType type in (StationType[])Enum.GetValues(typeof(StationType)))
            {
                if (type == StationType.StrandedShip)
                {
                    continue;
                }
                ZoneStation station = new ZoneStation("Test", type, Vector3.zero, 0f, AbandonedProfile());
                Assert.IsTrue(catalog.TryGetPrefab(station, out StationView prefab), $"No prefab for {type}.");
                Assert.AreEqual(type, prefab.Type);
            }
            foreach (StrandedShipClass shipClass in (StrandedShipClass[])Enum.GetValues(typeof(StrandedShipClass)))
            {
                ZoneStation hulk = new ZoneStation("Test", Vector3.zero, 0f, AbandonedProfile(), new StrandedHull(shipClass, 0.3f));
                Assert.IsTrue(catalog.TryGetPrefab(hulk, out StationView prefab), $"No prefab for a stranded {shipClass}.");
                Assert.AreEqual(StationType.StrandedShip, prefab.Type);
                Assert.AreEqual(shipClass, prefab.StrandedClass);
            }
        }

        static StationProfile AbandonedProfile()
        {
            return new StationProfile(StationAtmosphere.Breathable, 0, StationAttitude.None, StationCondition.Damaged, StationRadiation.None);
        }

        [Test]
        public void EveryModel_StaysInsideItsFootprint()
        {
            List<string> failures = new List<string>();
            foreach (StationView prefab in AllPrefabs())
            {
                float footprint = FootprintOf(prefab);
                float extent = PlanarExtent(prefab.transform);
                if (extent > footprint)
                {
                    failures.Add($"{NameOf(prefab)}: model reaches {extent:0.0} units, footprint is {footprint:0.0}");
                }
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void EveryModel_HasItsDockingPortsOnThePlane_FacingOutward()
        {
            foreach (StationView prefab in AllPrefabs())
            {
                Assert.AreEqual(PortCountOf(prefab), prefab.DockingPorts.Count, $"{NameOf(prefab)} port count");
                foreach (Transform port in prefab.DockingPorts)
                {
                    Assert.IsNotNull(port, $"{NameOf(prefab)} has an empty port slot.");
                    Vector3 local = prefab.transform.InverseTransformPoint(port.position);
                    Vector3 forward = prefab.transform.InverseTransformDirection(port.forward);
                    Assert.AreEqual(0f, local.y, PlaneToleranceUnits, $"{NameOf(prefab)} {port.name} is off the plane.");
                    Assert.Greater(Vector2.Dot(new Vector2(local.x, local.z), new Vector2(forward.x, forward.z)), 0f,
                        $"{NameOf(prefab)} {port.name} faces into the station.");
                }
            }
        }

        static StationPrefabCatalog LoadCatalog()
        {
            StationPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<StationPrefabCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, $"Station catalog missing at {CatalogPath}.");
            return catalog;
        }

        static IReadOnlyList<StationView> AllPrefabs()
        {
            return LoadCatalog().Prefabs;
        }

        static float FootprintOf(StationView prefab)
        {
            return prefab.Type == StationType.StrandedShip
                ? StrandedShipClasses.FootprintRadiusUnits(prefab.StrandedClass)
                : StationTypes.FootprintRadiusUnits(prefab.Type);
        }

        static int PortCountOf(StationView prefab)
        {
            return prefab.Type == StationType.StrandedShip ? StrandedShipClasses.DockingPortCount : StationTypes.DockingPortCount(prefab.Type);
        }

        static string NameOf(StationView prefab)
        {
            return prefab.Type == StationType.StrandedShip ? $"Stranded {prefab.StrandedClass}" : prefab.Type.ToString();
        }

        /// <summary>Largest distance from the station center on the zone plane over every mesh vertex.</summary>
        static float PlanarExtent(Transform root)
        {
            float extent = 0f;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                {
                    Vector3 local = root.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    extent = Mathf.Max(extent, new Vector2(local.x, local.z).magnitude);
                }
            }
            return extent;
        }
    }
}
