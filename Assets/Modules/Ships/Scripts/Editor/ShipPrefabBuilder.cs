using LastFleet.Exploration;
using LastFleet.Mining;
using LastFleet.Shared.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastFleet.Ships.Editor
{
    /// <summary>
    /// Creates the blockout ship prefabs with their movement profiles and materials.
    /// Game scale (GDD): a harvester looks like about 200 m (2 units), the refinery about 1 km (10 units).
    /// Menu: Last Fleet / Rebuild Ship Prefabs.
    /// </summary>
    public static class ShipPrefabBuilder
    {
        const string ModuleRoot = "Assets/Modules/Ships";
        const string PrefabFolder = ModuleRoot + "/Prefabs";
        const string DataFolder = ModuleRoot + "/Data";
        const string MaterialFolder = ModuleRoot + "/Graphics/Blockout";
        const string HarvesterPrefabPath = PrefabFolder + "/Harvester.prefab";
        const string RefineryPrefabPath = PrefabFolder + "/Refinery.prefab";
        const string LitShaderName = "Universal Render Pipeline/Lit";
        const string UnlitShaderName = "Universal Render Pipeline/Unlit";
        const string ParticleShaderName = "Universal Render Pipeline/Particles/Unlit";
        const float HarvesterHull = 100f;
        const float RefineryHull = 2000f;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // Industrial design language (GDD): angular, yellow-black warning colors, rust, warm orange engines.
        static readonly Color HarvesterHullColor = new Color(0.85f, 0.7f, 0.2f);
        static readonly Color CargoColor = new Color(0.25f, 0.25f, 0.28f);
        static readonly Color RefineryHullColor = new Color(0.32f, 0.32f, 0.34f);
        static readonly Color RustColor = new Color(0.5f, 0.3f, 0.2f);
        static readonly Color WarningColor = new Color(0.9f, 0.75f, 0.1f);
        static readonly Color BayColor = new Color(0.3f, 1.5f, 2f);
        static readonly Color EngineColor = new Color(4f, 1.8f, 0.5f);
        static readonly Color TrailColor = new Color(1f, 0.5f, 0.15f);

        [MenuItem("Last Fleet/Rebuild Ship Prefabs")]
        public static void RebuildAll()
        {
            AssetDatabase.DeleteAsset(HarvesterPrefabPath);
            AssetDatabase.DeleteAsset(RefineryPrefabPath);
            LoadOrCreateHarvester();
            LoadOrCreateRefinery();
        }

        public static ShipAgent LoadOrCreateHarvester()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(HarvesterPrefabPath);
            if (existing != null)
            {
                return existing.GetComponent<ShipAgent>();
            }

            GameObject root = new GameObject("Harvester");
            Material hull = LoadOrCreateMaterial("HarvesterHull", LitShaderName, HarvesterHullColor);
            Material cargo = LoadOrCreateMaterial("HarvesterCargo", LitShaderName, CargoColor);

            // Everything visible sits under Model, so mining can clamp the model onto a rock while the
            // ship's gameplay position stays at its mining slot.
            Transform model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);
            CreatePart(model, PrimitiveType.Cube, "Hull", hull, new Vector3(0f, 0f, 0.2f), new Vector3(0.6f, 0.4f, 1.6f));
            CreatePart(model, PrimitiveType.Cube, "Cargo Left", cargo, new Vector3(-0.5f, 0f, -0.1f), new Vector3(0.4f, 0.35f, 1f));
            CreatePart(model, PrimitiveType.Cube, "Cargo Right", cargo, new Vector3(0.5f, 0f, -0.1f), new Vector3(0.4f, 0.35f, 1f));
            CreatePart(model, PrimitiveType.Cube, "Clamp Front", cargo, new Vector3(0f, -0.28f, 0.6f), new Vector3(0.5f, 0.16f, 0.12f));
            CreatePart(model, PrimitiveType.Cube, "Clamp Rear", cargo, new Vector3(0f, -0.28f, -0.3f), new Vector3(0.5f, 0.16f, 0.12f));

            Transform engineGlow = CreatePart(model, PrimitiveType.Sphere, "Engine Glow", Engine(),
                new Vector3(0f, 0f, -0.75f), Vector3.one * 0.35f).transform;
            TrailRenderer trail = CreateTrail(model, new Vector3(0f, 0f, -0.8f), 0.3f, 4f);
            ParticleSystem debris = CreateDebrisParticles(model, new Vector3(0f, -0.35f, 0.15f));
            LineRenderer bracket = CreateBracket(root.transform);

            ShipAgent agent = root.AddComponent<ShipAgent>();
            ShipMovementProfile profile = LoadOrCreateProfile("HarvesterMovement", null);
            EditorAssetUtility.SetReferences(agent, ("_movementProfile", profile));
            AddPresentation(root, engineGlow, trail, bracket, 22f, 1f);
            EditorAssetUtility.SetReferences(root.GetComponent<ShipPresentation>(), ("_model", model));
            AddDurability(root, HarvesterHull);
            root.AddComponent<HarvesterMining>();
            root.AddComponent<ShipScanner>();
            MiningAttachment attachment = root.AddComponent<MiningAttachment>();
            EditorAssetUtility.SetReferences(attachment, ("_model", model), ("_debris", debris));
            return SavePrefab(root, HarvesterPrefabPath);
        }

        public static ShipAgent LoadOrCreateRefinery()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(RefineryPrefabPath);
            if (existing != null)
            {
                return existing.GetComponent<ShipAgent>();
            }

            GameObject root = new GameObject("Refinery");
            Material hull = LoadOrCreateMaterial("RefineryHull", LitShaderName, RefineryHullColor);
            Material rust = LoadOrCreateMaterial("RefineryRust", LitShaderName, RustColor);
            Material warning = LoadOrCreateMaterial("RefineryWarning", LitShaderName, WarningColor);
            Material bay = LoadOrCreateMaterial("HangarBay", UnlitShaderName, BayColor);

            CreatePart(root.transform, PrimitiveType.Cube, "Hull", hull, new Vector3(0f, 0f, 0.5f), new Vector3(2.2f, 1.6f, 8f));
            CreatePart(root.transform, PrimitiveType.Cube, "Bridge", hull, new Vector3(0f, 1.1f, 3.8f), new Vector3(1f, 0.6f, 1.2f));
            CreatePart(root.transform, PrimitiveType.Cube, "Processing Port", rust, new Vector3(-1.6f, 0.2f, 1.5f), new Vector3(1.4f, 1.2f, 1.4f));
            CreatePart(root.transform, PrimitiveType.Cube, "Processing Starboard", rust, new Vector3(1.6f, 0.2f, 1.5f), new Vector3(1.4f, 1.2f, 1.4f));
            CreatePart(root.transform, PrimitiveType.Cube, "Ore Silo", rust, new Vector3(0f, 1.3f, -0.5f), new Vector3(1.4f, 1.2f, 1.8f));
            CreatePart(root.transform, PrimitiveType.Cube, "Gantry Port", warning, new Vector3(-1.2f, 0.9f, 0.5f), new Vector3(0.2f, 0.2f, 7f));
            CreatePart(root.transform, PrimitiveType.Cube, "Gantry Starboard", warning, new Vector3(1.2f, 0.9f, 0.5f), new Vector3(0.2f, 0.2f, 7f));
            CreatePart(root.transform, PrimitiveType.Cube, "Hangar Bay Door", bay, new Vector3(1.12f, -0.2f, -1.5f), new Vector3(0.1f, 0.8f, 1.6f));

            // Landing bay on the starboard side; its forward axis points out of the bay.
            Transform bayTransform = new GameObject("Landing Bay").transform;
            bayTransform.SetParent(root.transform, false);
            bayTransform.localPosition = new Vector3(1.4f, 0f, -1.5f);
            bayTransform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            Transform engineGlow = new GameObject("Engine Glow").transform;
            engineGlow.SetParent(root.transform, false);
            engineGlow.localPosition = new Vector3(0f, 0f, -3.7f);
            CreatePart(engineGlow, PrimitiveType.Sphere, "Engine Port", Engine(), new Vector3(-0.6f, 0f, 0f), Vector3.one * 0.8f);
            CreatePart(engineGlow, PrimitiveType.Sphere, "Engine Starboard", Engine(), new Vector3(0.6f, 0f, 0f), Vector3.one * 0.8f);
            TrailRenderer trail = CreateTrail(root.transform, new Vector3(0f, 0f, -4f), 1.2f, 8f);
            LineRenderer bracket = CreateBracket(root.transform);

            ShipAgent agent = root.AddComponent<ShipAgent>();
            ShipMovementProfile profile = LoadOrCreateProfile("RefineryMovement", ConfigureRefineryMovement);
            SerializedObject agentData = new SerializedObject(agent);
            EditorAssetUtility.FindProperty(agentData, "_className").stringValue = "Refinery";
            EditorAssetUtility.FindProperty(agentData, "_size").enumValueIndex = (int)ShipSize.Capital;
            EditorAssetUtility.FindProperty(agentData, "_movementProfile").objectReferenceValue = profile;
            agentData.ApplyModifiedPropertiesWithoutUndo();

            Hangar hangar = root.AddComponent<Hangar>();
            EditorAssetUtility.SetReferences(hangar, ("_bay", bayTransform));
            root.AddComponent<RawMaterialStorage>();
            AddDurability(root, RefineryHull);
            AddPresentation(root, engineGlow, trail, bracket, 34f, 1f);
            return SavePrefab(root, RefineryPrefabPath);
        }

        /// <summary>Heavy and slow, like a flying building (GDD: capital ships).</summary>
        static void ConfigureRefineryMovement(SerializedObject profile)
        {
            EditorAssetUtility.FindProperty(profile, "_motion._maximumSpeed").floatValue = 10f;
            EditorAssetUtility.FindProperty(profile, "_motion._mainDriveAcceleration").floatValue = 0.8f;
            EditorAssetUtility.FindProperty(profile, "_motion._turnRateDegreesPerSecond").floatValue = 10f;
            EditorAssetUtility.FindProperty(profile, "_motion._thrusterAcceleration").floatValue = 0.6f;
            EditorAssetUtility.FindProperty(profile, "_motion._thrusterMaximumSpeed").floatValue = 2f;
            EditorAssetUtility.FindProperty(profile, "_motion._maneuverRangeUnits").floatValue = 40f;
            EditorAssetUtility.FindProperty(profile, "_motion._arrivalToleranceUnits").floatValue = 1.5f;
            EditorAssetUtility.FindProperty(profile, "_motion._alignToleranceDegrees").floatValue = 4f;
            EditorAssetUtility.FindProperty(profile, "_jumpChargeSeconds").floatValue = 15f;
        }

        static ShipMovementProfile LoadOrCreateProfile(string name, System.Action<SerializedObject> configure)
        {
            string path = $"{DataFolder}/{name}.asset";
            ShipMovementProfile profile = AssetDatabase.LoadAssetAtPath<ShipMovementProfile>(path);
            if (profile != null)
            {
                return profile;
            }

            EditorAssetUtility.EnsureFolder(DataFolder);
            profile = ScriptableObject.CreateInstance<ShipMovementProfile>();
            AssetDatabase.CreateAsset(profile, path);
            if (configure != null)
            {
                SerializedObject serialized = new SerializedObject(profile);
                configure(serialized);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
            return profile;
        }

        static void AddPresentation(GameObject root, Transform engineGlow, TrailRenderer trail, LineRenderer bracket,
            float bracketSizePixels, float engineGlowScale)
        {
            ShipPresentation presentation = root.AddComponent<ShipPresentation>();
            SerializedObject serialized = new SerializedObject(presentation);
            EditorAssetUtility.FindProperty(serialized, "_engineGlow").objectReferenceValue = engineGlow;
            EditorAssetUtility.FindProperty(serialized, "_trail").objectReferenceValue = trail;
            EditorAssetUtility.FindProperty(serialized, "_bracket").objectReferenceValue = bracket;
            EditorAssetUtility.FindProperty(serialized, "_bracketSizePixels").floatValue = bracketSizePixels;
            EditorAssetUtility.FindProperty(serialized, "_engineGlowMaximumScale").floatValue = engineGlowScale;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddDurability(GameObject root, float maximumHull)
        {
            ShipDurability durability = root.AddComponent<ShipDurability>();
            SerializedObject serialized = new SerializedObject(durability);
            EditorAssetUtility.FindProperty(serialized, "_maximumHull").floatValue = maximumHull;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static ShipAgent SavePrefab(GameObject root, string path)
        {
            EditorAssetUtility.EnsureFolder(PrefabFolder);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<ShipAgent>();
        }

        static Material Engine()
        {
            return LoadOrCreateMaterial("ShipEngine", UnlitShaderName, EngineColor);
        }

        /// <summary>
        /// One material per blockout color: property blocks are not saved in prefabs, so colors that
        /// must survive in the asset need their own material.
        /// </summary>
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

        static Renderer CreatePart(Transform parent, PrimitiveType type, string name, Material material,
            Vector3 localPosition, Vector3 localScale)
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

        static TrailRenderer CreateTrail(Transform parent, Vector3 localPosition, float width, float seconds)
        {
            GameObject trailObject = new GameObject("Engine Trail");
            trailObject.transform.SetParent(parent, false);
            trailObject.transform.localPosition = localPosition;

            // The trail is longer than the ship so direction and speed read from afar (GDD: Light LOD).
            TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = LoadOrCreateMaterial("ShipTrail", UnlitShaderName, TrailColor);
            trail.time = seconds;
            trail.minVertexDistance = 0.5f;
            trail.widthMultiplier = width;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            return trail;
        }

        /// <summary>
        /// Blockout debris spray at the clamps: small chunks thrown away from the rock in world space,
        /// so they drift off instead of following the ship. The color is set per resource at runtime.
        /// </summary>
        static ParticleSystem CreateDebrisParticles(Transform parent, Vector3 localPosition)
        {
            GameObject debrisObject = new GameObject("Mining Debris");
            debrisObject.transform.SetParent(parent, false);
            debrisObject.transform.localPosition = localPosition;
            // Cone shapes emit along local +z; turn it to spray along the ship's up axis, away from the rock.
            debrisObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem debris = debrisObject.AddComponent<ParticleSystem>();
            debris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = debris.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 150;

            ParticleSystem.EmissionModule emission = debris.emission;
            emission.rateOverTime = 35f;

            ParticleSystem.ShapeModule shape = debris.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 55f;
            shape.radius = 0.25f;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = debris.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            ParticleSystemRenderer renderer = debrisObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = LoadOrCreateMaterial("MiningDebris", ParticleShaderName, Color.white);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return debris;
        }

        static LineRenderer CreateBracket(Transform parent)
        {
            GameObject bracketObject = new GameObject("Bracket");
            bracketObject.transform.SetParent(parent, false);

            LineRenderer bracket = bracketObject.AddComponent<LineRenderer>();
            bracket.sharedMaterial = LoadOrCreateMaterial("ShipBracket", UnlitShaderName, Color.white);
            bracket.useWorldSpace = false;
            bracket.loop = true;
            bracket.positionCount = 4;
            bracket.SetPositions(new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f)
            });
            bracket.shadowCastingMode = ShadowCastingMode.Off;
            bracket.receiveShadows = false;
            return bracket;
        }
    }
}
