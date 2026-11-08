using LastFleet.CameraControl;
using LastFleet.CameraControl.Editor;
using LastFleet.Dialogue;
using LastFleet.Combat;
using LastFleet.Exploration;
using LastFleet.FleetJump;
using LastFleet.Galaxy;
using LastFleet.Hud;
using LastFleet.Inspection;
using LastFleet.Mining;
using LastFleet.People;
using LastFleet.Provisions;
using LastFleet.Pursuit;
using LastFleet.Quests;
using LastFleet.Rewards;
using LastFleet.RunEnd;
using LastFleet.Stations;
using LastFleet.Stations.Editor;
using LastFleet.Shared.Editor;
using LastFleet.ShipControl;
using LastFleet.Ships;
using LastFleet.Ships.Editor;
using LastFleet.Space;
using LastFleet.Space.Editor;
using LastFleet.StarSystem;
using LastFleet.Zones;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastFleet.GenerationPreview.Editor
{
    /// <summary>
    /// Builds the generation preview scene: starfield backdrop, star map, star system view and an
    /// RTS camera. Menu: Last Fleet / Create Generation Preview Scene.
    /// </summary>
    public static class GenerationPreviewSceneBuilder
    {
        const string ModuleRoot = "Assets/Modules/GenerationPreview";
        const string SceneFolder = ModuleRoot + "/Scenes";
        const string ScenePath = SceneFolder + "/GenerationPreview.unity";
        const string LitMaterialPath = ModuleRoot + "/Graphics/Blockout/BlockoutLit.mat";
        const string UnlitMaterialPath = ModuleRoot + "/Graphics/Blockout/BlockoutUnlit.mat";
        const string LitShaderName = "Universal Render Pipeline/Lit";
        const string UnlitShaderName = "Universal Render Pipeline/Unlit";
        const float InitialBoundsRadius = 500f;
        const float InitialCameraDistance = 600f;
        static readonly Vector3 ZoneWorldOrigin = new Vector3(6000f, 0f, 0f);

        [MenuItem("Last Fleet/Create Generation Preview Scene")]
        static void CreateFromMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Generation preview exists", $"Overwrite {ScenePath}?", "Overwrite", "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Build();
        }

        public static void Build()
        {
            Material litMaterial = EditorAssetUtility.LoadOrCreateMaterial(LitMaterialPath, LitShaderName);
            Material unlitMaterial = EditorAssetUtility.LoadOrCreateMaterial(UnlitMaterialPath, UnlitShaderName);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // No sun on the maps: in the system map the star lights the planets; zones use the zone sun.
            SpaceEnvironment environment = SpaceSceneFactory.CreateEnvironment(null);
            SpaceSceneFactory.CreateGlobalVolume();
            RtsCameraRig cameraRig = CameraRigFactory.Create(Vector3.zero, InitialBoundsRadius, InitialCameraDistance);
            ZoneWorld zoneWorld = CreateZoneWorld(litMaterial, unlitMaterial);

            GameObject mapObject = new GameObject("Galaxy Map View");
            GalaxyMapView mapView = mapObject.AddComponent<GalaxyMapView>();
            EditorAssetUtility.SetReferences(mapView, ("_markerMaterial", unlitMaterial), ("_lineMaterial", unlitMaterial));

            GameObject systemObject = new GameObject("Star System View");
            StarSystemView systemView = systemObject.AddComponent<StarSystemView>();
            EditorAssetUtility.SetReferences(systemView, ("_bodyMaterial", litMaterial), ("_glowMaterial", unlitMaterial));

            GameObject shipsObject = new GameObject("Ships");
            ShipRegistry shipRegistry = shipsObject.AddComponent<ShipRegistry>();
            EditorAssetUtility.SetReferences(shipRegistry, ("_zoneWorld", zoneWorld), ("_camera", cameraRig.Camera));
            // The enemy keeps its own registry, so fleet systems such as mining or recall never see its ships.
            GameObject proninObject = new GameObject("Pronin Ships");
            ShipRegistry proninRegistry = proninObject.AddComponent<ShipRegistry>();
            SerializedObject proninData = new SerializedObject(proninRegistry);
            EditorAssetUtility.FindProperty(proninData, "_allegiance").enumValueIndex = (int)Allegiance.Pronin;
            proninData.ApplyModifiedPropertiesWithoutUndo();
            EditorAssetUtility.SetReferences(proninRegistry, ("_zoneWorld", zoneWorld), ("_camera", cameraRig.Camera));
            // Rocks, stations and capital ships of both sides are flown around and give cover.
            ObstacleField obstacles = shipsObject.AddComponent<ObstacleField>();
            SerializedObject obstacleData = new SerializedObject(obstacles);
            SerializedProperty registries = EditorAssetUtility.FindProperty(obstacleData, "_registries");
            registries.arraySize = 2;
            registries.GetArrayElementAtIndex(0).objectReferenceValue = shipRegistry;
            registries.GetArrayElementAtIndex(1).objectReferenceValue = proninRegistry;
            obstacleData.ApplyModifiedPropertiesWithoutUndo();
            EditorAssetUtility.SetReferences(shipRegistry, ("_obstacles", obstacles));
            EditorAssetUtility.SetReferences(proninRegistry, ("_obstacles", obstacles));
            CombatService combat = shipsObject.AddComponent<CombatService>();
            EditorAssetUtility.SetReferences(combat, ("_fleet", shipRegistry), ("_pronin", proninRegistry), ("_obstacles", obstacles));

            ShipCommandInput shipInput = shipsObject.AddComponent<ShipCommandInput>();
            EditorAssetUtility.SetReferences(shipInput, ("_camera", cameraRig.Camera), ("_registry", shipRegistry), ("_combat", combat));
            ControlGroupInput groupInput = shipsObject.AddComponent<ControlGroupInput>();
            EditorAssetUtility.SetReferences(groupInput, ("_commandInput", shipInput), ("_registry", shipRegistry));
            HangarCommandInput hangarInput = shipsObject.AddComponent<HangarCommandInput>();
            EditorAssetUtility.SetReferences(hangarInput, ("_commandInput", shipInput), ("_registry", shipRegistry));
            MiningCoordinator mining = shipsObject.AddComponent<MiningCoordinator>();
            EditorAssetUtility.SetReferences(mining, ("_registry", shipRegistry));
            ScoutingService scouting = shipsObject.AddComponent<ScoutingService>();
            EditorAssetUtility.SetReferences(scouting, ("_registry", shipRegistry));
            DockingService docking = shipsObject.AddComponent<DockingService>();
            EditorAssetUtility.SetReferences(docking, ("_registry", shipRegistry));
            EditorAssetUtility.SetReferences(shipInput, ("_scouting", scouting), ("_docking", docking));
            SearchService search = shipsObject.AddComponent<SearchService>();
            EditorAssetUtility.SetReferences(search, ("_docking", docking));
            RewardDelivery rewards = shipsObject.AddComponent<RewardDelivery>();
            EditorAssetUtility.SetReferences(rewards, ("_registry", shipRegistry));
            FleetPopulation population = shipsObject.AddComponent<FleetPopulation>();
            EditorAssetUtility.SetReferences(population, ("_registry", shipRegistry));
            FleetWaterSupply water = shipsObject.AddComponent<FleetWaterSupply>();
            EditorAssetUtility.SetReferences(water, ("_registry", shipRegistry), ("_population", population));
            CrewRecruitment recruitment = shipsObject.AddComponent<CrewRecruitment>();
            EditorAssetUtility.SetReferences(recruitment, ("_registry", shipRegistry));

            GameObject inspectionObject = new GameObject("Inspection Panel");
            InspectionPanel inspection = inspectionObject.AddComponent<InspectionPanel>();
            EditorAssetUtility.SetReferences(inspection, ("_shipInput", shipInput));

            GameObject shipPanelObject = new GameObject("Ship Panel");
            InspectionPanel shipPanel = shipPanelObject.AddComponent<InspectionPanel>();
            SerializedObject shipPanelData = new SerializedObject(shipPanel);
            EditorAssetUtility.FindProperty(shipPanelData, "_shipInput").objectReferenceValue = shipInput;
            EditorAssetUtility.FindProperty(shipPanelData, "_anchorBottom").boolValue = true;
            shipPanelData.ApplyModifiedPropertiesWithoutUndo();

            GameObject supplyBarObject = new GameObject("Supply Bar");
            SupplyBar supplyBar = supplyBarObject.AddComponent<SupplyBar>();
            EditorAssetUtility.SetReferences(supplyBar, ("_mining", mining), ("_shipInput", shipInput), ("_water", water));

            GameObject pursuitObject = new GameObject("Pursuit Clock");
            PursuitClock pursuit = pursuitObject.AddComponent<PursuitClock>();
            ProninAttack proninAttack = pursuitObject.AddComponent<ProninAttack>();
            EditorAssetUtility.SetReferences(proninAttack, ("_pursuit", pursuit), ("_zoneWorld", zoneWorld),
                ("_fleet", shipRegistry), ("_pronin", proninRegistry),
                ("_carrierPrefab", CombatShipPrefabBuilder.LoadOrCreateProninCarrier()),
                ("_fighterPrefab", CombatShipPrefabBuilder.LoadOrCreateProninFighter()));
            ThreatIndicator threatIndicator = supplyBarObject.AddComponent<ThreatIndicator>();
            EditorAssetUtility.SetReferences(threatIndicator, ("_pursuit", pursuit), ("_shipInput", shipInput));

            FleetJumpService fleetJump = pursuitObject.AddComponent<FleetJumpService>();
            EditorAssetUtility.SetReferences(fleetJump, ("_fleet", shipRegistry));
            JumpStatusIndicator jumpStatus = supplyBarObject.AddComponent<JumpStatusIndicator>();
            EditorAssetUtility.SetReferences(jumpStatus, ("_fleetJump", fleetJump), ("_shipInput", shipInput));
            FleetJumpDialog jumpDialog = supplyBarObject.AddComponent<FleetJumpDialog>();
            EditorAssetUtility.SetReferences(jumpDialog, ("_fleetJump", fleetJump), ("_shipInput", shipInput));

            GameObject questObject = new GameObject("Quests");
            DialogueService dialogue = questObject.AddComponent<DialogueService>();
            DialogueWindow dialogueWindow = questObject.AddComponent<DialogueWindow>();
            EditorAssetUtility.SetReferences(dialogueWindow, ("_dialogue", dialogue), ("_shipInput", shipInput));
            QuestDirector quests = questObject.AddComponent<QuestDirector>();
            EditorAssetUtility.SetReferences(quests, ("_fleet", shipRegistry), ("_pronin", proninRegistry), ("_zoneWorld", zoneWorld),
                ("_search", search), ("_dialogue", dialogue),
                ("_shuttlePrefab", ShipPrefabBuilder.LoadOrCreateShuttle()),
                ("_proninFighterPrefab", CombatShipPrefabBuilder.LoadOrCreateProninFighter()),
                ("_guardFighterPrefab", CombatShipPrefabBuilder.LoadOrCreateStationGuard()),
                ("_explosionPrefab",CombatEffectsBuilder.LoadOrCreateExplosion()));
            QuestLogPanel questLog = questObject.AddComponent<QuestLogPanel>();
            EditorAssetUtility.SetReferences(questLog, ("_quests", quests), ("_shipInput", shipInput));
            EditorAssetUtility.SetReferences(jumpDialog, ("_quests", quests));

            RunEndWindow runEndWindow = questObject.AddComponent<RunEndWindow>();
            EditorAssetUtility.SetReferences(runEndWindow, ("_shipInput", shipInput));

            GameObject previewObject = new GameObject("Generation Preview");
            GenerationPreviewController preview = previewObject.AddComponent<GenerationPreviewController>();
            EditorAssetUtility.SetReferences(preview,
                ("_galaxyMapView", mapView), ("_starSystemView", systemView), ("_zoneWorld", zoneWorld),
                ("_environment", environment), ("_cameraRig", cameraRig), ("_shipRegistry", shipRegistry),
                ("_shipInput", shipInput), ("_groupInput", groupInput), ("_hangarInput", hangarInput), ("_mining", mining),
                ("_scouting", scouting), ("_population", population), ("_docking", docking), ("_search", search), ("_rewards", rewards), ("_combat", combat), ("_pursuit", pursuit), ("_proninAttack", proninAttack), ("_fleetJump", fleetJump), ("_jumpDialog", jumpDialog), ("_jumpStatus", jumpStatus), ("_inspection", inspection), ("_shipPanel", shipPanel), ("_quests", quests),
                ("_runEndWindow", runEndWindow), ("_water", water),
                ("_cruiserPrefab", CombatShipPrefabBuilder.LoadOrCreateCruiser()),
                ("_fighterPrefab", CombatShipPrefabBuilder.LoadOrCreateFighter()),
                ("_refineryPrefab", ShipPrefabBuilder.LoadOrCreateRefinery()),
                ("_tankerPrefab", ShipPrefabBuilder.LoadOrCreateTanker()),
                ("_linerPrefab", ShipPrefabBuilder.LoadOrCreateLiner()),
                ("_shuttlePrefab", ShipPrefabBuilder.LoadOrCreateShuttle()),
                ("_harvesterPrefab", ShipPrefabBuilder.LoadOrCreateHarvester()));
            RunEndFlow runEnd = previewObject.AddComponent<RunEndFlow>();
            EditorAssetUtility.SetReferences(runEnd, ("_controller", preview), ("_quests", quests), ("_dialogue", dialogue),
                ("_fleet", shipRegistry), ("_population", population), ("_window", runEndWindow));
            GenerationPreviewOverlay overlay = previewObject.AddComponent<GenerationPreviewOverlay>();
            EditorAssetUtility.SetReferences(overlay, ("_controller", preview));
            DeveloperRaidInput raid = previewObject.AddComponent<DeveloperRaidInput>();
            EditorAssetUtility.SetReferences(raid, ("_controller", preview), ("_pronin", proninRegistry),
                ("_proninFighterPrefab", CombatShipPrefabBuilder.LoadOrCreateProninFighter()));

            EditorAssetUtility.EnsureFolder(SceneFolder);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorAssetUtility.AddSceneToBuildSettings(ScenePath, true);
            Debug.Log($"Generation preview scene created at {ScenePath}.");
        }

        static ZoneWorld CreateZoneWorld(Material litMaterial, Material unlitMaterial)
        {
            GameObject sunObject = new GameObject("Zone Sun");
            Light zoneSun = sunObject.AddComponent<Light>();
            zoneSun.type = LightType.Directional;
            zoneSun.color = new Color(1f, 0.95f, 0.88f);
            zoneSun.intensity = 1.6f;
            zoneSun.shadows = LightShadows.None;
            zoneSun.enabled = false;

            // Far from the maps at the origin, so zones never appear in the map views.
            GameObject worldObject = new GameObject("Zone World");
            worldObject.transform.position = ZoneWorldOrigin;
            worldObject.SetActive(false);
            ZoneWorld zoneWorld = worldObject.AddComponent<ZoneWorld>();
            EditorAssetUtility.SetReferences(zoneWorld,
                ("_gridMaterial", SpaceSceneFactory.LoadOrCreateGridMaterial()), ("_bodyMaterial", litMaterial),
                ("_glowMaterial", unlitMaterial), ("_zoneSun", zoneSun), ("_stationViews", StationPrefabBuilder.LoadOrCreateCatalog()));
            worldObject.SetActive(true);
            return zoneWorld;
        }
    }
}
