using LastFleet.Exploration;
using LastFleet.Mining;
using LastFleet.People;
using LastFleet.Shared.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastFleet.Ships.Editor
{
    /// <summary>
    /// Creates the blockout ship prefabs with their movement profiles and materials.
    /// Game scale (GDD): a harvester looks like about 200 m (2 units), the refinery about 1 km (10 units),
    /// the liner about 1.6 km (16 units).
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
        const string LinerPrefabPath = PrefabFolder + "/Liner.prefab";
        const string ShuttlePrefabPath = PrefabFolder + "/Shuttle.prefab";
        const string LitShaderName = "Universal Render Pipeline/Lit";
        const string UnlitShaderName = "Universal Render Pipeline/Unlit";
        const string ParticleShaderName = "Universal Render Pipeline/Particles/Unlit";
        const float HarvesterHull = 100f;
        const float RefineryHull = 2000f;
        const float LinerHull = 2500f;
        const int LinerHangarCapacity = 6;
        const float ShuttleHull = 80f;
        const float ShuttleScanSeconds = 2f;

        // Blockout balance: a harvester tank lasts about 1000 km or 50 AU; the refinery carries enough
        // to refuel its harvesters several times. Zones are 50 km in radius, zone jumps a few AU.
        const float HarvesterTank = 100f;
        const float HarvesterFuelPerKlick = 0.1f;
        const float HarvesterFuelPerAU = 2f;
        const float RefineryTank = 5000f;
        const float RefineryFuelPerKlick = 1f;
        const float RefineryFuelPerAU = 50f;
        const float LinerTank = 4000f;
        const float LinerFuelPerKlick = 1f;
        const float LinerFuelPerAU = 40f;
        const float ShuttleTank = 120f;
        const float ShuttleFuelPerKlick = 0.08f;
        const float ShuttleFuelPerAU = 1.5f;

        // GDD: capital ships start below capacity, so rescued people have room.
        const int RefineryPassengerCapacity = 1200;
        const int RefineryStartingPassengers = 800;
        const int LinerPassengerCapacity = 8000;
        const int LinerStartingPassengers = 6000;
        // A shuttle carries more people than any other small ship (GDD: Small ships), and starts empty.
        const int ShuttleSeats = 40;

        // Crew of small ships: a harvester needs a pilot and two for mining, a shuttle two pilots.
        const int HarvesterCrew = 3;
        const int ShuttleCrew = 2;
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

        // Civilian design language (GDD): bright, rounded, many windows, faded advertising.
        static readonly Color LinerHullColor = new Color(0.86f, 0.87f, 0.83f);
        static readonly Color LinerAccentColor = new Color(0.35f, 0.5f, 0.65f);
        static readonly Color WindowColor = new Color(2f, 1.8f, 1.3f);
        static readonly Color AdvertisingColor = new Color(0.62f, 0.32f, 0.3f);

        [MenuItem("Last Fleet/Rebuild Ship Prefabs")]
        public static void RebuildAll()
        {
            AssetDatabase.DeleteAsset(HarvesterPrefabPath);
            AssetDatabase.DeleteAsset(RefineryPrefabPath);
            AssetDatabase.DeleteAsset(LinerPrefabPath);
            AssetDatabase.DeleteAsset(ShuttlePrefabPath);
            LoadOrCreateHarvester();
            LoadOrCreateRefinery();
            LoadOrCreateLiner();
            LoadOrCreateShuttle();
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
            AddFuel(root, HarvesterTank, HarvesterFuelPerKlick, HarvesterFuelPerAU);
            AddCrew(root, HarvesterCrew);
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
            AddFuel(root, RefineryTank, RefineryFuelPerKlick, RefineryFuelPerAU);
            AddPassengers(root, RefineryPassengerCapacity, RefineryStartingPassengers);
            AddPresentation(root, engineGlow, trail, bracket, 34f, 1f);
            return SavePrefab(root, RefineryPrefabPath);
        }

        /// <summary>Civilian passenger liner with a shuttle hangar (GDD: Interplanetary Liner 1).</summary>
        public static ShipAgent LoadOrCreateLiner()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(LinerPrefabPath);
            if (existing != null)
            {
                return existing.GetComponent<ShipAgent>();
            }

            GameObject root = new GameObject("Liner");
            Material hull = LoadOrCreateMaterial("LinerHull", LitShaderName, LinerHullColor);
            Material accent = LoadOrCreateMaterial("LinerAccent", LitShaderName, LinerAccentColor);
            Material advertising = LoadOrCreateMaterial("LinerAdvertising", LitShaderName, AdvertisingColor);
            Material windows = LoadOrCreateMaterial("LinerWindows", UnlitShaderName, WindowColor);
            Material bay = LoadOrCreateMaterial("HangarBay", UnlitShaderName, BayColor);

            // Rounded hull along +z: a long cylinder with spherical bow and stern.
            Renderer body = CreatePart(root.transform, PrimitiveType.Cylinder, "Hull", hull, new Vector3(0f, 0f, 0f), new Vector3(2.4f, 6.5f, 2.4f));
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePart(root.transform, PrimitiveType.Sphere, "Bow", hull, new Vector3(0f, 0f, 6.5f), new Vector3(2.4f, 2.4f, 3f));
            CreatePart(root.transform, PrimitiveType.Sphere, "Stern", hull, new Vector3(0f, 0f, -6.5f), new Vector3(2.4f, 2.4f, 2f));
            CreatePart(root.transform, PrimitiveType.Capsule, "Bridge", accent, new Vector3(0f, 1.3f, 4.5f), new Vector3(0.9f, 0.45f, 1.6f))
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePart(root.transform, PrimitiveType.Cube, "Accent Stripe", accent, new Vector3(0f, -0.9f, 0f), new Vector3(2.2f, 0.25f, 12f));
            CreatePart(root.transform, PrimitiveType.Cube, "Advertising", advertising, new Vector3(0f, 1.15f, -1.5f), new Vector3(1.4f, 0.1f, 3.5f));

            // Rows of lit windows along both flanks.
            for (int row = 0; row < 2; row++)
            {
                float height = 0.45f - row * 0.5f;
                CreatePart(root.transform, PrimitiveType.Cube, $"Windows Port {row + 1}", windows, new Vector3(-1.19f, height, 0.5f), new Vector3(0.05f, 0.12f, 10f));
                CreatePart(root.transform, PrimitiveType.Cube, $"Windows Starboard {row + 1}", windows, new Vector3(1.19f, height, 0.5f), new Vector3(0.05f, 0.12f, 10f));
            }
            CreatePart(root.transform, PrimitiveType.Cube, "Hangar Bay Door", bay, new Vector3(-1.2f, -0.3f, -3.5f), new Vector3(0.1f, 0.7f, 1.6f));

            // Landing bay on the port side; its forward axis points out of the bay.
            Transform bayTransform = new GameObject("Landing Bay").transform;
            bayTransform.SetParent(root.transform, false);
            bayTransform.localPosition = new Vector3(-1.5f, -0.3f, -3.5f);
            bayTransform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            Transform engineGlow = new GameObject("Engine Glow").transform;
            engineGlow.SetParent(root.transform, false);
            engineGlow.localPosition = new Vector3(0f, 0f, -7.4f);
            CreatePart(engineGlow, PrimitiveType.Sphere, "Engine Port", Engine(), new Vector3(-0.7f, 0f, 0f), Vector3.one * 0.7f);
            CreatePart(engineGlow, PrimitiveType.Sphere, "Engine Center", Engine(), new Vector3(0f, 0f, -0.1f), Vector3.one * 0.8f);
            CreatePart(engineGlow, PrimitiveType.Sphere, "Engine Starboard", Engine(), new Vector3(0.7f, 0f, 0f), Vector3.one * 0.7f);
            TrailRenderer trail = CreateTrail(root.transform, new Vector3(0f, 0f, -7.8f), 1.4f, 9f);
            LineRenderer bracket = CreateBracket(root.transform);

            ShipAgent agent = root.AddComponent<ShipAgent>();
            ShipMovementProfile profile = LoadOrCreateProfile("LinerMovement", ConfigureLinerMovement);
            SerializedObject agentData = new SerializedObject(agent);
            EditorAssetUtility.FindProperty(agentData, "_className").stringValue = "Liner";
            EditorAssetUtility.FindProperty(agentData, "_size").enumValueIndex = (int)ShipSize.Capital;
            EditorAssetUtility.FindProperty(agentData, "_movementProfile").objectReferenceValue = profile;
            agentData.ApplyModifiedPropertiesWithoutUndo();

            Hangar hangar = root.AddComponent<Hangar>();
            SerializedObject hangarData = new SerializedObject(hangar);
            EditorAssetUtility.FindProperty(hangarData, "_bay").objectReferenceValue = bayTransform;
            EditorAssetUtility.FindProperty(hangarData, "_capacity").intValue = LinerHangarCapacity;
            hangarData.ApplyModifiedPropertiesWithoutUndo();
            AddDurability(root, LinerHull);
            AddFuel(root, LinerTank, LinerFuelPerKlick, LinerFuelPerAU);
            AddPassengers(root, LinerPassengerCapacity, LinerStartingPassengers);
            AddPresentation(root, engineGlow, trail, bracket, 38f, 1f);
            return SavePrefab(root, LinerPrefabPath);
        }

        /// <summary>
        /// Civilian shuttle (GDD: Small ships): carries people, scans and later docks at stations. A
        /// rounded white body with a window band, about the size of a harvester.
        /// </summary>
        public static ShipAgent LoadOrCreateShuttle()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ShuttlePrefabPath);
            if (existing != null)
            {
                return existing.GetComponent<ShipAgent>();
            }

            GameObject root = new GameObject("Shuttle");
            Material hull = LoadOrCreateMaterial("LinerHull", LitShaderName, LinerHullColor);
            Material accent = LoadOrCreateMaterial("LinerAccent", LitShaderName, LinerAccentColor);
            Material windows = LoadOrCreateMaterial("LinerWindows", UnlitShaderName, WindowColor);

            CreatePart(root.transform, PrimitiveType.Capsule, "Hull", hull, new Vector3(0f, 0f, 0.1f), new Vector3(0.6f, 0.75f, 0.5f))
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePart(root.transform, PrimitiveType.Sphere, "Cockpit", accent, new Vector3(0f, 0.12f, 0.6f), new Vector3(0.38f, 0.26f, 0.36f));
            CreatePart(root.transform, PrimitiveType.Cube, "Windows Port", windows, new Vector3(-0.29f, 0.05f, 0f), new Vector3(0.03f, 0.08f, 0.8f));
            CreatePart(root.transform, PrimitiveType.Cube, "Windows Starboard", windows, new Vector3(0.29f, 0.05f, 0f), new Vector3(0.03f, 0.08f, 0.8f));
            CreatePart(root.transform, PrimitiveType.Cube, "Wing", accent, new Vector3(0f, -0.12f, -0.35f), new Vector3(1.2f, 0.05f, 0.35f));

            Transform engineGlow = CreatePart(root.transform, PrimitiveType.Sphere, "Engine Glow", Engine(),
                new Vector3(0f, 0f, -0.68f), Vector3.one * 0.28f).transform;
            TrailRenderer trail = CreateTrail(root.transform, new Vector3(0f, 0f, -0.72f), 0.25f, 3.5f);
            LineRenderer bracket = CreateBracket(root.transform);

            ShipAgent agent = root.AddComponent<ShipAgent>();
            ShipMovementProfile profile = LoadOrCreateProfile("ShuttleMovement", ConfigureShuttleMovement);
            SerializedObject agentData = new SerializedObject(agent);
            EditorAssetUtility.FindProperty(agentData, "_className").stringValue = "Shuttle";
            EditorAssetUtility.FindProperty(agentData, "_size").enumValueIndex = (int)ShipSize.Small;
            EditorAssetUtility.FindProperty(agentData, "_movementProfile").objectReferenceValue = profile;
            agentData.ApplyModifiedPropertiesWithoutUndo();

            AddPresentation(root, engineGlow, trail, bracket, 20f, 1f);
            AddDurability(root, ShuttleHull);
            AddFuel(root, ShuttleTank, ShuttleFuelPerKlick, ShuttleFuelPerAU);
            AddCrew(root, ShuttleCrew);
            ShipScanner scanner = root.AddComponent<ShipScanner>();
            SerializedObject scannerData = new SerializedObject(scanner);
            EditorAssetUtility.FindProperty(scannerData, "_scanSeconds").floatValue = ShuttleScanSeconds;
            EditorAssetUtility.FindProperty(scannerData, "_isScout").boolValue = true;
            scannerData.ApplyModifiedPropertiesWithoutUndo();
            AddPassengers(root, ShuttleSeats, 0);
            SerializedObject passengerData = new SerializedObject(root.GetComponent<ShipPassengers>());
            EditorAssetUtility.FindProperty(passengerData, "_disembarkWhenDocked").boolValue = true;
            passengerData.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(root, ShuttlePrefabPath);
        }

        /// <summary>Quick and nimble: shuttles are the fleet's eyes and ferries.</summary>
        static void ConfigureShuttleMovement(SerializedObject profile)
        {
            EditorAssetUtility.FindProperty(profile, "_motion._maximumSpeed").floatValue = 50f;
            EditorAssetUtility.FindProperty(profile, "_motion._mainDriveAcceleration").floatValue = 16f;
            EditorAssetUtility.FindProperty(profile, "_motion._turnRateDegreesPerSecond").floatValue = 100f;
            EditorAssetUtility.FindProperty(profile, "_motion._thrusterAcceleration").floatValue = 5f;
            EditorAssetUtility.FindProperty(profile, "_motion._thrusterMaximumSpeed").floatValue = 7f;
            EditorAssetUtility.FindProperty(profile, "_jumpChargeSeconds").floatValue = 4f;
        }

        /// <summary>A little livelier than the industrial ships, but still a capital ship.</summary>
        static void ConfigureLinerMovement(SerializedObject profile)
        {
            EditorAssetUtility.FindProperty(profile, "_motion._maximumSpeed").floatValue = 12f;
            EditorAssetUtility.FindProperty(profile, "_motion._mainDriveAcceleration").floatValue = 1f;
            EditorAssetUtility.FindProperty(profile, "_motion._turnRateDegreesPerSecond").floatValue = 12f;
            EditorAssetUtility.FindProperty(profile, "_motion._thrusterAcceleration").floatValue = 0.7f;
            EditorAssetUtility.FindProperty(profile, "_motion._thrusterMaximumSpeed").floatValue = 2.5f;
            EditorAssetUtility.FindProperty(profile, "_motion._maneuverRangeUnits").floatValue = 45f;
            EditorAssetUtility.FindProperty(profile, "_motion._arrivalToleranceUnits").floatValue = 1.5f;
            EditorAssetUtility.FindProperty(profile, "_motion._alignToleranceDegrees").floatValue = 4f;
            EditorAssetUtility.FindProperty(profile, "_jumpChargeSeconds").floatValue = 15f;
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

        static void AddFuel(GameObject root, float tankCapacity, float fuelPerKlick, float fuelPerAU)
        {
            ShipFuel fuel = root.AddComponent<ShipFuel>();
            SerializedObject serialized = new SerializedObject(fuel);
            EditorAssetUtility.FindProperty(serialized, "_tankCapacity").floatValue = tankCapacity;
            EditorAssetUtility.FindProperty(serialized, "_fuelPerKlick").floatValue = fuelPerKlick;
            EditorAssetUtility.FindProperty(serialized, "_fuelPerAU").floatValue = fuelPerAU;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddCrew(GameObject root, int requiredCrew)
        {
            ShipCrew crew = root.AddComponent<ShipCrew>();
            SerializedObject serialized = new SerializedObject(crew);
            EditorAssetUtility.FindProperty(serialized, "_requiredCrew").intValue = requiredCrew;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddPassengers(GameObject root, int capacity, int startingPassengers)
        {
            ShipPassengers passengers = root.AddComponent<ShipPassengers>();
            SerializedObject serialized = new SerializedObject(passengers);
            EditorAssetUtility.FindProperty(serialized, "_capacity").intValue = capacity;
            EditorAssetUtility.FindProperty(serialized, "_startingPassengers").intValue = startingPassengers;
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
