using System.Collections.Generic;
using LastFleet.Shared.Editor;
using LastFleet.Zones;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastFleet.Stations.Editor
{
    /// <summary>
    /// Creates the blockout station prefabs at their real size and the catalog that maps types to
    /// them. Stations are 3–6 times the size of capital ships (1 unit = 100 m): outpost about 3 km,
    /// research station 5 km, industrial station 7 km, habitat ring 9 km. Every model stays inside
    /// its type's footprint, and every docking port sits on the zone plane at the end of an arm.
    /// Menu: Last Fleet / Rebuild Station Prefabs.
    /// </summary>
    public static class StationPrefabBuilder
    {
        const string ModuleRoot = "Assets/Modules/Stations";
        const string PrefabFolder = ModuleRoot + "/Prefabs";
        const string DataFolder = ModuleRoot + "/Data";
        const string MaterialFolder = ModuleRoot + "/Graphics/Blockout";
        const string CatalogPath = DataFolder + "/StationPrefabs.asset";
        const string LitShaderName = "Universal Render Pipeline/Lit";
        const string UnlitShaderName = "Universal Render Pipeline/Unlit";
        const float ArmThicknessUnits = 1.2f;
        const float PortFrameSizeUnits = 2.4f;
        const float PortFrameDepthUnits = 0.3f;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        static readonly Color HullColor = new Color(0.62f, 0.64f, 0.68f);
        static readonly Color DarkColor = new Color(0.28f, 0.3f, 0.34f);
        static readonly Color SolarColor = new Color(0.12f, 0.18f, 0.38f);
        static readonly Color RustColor = new Color(0.5f, 0.3f, 0.2f);
        static readonly Color WarningColor = new Color(0.9f, 0.75f, 0.1f);
        static readonly Color WindowColor = new Color(2f, 1.8f, 1.3f);
        static readonly Color PortColor = new Color(0.3f, 1.5f, 2f);

        [MenuItem("Last Fleet/Rebuild Station Prefabs")]
        public static void RebuildAll()
        {
            foreach (StationType type in new[] { StationType.Outpost, StationType.Research, StationType.Industrial, StationType.Habitat })
            {
                AssetDatabase.DeleteAsset(PrefabPath(type));
            }
            LoadOrCreateCatalog();
        }

        /// <summary>The catalog with one prefab per type; missing prefabs are built first.</summary>
        public static StationPrefabCatalog LoadOrCreateCatalog()
        {
            List<StationView> prefabs = new List<StationView>
            {
                LoadOrCreate(StationType.Outpost, BuildOutpost),
                LoadOrCreate(StationType.Research, BuildResearchStation),
                LoadOrCreate(StationType.Industrial, BuildIndustrialStation),
                LoadOrCreate(StationType.Habitat, BuildHabitatRing)
            };

            StationPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<StationPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                EditorAssetUtility.EnsureFolder(DataFolder);
                catalog = ScriptableObject.CreateInstance<StationPrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty list = EditorAssetUtility.FindProperty(serialized, "_prefabs");
            list.arraySize = prefabs.Count;
            for (int i = 0; i < prefabs.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return catalog;
        }

        static StationView LoadOrCreate(StationType type, System.Action<Transform, List<Transform>> build)
        {
            string path = PrefabPath(type);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing.GetComponent<StationView>();
            }

            GameObject root = new GameObject($"{type} Station");
            List<Transform> ports = new List<Transform>();
            build(root.transform, ports);

            StationView view = root.AddComponent<StationView>();
            SerializedObject serialized = new SerializedObject(view);
            EditorAssetUtility.FindProperty(serialized, "_type").enumValueIndex = (int)type;
            SerializedProperty portList = EditorAssetUtility.FindProperty(serialized, "_dockingPorts");
            portList.arraySize = ports.Count;
            for (int i = 0; i < ports.Count; i++)
            {
                portList.GetArrayElementAtIndex(i).objectReferenceValue = ports[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorAssetUtility.EnsureFolder(PrefabFolder);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<StationView>();
        }

        static string PrefabPath(StationType type)
        {
            return $"{PrefabFolder}/{type}Station.prefab";
        }

        /// <summary>About 3 km: a squat hub with a dome, an antenna, two solar wings and two docking arms.</summary>
        static void BuildOutpost(Transform root, List<Transform> ports)
        {
            CreatePart(root, PrimitiveType.Cylinder, "Hub", Hull(), new Vector3(0f, 0f, 0f), new Vector3(10f, 1.5f, 10f));
            CreatePart(root, PrimitiveType.Sphere, "Dome", Dark(), new Vector3(0f, 1.5f, 0f), new Vector3(7f, 3.5f, 7f));
            CreatePart(root, PrimitiveType.Cylinder, "Antenna", Hull(), new Vector3(0f, 5f, 0f), new Vector3(0.4f, 3f, 0.4f));
            CreatePart(root, PrimitiveType.Cube, "Windows", Windows(), new Vector3(0f, 0.2f, 0f), new Vector3(10.1f, 0.3f, 2f));
            foreach (float side in new[] { -1f, 1f })
            {
                CreatePart(root, PrimitiveType.Cube, side < 0f ? "Solar Port" : "Solar Starboard", Solar(),
                    new Vector3(side * 10f, 0.5f, 0f), new Vector3(9f, 0.2f, 4f));
            }
            AddDockingArm(root, ports, 0f, 5f, 7f);
            AddDockingArm(root, ports, 180f, 5f, 7f);
        }

        /// <summary>About 5 km: a central sphere, a segmented ring on three spokes and three docking arms.</summary>
        static void BuildResearchStation(Transform root, List<Transform> ports)
        {
            const float ringRadius = 20f;
            CreatePart(root, PrimitiveType.Sphere, "Core", Hull(), Vector3.zero, Vector3.one * 14f);
            CreatePart(root, PrimitiveType.Sphere, "Observatory", Dark(), new Vector3(0f, 7f, 0f), new Vector3(8f, 3f, 8f));
            CreateRing(root, "Ring", ringRadius, 12, new Vector3(10.5f, 2.5f, 3f), Hull(), Windows());
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f + 60f;
                CreateSpoke(root, $"Spoke {i + 1}", angle, 6.5f, ringRadius - 1.5f, 1.2f, Dark());
                AddDockingArm(root, ports, i * 120f, ringRadius + 1.5f, 2f);
            }
        }

        /// <summary>About 7 km: a long truss with tanks, rusty modules, warning stripes and four docking arms.</summary>
        static void BuildIndustrialStation(Transform root, List<Transform> ports)
        {
            Material rust = LoadOrCreateMaterial("StationRust", LitShaderName, RustColor);
            Material warning = LoadOrCreateMaterial("StationWarning", LitShaderName, WarningColor);
            CreatePart(root, PrimitiveType.Cube, "Truss", Dark(), Vector3.zero, new Vector3(60f, 3f, 3f));
            for (int i = 0; i < 4; i++)
            {
                float x = -24f + i * 16f;
                Renderer tank = CreatePart(root, PrimitiveType.Cylinder, $"Tank {i + 1}", rust, new Vector3(x, 0f, 0f), new Vector3(7f, 4f, 7f));
                tank.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                CreatePart(root, PrimitiveType.Cube, $"Warning {i + 1}", warning, new Vector3(x, 0f, 0f), new Vector3(1f, 7.2f, 7.2f));
            }
            CreatePart(root, PrimitiveType.Cube, "Processing Block", Hull(), new Vector3(-8f, 3.5f, 0f), new Vector3(12f, 5f, 8f));
            CreatePart(root, PrimitiveType.Cube, "Control Block", Hull(), new Vector3(20f, 3f, 0f), new Vector3(6f, 4f, 5f));
            CreatePart(root, PrimitiveType.Cube, "Control Windows", Windows(), new Vector3(20f, 3.5f, 2.55f), new Vector3(5f, 0.6f, 0.1f));
            foreach (float x in new[] { -16f, 16f })
            {
                AddDockingArm(root, ports, 0f, 4f, 6f, new Vector3(x, 0f, 0f));
                AddDockingArm(root, ports, 180f, 4f, 6f, new Vector3(x, 0f, 0f));
            }
        }

        /// <summary>About 9 km: a lit habitat ring on four spokes around a tall hub, with six docking arms.</summary>
        static void BuildHabitatRing(Transform root, List<Transform> ports)
        {
            const float ringRadius = 40f;
            CreatePart(root, PrimitiveType.Cylinder, "Hub", Hull(), Vector3.zero, new Vector3(12f, 8f, 12f));
            CreatePart(root, PrimitiveType.Sphere, "Hub Cap", Dark(), new Vector3(0f, 8f, 0f), new Vector3(10f, 4f, 10f));
            CreateRing(root, "Ring", ringRadius, 32, new Vector3(8.5f, 4f, 5f), Hull(), Windows());
            for (int i = 0; i < 4; i++)
            {
                CreateSpoke(root, $"Spoke {i + 1}", i * 90f + 45f, 6f, ringRadius - 2.5f, 2f, Dark());
            }
            for (int i = 0; i < 6; i++)
            {
                AddDockingArm(root, ports, i * 60f, ringRadius + 2.5f, 1.5f);
            }
        }

        /// <summary>Segments around a circle on the zone plane, each with a window strip on its outer face.</summary>
        static void CreateRing(Transform root, string name, float radius, int segments, Vector3 segmentScale, Material hull, Material windows)
        {
            Transform ring = new GameObject(name).transform;
            ring.SetParent(root, false);
            for (int i = 0; i < segments; i++)
            {
                Quaternion rotation = Quaternion.Euler(0f, i * 360f / segments, 0f);
                Vector3 outward = rotation * Vector3.forward;
                Renderer segment = CreatePart(ring, PrimitiveType.Cube, $"Segment {i + 1}", hull, outward * radius, segmentScale);
                segment.transform.localRotation = rotation;
                Renderer strip = CreatePart(ring, PrimitiveType.Cube, $"Windows {i + 1}", windows,
                    outward * (radius + segmentScale.z * 0.5f + 0.05f), new Vector3(segmentScale.x * 0.9f, segmentScale.y * 0.25f, 0.1f));
                strip.transform.localRotation = rotation;
            }
        }

        static void CreateSpoke(Transform root, string name, float angleDegrees, float innerRadius, float outerRadius, float thickness, Material material)
        {
            Quaternion rotation = Quaternion.Euler(0f, angleDegrees, 0f);
            float length = outerRadius - innerRadius;
            Renderer spoke = CreatePart(root, PrimitiveType.Cube, name, material, rotation * Vector3.forward * (innerRadius + length * 0.5f),
                new Vector3(thickness, thickness, length));
            spoke.transform.localRotation = rotation;
        }

        /// <summary>
        /// An arm reaching out on the zone plane with a lit port frame at its tip. The port transform sits
        /// on the frame's outer face, where a ship's airlock meets it, with its forward axis pointing away
        /// from the station.
        /// </summary>
        static void AddDockingArm(Transform root, List<Transform> ports, float angleDegrees, float startRadius, float length)
        {
            AddDockingArm(root, ports, angleDegrees, startRadius, length, Vector3.zero);
        }

        static void AddDockingArm(Transform root, List<Transform> ports, float angleDegrees, float startRadius, float length, Vector3 origin)
        {
            Quaternion rotation = Quaternion.Euler(0f, angleDegrees, 0f);
            Vector3 outward = rotation * Vector3.forward;
            int number = ports.Count + 1;
            Renderer arm = CreatePart(root, PrimitiveType.Cube, $"Docking Arm {number}", Dark(),
                origin + outward * (startRadius + length * 0.5f), new Vector3(ArmThicknessUnits, ArmThicknessUnits, length));
            arm.transform.localRotation = rotation;
            Renderer frame = CreatePart(root, PrimitiveType.Cube, $"Port Frame {number}", Port(),
                origin + outward * (startRadius + length), new Vector3(PortFrameSizeUnits, PortFrameSizeUnits * 0.5f, PortFrameDepthUnits));
            frame.transform.localRotation = rotation;

            Transform port = new GameObject($"Docking Port {number}").transform;
            port.SetParent(root, false);
            port.localPosition = origin + outward * (startRadius + length + PortFrameDepthUnits * 0.5f);
            port.localRotation = rotation;
            ports.Add(port);
        }

        static Material Hull()
        {
            return LoadOrCreateMaterial("StationHull", LitShaderName, HullColor);
        }

        static Material Dark()
        {
            return LoadOrCreateMaterial("StationDark", LitShaderName, DarkColor);
        }

        static Material Solar()
        {
            return LoadOrCreateMaterial("StationSolar", LitShaderName, SolarColor);
        }

        static Material Windows()
        {
            return LoadOrCreateMaterial("StationWindows", UnlitShaderName, WindowColor);
        }

        static Material Port()
        {
            return LoadOrCreateMaterial("StationPort", UnlitShaderName, PortColor);
        }

        static Material LoadOrCreateMaterial(string name, string shaderName, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            bool isNew = AssetDatabase.LoadAssetAtPath<Material>(path) == null;
            Material material = EditorAssetUtility.LoadOrCreateMaterial(path, shaderName);
            if (isNew)
            {
                material.SetColor(BaseColorId, color);
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        static Renderer CreatePart(Transform parent, PrimitiveType type, string name, Material material, Vector3 localPosition, Vector3 localScale)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return renderer;
        }
    }
}
