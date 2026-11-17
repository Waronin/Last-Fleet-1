using System;
using System.Collections.Generic;
using LastFleet.Shared.Editor;
using LastFleet.Zones;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace LastFleet.Stations.Editor
{
    /// <summary>
    /// Builds the blockout hulk of each stranded ship class from the fleet's own ship prefab: the same hull
    /// parts, without engines, trails or markers, its windows dark, rolled a little off the plane, with dark
    /// hull breaches and a red distress beacon. Two docking ports sit midship on the zone plane, one per
    /// flank. When the ship is repaired, the real ship appears in its place.
    /// </summary>
    static class StrandedShipPrefabBuilder
    {
        internal const string ShipPrefabFolder = "Assets/Modules/Ships/Prefabs";
        const float RollDegrees = 8f;
        const float PortGapUnits = 0.4f;
        const string EngineGlowName = "Engine Glow";

        static readonly Color BreachColor = new Color(0.05f, 0.05f, 0.06f);
        static readonly Color DeadWindowColor = new Color(0.12f, 0.13f, 0.15f);
        static readonly Color BeaconColor = new Color(3f, 0.25f, 0.15f);

        public static void DeleteAll()
        {
            foreach (StrandedShipClass shipClass in (StrandedShipClass[])Enum.GetValues(typeof(StrandedShipClass)))
            {
                AssetDatabase.DeleteAsset(PrefabPath(shipClass));
            }
        }

        public static StationView LoadOrCreate(StrandedShipClass shipClass)
        {
            string path = PrefabPath(shipClass);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing.GetComponent<StationView>();
            }

            string className = StrandedShipClasses.ShipClassName(shipClass);
            string shipPath = $"{ShipPrefabFolder}/{className}.prefab";
            GameObject ship = AssetDatabase.LoadAssetAtPath<GameObject>(shipPath);
            if (ship == null)
            {
                throw new InvalidOperationException($"The stranded {className} copies the ship prefab at {shipPath}; build the ship prefabs first.");
            }

            GameObject root = new GameObject($"Stranded {className}");
            Transform model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);
            model.localRotation = Quaternion.Euler(0f, 0f, RollDegrees);
            CopyHullParts(ship.transform, model);
            Bounds bounds = ModelBounds(root.transform);
            AddDamage(model, bounds);

            List<Transform> ports = new List<Transform>();
            AddPort(root.transform, ports, -1f, bounds);
            AddPort(root.transform, ports, 1f, bounds);
            StationView view = root.AddComponent<StationView>();
            SerializedObject serialized = new SerializedObject(view);
            EditorAssetUtility.FindProperty(serialized, "_type").enumValueIndex = (int)StationType.StrandedShip;
            EditorAssetUtility.FindProperty(serialized, "_strandedClass").enumValueIndex = (int)shipClass;
            SerializedProperty portList = EditorAssetUtility.FindProperty(serialized, "_dockingPorts");
            portList.arraySize = ports.Count;
            for (int i = 0; i < ports.Count; i++)
            {
                portList.GetArrayElementAtIndex(i).objectReferenceValue = ports[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorAssetUtility.EnsureFolder(StationPrefabBuilder.PrefabFolder);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<StationView>();
        }

        static string PrefabPath(StrandedShipClass shipClass)
        {
            return $"{StationPrefabBuilder.PrefabFolder}/Stranded{shipClass}.prefab";
        }

        /// <summary>Copies every hull mesh of the ship, flattened under the model; engines and unlit markers stay behind.</summary>
        internal static void CopyHullParts(Transform ship, Transform model)
        {
            Material deadWindows = StationPrefabBuilder.LoadOrCreateMaterial("StrandedWindows", StationPrefabBuilder.LitShaderName, DeadWindowColor);
            foreach (MeshRenderer source in ship.GetComponentsInChildren<MeshRenderer>())
            {
                // Hidden effects such as muzzle flashes are switched off in the prefab.
                bool isHidden = !source.enabled || !IsActiveInPrefab(source.transform, ship);
                if (isHidden || IsUnderEngineGlow(source.transform, ship) || !source.TryGetComponent(out MeshFilter sourceFilter))
                {
                    continue;
                }

                GameObject part = new GameObject(source.name);
                part.transform.SetParent(model, false);
                part.transform.localPosition = ship.InverseTransformPoint(source.transform.position);
                part.transform.localRotation = Quaternion.Inverse(ship.rotation) * source.transform.rotation;
                part.transform.localScale = source.transform.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
                MeshRenderer renderer = part.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                // Lit windows and bay lights go dark: there is no power aboard.
                bool isLight = source.sharedMaterial != null && source.sharedMaterial.shader.name == StationPrefabBuilder.UnlitShaderName;
                renderer.sharedMaterial = isLight ? deadWindows : source.sharedMaterial;
            }
        }

        /// <summary>activeInHierarchy is always false on prefab assets, so the chain of activeSelf is checked instead.</summary>
        static bool IsActiveInPrefab(Transform part, Transform shipRoot)
        {
            for (Transform current = part; current != null && current != shipRoot; current = current.parent)
            {
                if (!current.gameObject.activeSelf)
                {
                    return false;
                }
            }
            return true;
        }

        static bool IsUnderEngineGlow(Transform part, Transform shipRoot)
        {
            for (Transform current = part; current != null && current != shipRoot; current = current.parent)
            {
                if (current.name == EngineGlowName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Dark breaches along the hull and a red beacon on top.</summary>
        static void AddDamage(Transform model, Bounds bounds)
        {
            Material breach = StationPrefabBuilder.LoadOrCreateMaterial("StrandedBreach", StationPrefabBuilder.LitShaderName, BreachColor);
            Material beacon = StationPrefabBuilder.LoadOrCreateMaterial("StrandedBeacon", StationPrefabBuilder.UnlitShaderName, BeaconColor);
            float length = bounds.size.z;
            float top = bounds.max.y;
            StationPrefabBuilder.CreatePart(model, PrimitiveType.Cube, "Breach Bow", breach,
                new Vector3(bounds.max.x * 0.6f, top * 0.6f, bounds.center.z + length * 0.25f), new Vector3(0.9f, 0.5f, 1.2f))
                .transform.localRotation = Quaternion.Euler(10f, 25f, 15f);
            StationPrefabBuilder.CreatePart(model, PrimitiveType.Cube, "Breach Midship", breach,
                new Vector3(bounds.min.x * 0.7f, 0f, bounds.center.z - length * 0.05f), new Vector3(0.6f, 0.8f, 1.6f))
                .transform.localRotation = Quaternion.Euler(-5f, -20f, 0f);
            StationPrefabBuilder.CreatePart(model, PrimitiveType.Cube, "Breach Stern", breach,
                new Vector3(0f, top * 0.7f, bounds.center.z - length * 0.3f), new Vector3(1.1f, 0.4f, 0.8f))
                .transform.localRotation = Quaternion.Euler(0f, 40f, 10f);
            StationPrefabBuilder.CreatePart(model, PrimitiveType.Sphere, "Distress Beacon", beacon,
                new Vector3(0f, top + 0.3f, bounds.center.z), Vector3.one * 0.45f);
        }

        /// <summary>A lit frame just off the flank, midship; the port faces away from the hull.</summary>
        internal static void AddPort(Transform root, List<Transform> ports, float side, Bounds bounds)
        {
            float flank = side < 0f ? bounds.min.x : bounds.max.x;
            int number = ports.Count + 1;
            Quaternion rotation = Quaternion.Euler(0f, side * 90f, 0f);
            float frameX = flank + side * (PortGapUnits + StationPrefabBuilder.PortFrameDepthUnits * 0.5f);
            Renderer frame = StationPrefabBuilder.CreatePart(root, PrimitiveType.Cube, $"Port Frame {number}", StationPrefabBuilder.Port(),
                new Vector3(frameX, 0f, bounds.center.z),
                new Vector3(StationPrefabBuilder.PortFrameSizeUnits * 0.5f, StationPrefabBuilder.PortFrameSizeUnits * 0.25f,
                    StationPrefabBuilder.PortFrameDepthUnits));
            frame.transform.localRotation = rotation;
            StationPrefabBuilder.CreatePart(root, PrimitiveType.Cube, $"Port Collar {number}", StationPrefabBuilder.Dark(),
                new Vector3((flank + frameX) * 0.5f, 0f, bounds.center.z), new Vector3(Mathf.Abs(frameX - flank), 0.4f, 0.6f));

            Transform port = new GameObject($"Docking Port {number}").transform;
            port.SetParent(root, false);
            port.localPosition = new Vector3(frameX + side * StationPrefabBuilder.PortFrameDepthUnits * 0.5f, 0f, bounds.center.z);
            port.localRotation = rotation;
            ports.Add(port);
        }

        internal static Bounds ModelBounds(Transform root)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                {
                    Vector3 local = root.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    if (first)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        first = false;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }
            return bounds;
        }
    }
}
