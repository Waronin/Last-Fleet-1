using System;
using System.Collections.Generic;
using LastFleet.Shared.Editor;
using LastFleet.Zones;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastFleet.Stations.Editor
{
    /// <summary>
    /// Builds the blockout derelict of a wreck field: the fleet's cruiser hull, dark and broken in two, the bow
    /// section knocked askew, with burnt breaches and no beacon, since nobody aboard is left to call for help.
    /// Two docking ports sit on the stern section's flanks on the zone plane.
    /// </summary>
    static class DerelictPrefabBuilder
    {
        const string SourceShipName = "Cruiser";
        const float SternRollDegrees = 14f;
        const float BowYawDegrees = 22f;
        const float BowRollDegrees = -18f;
        const float BowGapUnits = 1.2f;

        static readonly Color BurnColor = new Color(0.04f, 0.035f, 0.03f);

        static string PrefabPath => $"{StationPrefabBuilder.PrefabFolder}/Derelict.prefab";

        public static void Delete()
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        public static StationView LoadOrCreate()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                return existing.GetComponent<StationView>();
            }

            string shipPath = $"{StrandedShipPrefabBuilder.ShipPrefabFolder}/{SourceShipName}.prefab";
            GameObject ship = AssetDatabase.LoadAssetAtPath<GameObject>(shipPath);
            if (ship == null)
            {
                throw new InvalidOperationException($"The derelict copies the ship prefab at {shipPath}; build the ship prefabs first.");
            }

            GameObject root = new GameObject("Derelict");
            Transform stern = new GameObject("Stern Section").transform;
            stern.SetParent(root.transform, false);
            StrandedShipPrefabBuilder.CopyHullParts(ship.transform, stern);
            Bounds intact = StrandedShipPrefabBuilder.ModelBounds(root.transform);
            Transform bow = BreakOffBow(stern, root.transform, intact);
            stern.localRotation = Quaternion.Euler(0f, 0f, SternRollDegrees);
            AddBurns(stern, bow, intact);

            Bounds sternBounds = StrandedShipPrefabBuilder.ModelBounds(stern);
            List<Transform> ports = new List<Transform>();
            StrandedShipPrefabBuilder.AddPort(root.transform, ports, -1f, sternBounds);
            StrandedShipPrefabBuilder.AddPort(root.transform, ports, 1f, sternBounds);

            StationView view = root.AddComponent<StationView>();
            SerializedObject serialized = new SerializedObject(view);
            EditorAssetUtility.FindProperty(serialized, "_type").enumValueIndex = (int)StationType.Derelict;
            SerializedProperty portList = EditorAssetUtility.FindProperty(serialized, "_dockingPorts");
            portList.arraySize = ports.Count;
            for (int i = 0; i < ports.Count; i++)
            {
                portList.GetArrayElementAtIndex(i).objectReferenceValue = ports[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorAssetUtility.EnsureFolder(StationPrefabBuilder.PrefabFolder);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<StationView>();
        }

        /// <summary>Moves every part in front of the break line into its own section, pushed forward and knocked askew.</summary>
        static Transform BreakOffBow(Transform stern, Transform root, Bounds intact)
        {
            float breakZ = intact.center.z + intact.size.z * 0.1f;
            Transform bow = new GameObject("Bow Section").transform;
            bow.SetParent(root, false);
            bow.localPosition = new Vector3(0f, 0f, breakZ);

            List<Transform> bowParts = new List<Transform>();
            foreach (Transform part in stern)
            {
                if (part.localPosition.z > breakZ)
                {
                    bowParts.Add(part);
                }
            }
            foreach (Transform part in bowParts)
            {
                part.SetParent(bow, true);
            }

            bow.localPosition = new Vector3(intact.size.x * 0.25f, 0f, breakZ + BowGapUnits);
            bow.localRotation = Quaternion.Euler(0f, BowYawDegrees, BowRollDegrees);
            return bow;
        }

        /// <summary>Burnt breaches on both sections and a jagged edge at the break.</summary>
        static void AddBurns(Transform stern, Transform bow, Bounds intact)
        {
            Material burn = StationPrefabBuilder.LoadOrCreateMaterial("DerelictBurn", StationPrefabBuilder.LitShaderName, BurnColor);
            float length = intact.size.z;
            float top = intact.max.y;
            StationPrefabBuilder.CreatePart(stern, PrimitiveType.Cube, "Burn Midship", burn,
                new Vector3(intact.max.x * 0.7f, top * 0.5f, intact.center.z - length * 0.15f), new Vector3(0.8f, 0.7f, 1.8f))
                .transform.localRotation = Quaternion.Euler(5f, 20f, 10f);
            StationPrefabBuilder.CreatePart(stern, PrimitiveType.Cube, "Burn Stern", burn,
                new Vector3(intact.min.x * 0.6f, top * 0.6f, intact.center.z - length * 0.35f), new Vector3(1f, 0.5f, 1.1f))
                .transform.localRotation = Quaternion.Euler(-10f, -30f, 5f);
            StationPrefabBuilder.CreatePart(stern, PrimitiveType.Cube, "Break Edge", burn,
                new Vector3(0f, 0f, intact.center.z + length * 0.1f), new Vector3(intact.size.x * 0.8f, intact.size.y * 0.8f, 0.3f))
                .transform.localRotation = Quaternion.Euler(8f, 12f, 25f);
            StationPrefabBuilder.CreatePart(bow, PrimitiveType.Cube, "Burn Bow", burn,
                new Vector3(intact.min.x * 0.5f, top * 0.4f, length * 0.15f), new Vector3(0.7f, 0.6f, 1.2f))
                .transform.localRotation = Quaternion.Euler(15f, -15f, 20f);
        }
    }
}
