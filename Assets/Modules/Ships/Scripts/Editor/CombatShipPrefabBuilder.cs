using LastFleet.Combat;
using LastFleet.Shared.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastFleet.Ships.Editor
{
    /// <summary>
    /// Creates the blockout prefabs of armed ships: the human cruiser with its fighter hangar, the
    /// human fighter and the Pronin fighter. All of them use the same weapon, shield and combat
    /// components; only their numbers and looks differ. Game scale (GDD): a cruiser looks like about
    /// 1.4 km (14 units), a fighter about 100 m (1 unit).
    /// Built together with the other ships by Last Fleet / Rebuild Ship Prefabs.
    /// </summary>
    public static class CombatShipPrefabBuilder
    {
        const string CruiserPrefabPath = ShipPrefabBuilder.PrefabFolder + "/Cruiser.prefab";
        const string FighterPrefabPath = ShipPrefabBuilder.PrefabFolder + "/Fighter.prefab";
        const string ProninFighterPrefabPath = ShipPrefabBuilder.PrefabFolder + "/ProninFighter.prefab";
        const string LitShaderName = ShipPrefabBuilder.LitShaderName;
        const string UnlitShaderName = ShipPrefabBuilder.UnlitShaderName;

        // Weapons reach across half a zone (zones are 50 km in radius); the hit chance falls with distance.
        const float HalfZoneRangeUnits = 250f;

        const float CruiserHull = 3500f;
        const int CruiserHangarCapacity = 8;
        const float CruiserTank = 4500f;
        const float CruiserFuelPerKlick = 1f;
        const float CruiserFuelPerAU = 45f;

        // GDD: fighters are agile, with strong weapons and shields, room for up to 2 pilots and no cargo.
        const float FighterHull = 60f;
        const int FighterCrew = 2;
        const float FighterTank = 80f;
        const float FighterFuelPerKlick = 0.08f;
        const float FighterFuelPerAU = 1.5f;

        // The Pronin are machines: no crew to lose, and tanks for long hunts through the system.
        const float ProninFighterHull = 50f;
        const float ProninFighterTank = 200f;
        const float ProninFighterFuelPerKlick = 0.08f;
        const float ProninFighterFuelPerAU = 1.5f;

        // About a fifth of a harvester's hold: worth a short trip, not worth staying for long.
        const float ProninFighterWreckMetal = 25f;

        // Military design language (GDD): dark hulls with painted bows like old warplanes.
        static readonly Color MilitaryHullColor = new Color(0.16f, 0.17f, 0.19f);
        static readonly Color MilitaryPlatingColor = new Color(0.26f, 0.27f, 0.25f);
        static readonly Color NoseArtColor = new Color(0.7f, 0.12f, 0.1f);
        static readonly Color TeethColor = new Color(0.9f, 0.88f, 0.8f);
        static readonly Color HumanTracerColor = new Color(4f, 2.2f, 0.5f);
        static readonly Color MuzzleFlashColor = new Color(5f, 3.5f, 1.2f);

        // Pronin design language (GDD): bright, glossy and sleek; clean energy beams.
        static readonly Color ProninHullColor = new Color(0.92f, 0.94f, 0.97f);
        static readonly Color ProninGlowColor = new Color(0.6f, 2.2f, 3f);
        static readonly Color ProninBeamColor = new Color(0.8f, 2.5f, 4f);

        public static void RebuildAll()
        {
            AssetDatabase.DeleteAsset(CruiserPrefabPath);
            AssetDatabase.DeleteAsset(FighterPrefabPath);
            AssetDatabase.DeleteAsset(ProninFighterPrefabPath);
            LoadOrCreateCruiser();
            LoadOrCreateFighter();
            LoadOrCreateProninFighter();
        }

        /// <summary>Military capital ship with a fighter hangar and defensive guns (GDD: Cruiser 1).</summary>
        public static ShipAgent LoadOrCreateCruiser()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(CruiserPrefabPath);
            if (existing != null)
            {
                return existing.GetComponent<ShipAgent>();
            }

            GameObject root = new GameObject("Cruiser");
            Material hull = LitMaterial("MilitaryHull", MilitaryHullColor);
            Material plating = LitMaterial("MilitaryPlating", MilitaryPlatingColor);
            Material noseArt = LitMaterial("MilitaryNoseArt", NoseArtColor);
            Material teeth = LitMaterial("MilitaryTeeth", TeethColor);
            Material bay = ShipPrefabBuilder.LoadOrCreateMaterial("HangarBay", UnlitShaderName, ShipPrefabBuilder.BayColor);

            Transform body = root.transform;
            ShipPrefabBuilder.CreatePart(body, PrimitiveType.Cube, "Hull", hull, new Vector3(0f, 0f, 0f), new Vector3(2.4f, 1.4f, 11f));
            ShipPrefabBuilder.CreatePart(body, PrimitiveType.Cube, "Bow", hull, new Vector3(0f, -0.1f, 6.2f), new Vector3(1.8f, 1.1f, 1.6f));
            ShipPrefabBuilder.CreatePart(body, PrimitiveType.Cube, "Bridge", plating, new Vector3(0f, 1.0f, -2.5f), new Vector3(1f, 0.7f, 1.6f));
            ShipPrefabBuilder.CreatePart(body, PrimitiveType.Cube, "Flight Deck", plating, new Vector3(1.6f, -0.3f, 0.5f), new Vector3(0.9f, 0.5f, 8f));

            // Painted maw on the bow: a red mouth with a row of teeth on each flank.
            ShipPrefabBuilder.CreatePart(body, PrimitiveType.Cube, "Nose Art", noseArt, new Vector3(0f, -0.2f, 6.6f), new Vector3(1.85f, 0.5f, 1.0f));
            for (int i = 0; i < 4; i++)
            {
                float z = 6.2f + i * 0.25f;
                ShipPrefabBuilder.CreatePart(body, PrimitiveType.Cube, $"Teeth Port {i + 1}", teeth, new Vector3(-0.93f, -0.05f, z), new Vector3(0.03f, 0.16f, 0.1f));
                ShipPrefabBuilder.CreatePart(body, PrimitiveType.Cube, $"Teeth Starboard {i + 1}", teeth, new Vector3(0.93f, -0.05f, z), new Vector3(0.03f, 0.16f, 0.1f));
            }

            // Gun turrets along the spine; each barrel tip is a muzzle the guns fire from in turn.
            Transform[] turretMuzzles = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                Vector3 position = new Vector3(0f, 0.85f, 3.8f - i * 2.2f);
                ShipPrefabBuilder.CreatePart(body, PrimitiveType.Cylinder, $"Turret {i + 1}", plating, position, new Vector3(0.6f, 0.15f, 0.6f));
                ShipPrefabBuilder.CreatePart(body, PrimitiveType.Cube, $"Turret Barrel {i + 1}", hull, position + new Vector3(0f, 0.1f, 0.45f), new Vector3(0.12f, 0.12f, 0.7f));
                turretMuzzles[i] = CreateMuzzle(body, $"Turret Muzzle {i + 1}", position + new Vector3(0f, 0.1f, 0.85f));
            }
            ShipPrefabBuilder.CreatePart(body, PrimitiveType.Cube, "Hangar Bay Door", bay, new Vector3(2.06f, -0.3f, -2f), new Vector3(0.05f, 0.4f, 1.6f));

            // Landing bay on the starboard flight deck; its forward axis points out of the bay.
            Transform bayTransform = new GameObject("Landing Bay").transform;
            bayTransform.SetParent(body, false);
            bayTransform.localPosition = new Vector3(2.4f, -0.3f, -2f);
            bayTransform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            Transform engineGlow = new GameObject("Engine Glow").transform;
            engineGlow.SetParent(body, false);
            engineGlow.localPosition = new Vector3(0f, 0f, -5.7f);
            ShipPrefabBuilder.CreatePart(engineGlow, PrimitiveType.Sphere, "Engine Port", ShipPrefabBuilder.Engine(), new Vector3(-0.6f, 0f, 0f), Vector3.one * 0.8f);
            ShipPrefabBuilder.CreatePart(engineGlow, PrimitiveType.Sphere, "Engine Starboard", ShipPrefabBuilder.Engine(), new Vector3(0.6f, 0f, 0f), Vector3.one * 0.8f);
            TrailRenderer trail = ShipPrefabBuilder.CreateTrail(body, new Vector3(0f, 0f, -6f), 1.2f, 8f);
            LineRenderer bracket = ShipPrefabBuilder.CreateBracket(body);

            ShipAgent agent = root.AddComponent<ShipAgent>();
            ShipMovementProfile profile = ShipPrefabBuilder.LoadOrCreateProfile("CruiserMovement", ConfigureCruiserMovement);
            ConfigureAgent(agent, "Cruiser", ShipSize.Capital, profile);

            Hangar hangar = root.AddComponent<Hangar>();
            SerializedObject hangarData = new SerializedObject(hangar);
            EditorAssetUtility.FindProperty(hangarData, "_bay").objectReferenceValue = bayTransform;
            EditorAssetUtility.FindProperty(hangarData, "_capacity").intValue = CruiserHangarCapacity;
            // GDD: the start upgrade makes launches faster; military hangars start quicker than civilian ones.
            EditorAssetUtility.FindProperty(hangarData, "_launchIntervalSeconds").floatValue = 1f;
            hangarData.ApplyModifiedPropertiesWithoutUndo();
            ShipPrefabBuilder.AddDurability(root, CruiserHull);
            ShipPrefabBuilder.AddFuel(root, CruiserTank, CruiserFuelPerKlick, CruiserFuelPerAU);
            // Many light guns: they hold fighters off rather than chase them (GDD: capital ships fire automatically).
            AddWeapons(root, new WeaponSettings(HalfZoneRangeUnits, 3f, 0.15f, 0.6f, 0.05f,
                roundsPerBurst: 8, secondsBetweenBursts: 2.5f, firingArcDegrees: 360f, projectileSpeedUnitsPerSecond: 150f));
            AddProjectileView(root, turretMuzzles, 0.14f, 4f, 0.5f);
            ShipPrefabBuilder.AddPresentation(root, engineGlow, trail, bracket, 36f, 1f);
            CombatEffectsBuilder.AddHitFeedback(root, 0f, 0.6f);
            CombatEffectsBuilder.AddDestruction(root, 14f);
            return ShipPrefabBuilder.SavePrefab(root, CruiserPrefabPath);
        }

        /// <summary>Human fighter (GDD: Small ships): dark, with a painted red nose.</summary>
        public static ShipAgent LoadOrCreateFighter()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(FighterPrefabPath);
            if (existing != null)
            {
                return existing.GetComponent<ShipAgent>();
            }

            GameObject root = new GameObject("Fighter");
            Material hull = LitMaterial("MilitaryHull", MilitaryHullColor);
            Material plating = LitMaterial("MilitaryPlating", MilitaryPlatingColor);
            Material noseArt = LitMaterial("MilitaryNoseArt", NoseArtColor);

            ShipPrefabBuilder.CreatePart(root.transform, PrimitiveType.Cube, "Fuselage", hull, new Vector3(0f, 0f, 0f), new Vector3(0.25f, 0.18f, 1f));
            ShipPrefabBuilder.CreatePart(root.transform, PrimitiveType.Cube, "Nose", noseArt, new Vector3(0f, 0f, 0.6f), new Vector3(0.2f, 0.14f, 0.25f));
            ShipPrefabBuilder.CreatePart(root.transform, PrimitiveType.Cube, "Wings", plating, new Vector3(0f, 0f, -0.15f), new Vector3(1f, 0.04f, 0.35f));
            ShipPrefabBuilder.CreatePart(root.transform, PrimitiveType.Cube, "Tail", plating, new Vector3(0f, 0.12f, -0.4f), new Vector3(0.04f, 0.2f, 0.2f));

            Transform engineGlow = ShipPrefabBuilder.CreatePart(root.transform, PrimitiveType.Sphere, "Engine Glow", ShipPrefabBuilder.Engine(),
                new Vector3(0f, 0f, -0.55f), Vector3.one * 0.2f).transform;
            TrailRenderer trail = ShipPrefabBuilder.CreateTrail(root.transform, new Vector3(0f, 0f, -0.6f), 0.15f, 3f);
            LineRenderer bracket = ShipPrefabBuilder.CreateBracket(root.transform);
            // A gun in each wing root, firing in turn.
            Transform[] fighterMuzzles =
            {
                CreateMuzzle(root.transform, "Muzzle Port", new Vector3(-0.2f, 0f, 0.15f)),
                CreateMuzzle(root.transform, "Muzzle Starboard", new Vector3(0.2f, 0f, 0.15f))
            };

            ShipAgent agent = root.AddComponent<ShipAgent>();
            ShipMovementProfile profile = ShipPrefabBuilder.LoadOrCreateProfile("FighterMovement", ConfigureFighterMovement);
            ConfigureAgent(agent, "Fighter", ShipSize.Small, profile);

            ShipPrefabBuilder.AddPresentation(root, engineGlow, trail, bracket, 18f, 1f);
            ShipPrefabBuilder.AddDurability(root, FighterHull);
            ShipPrefabBuilder.AddFuel(root, FighterTank, FighterFuelPerKlick, FighterFuelPerAU);
            ShipPrefabBuilder.AddCrew(root, FighterCrew);
            AddShield(root, 60f, 6f, 4f);
            // Forward guns in short bursts: they only fire while the nose points at an enemy.
            AddWeapons(root, new WeaponSettings(HalfZoneRangeUnits, 4f, 0.1f, 0.85f, 0.05f,
                roundsPerBurst: 6, secondsBetweenBursts: 1.2f, firingArcDegrees: 30f, projectileSpeedUnitsPerSecond: 150f));
            AddProjectileView(root, fighterMuzzles, 0.08f, 2.5f, 0.25f);
            root.AddComponent<FighterCombat>();
            CombatEffectsBuilder.AddHitFeedback(root, 0.9f, 0.12f);
            CombatEffectsBuilder.AddDestruction(root, 1.2f);
            return ShipPrefabBuilder.SavePrefab(root, FighterPrefabPath);
        }

        /// <summary>Pronin fighter (GDD: fast, hunts small ships): a white arrowhead with cyan glow.</summary>
        public static ShipAgent LoadOrCreateProninFighter()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProninFighterPrefabPath);
            if (existing != null)
            {
                return existing.GetComponent<ShipAgent>();
            }

            GameObject root = new GameObject("ProninFighter");
            Material hull = LitMaterial("ProninHull", ProninHullColor);
            Material glow = ShipPrefabBuilder.LoadOrCreateMaterial("ProninGlow", UnlitShaderName, ProninGlowColor);

            ShipPrefabBuilder.CreatePart(root.transform, PrimitiveType.Capsule, "Body", hull, new Vector3(0f, 0f, 0.05f), new Vector3(0.22f, 0.5f, 0.16f))
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ShipPrefabBuilder.CreatePart(root.transform, PrimitiveType.Cube, "Wing Port", hull, new Vector3(-0.3f, 0f, -0.15f), new Vector3(0.55f, 0.03f, 0.25f))
                .transform.localRotation = Quaternion.Euler(0f, -30f, 0f);
            ShipPrefabBuilder.CreatePart(root.transform, PrimitiveType.Cube, "Wing Starboard", hull, new Vector3(0.3f, 0f, -0.15f), new Vector3(0.55f, 0.03f, 0.25f))
                .transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
            ShipPrefabBuilder.CreatePart(root.transform, PrimitiveType.Cube, "Sensor Line", glow, new Vector3(0f, 0.08f, 0.2f), new Vector3(0.04f, 0.02f, 0.5f));

            Transform engineGlow = ShipPrefabBuilder.CreatePart(root.transform, PrimitiveType.Sphere, "Engine Glow", glow,
                new Vector3(0f, 0f, -0.5f), Vector3.one * 0.2f).transform;
            TrailRenderer trail = ShipPrefabBuilder.CreateTrail(root.transform, new Vector3(0f, 0f, -0.55f), 0.15f, 3f, glow);
            LineRenderer bracket = ShipPrefabBuilder.CreateBracket(root.transform);
            LineRenderer beam = CreateBeam(root.transform, new Vector3(0f, 0f, 0.6f));

            ShipAgent agent = root.AddComponent<ShipAgent>();
            ShipMovementProfile profile = ShipPrefabBuilder.LoadOrCreateProfile("ProninFighterMovement", ConfigureProninFighterMovement);
            ConfigureAgent(agent, "ProninFighter", ShipSize.Small, profile);

            ShipPrefabBuilder.AddPresentation(root, engineGlow, trail, bracket, 18f, 1f);
            ShipPrefabBuilder.AddDurability(root, ProninFighterHull);
            ShipPrefabBuilder.AddFuel(root, ProninFighterTank, ProninFighterFuelPerKlick, ProninFighterFuelPerAU);
            AddShield(root, 40f, 5f, 5f);
            // A slow, hard-hitting beam in a slightly wider cone; energy hits at once.
            AddWeapons(root, new WeaponSettings(HalfZoneRangeUnits, 7f, 0.9f, 0.8f, 0.05f, firingArcDegrees: 40f));
            BeamShotView beamView = root.AddComponent<BeamShotView>();
            EditorAssetUtility.SetReferences(beamView, ("_beam", beam));
            // Hunters pick their prey by the hunting rules; idle self-defense would grab the nearest ship instead.
            FighterCombat combat = root.AddComponent<FighterCombat>();
            SerializedObject combatData = new SerializedObject(combat);
            EditorAssetUtility.FindProperty(combatData, "_defendWhenIdle").boolValue = false;
            combatData.ApplyModifiedPropertiesWithoutUndo();
            root.AddComponent<AutonomousHunting>();
            CombatEffectsBuilder.AddHitFeedback(root, 0.8f, 0.12f);
            CombatEffectsBuilder.AddDestruction(root, 1.2f);
            // GDD: destroyed enemy fighters leave wreckage that yields metal.
            ShipWreckage wreckage = root.AddComponent<ShipWreckage>();
            SerializedObject wreckageData = new SerializedObject(wreckage);
            EditorAssetUtility.FindProperty(wreckageData, "_metalAmount").floatValue = ProninFighterWreckMetal;
            wreckageData.ApplyModifiedPropertiesWithoutUndo();
            return ShipPrefabBuilder.SavePrefab(root, ProninFighterPrefabPath);
        }

        /// <summary>Faster to turn than the civilian capital ships, still a flying building.</summary>
        static void ConfigureCruiserMovement(SerializedObject profile)
        {
            EditorAssetUtility.FindProperty(profile, "_motion._maximumSpeed").floatValue = 14f;
            EditorAssetUtility.FindProperty(profile, "_motion._mainDriveAcceleration").floatValue = 1.2f;
            EditorAssetUtility.FindProperty(profile, "_motion._turnRateDegreesPerSecond").floatValue = 14f;
            EditorAssetUtility.FindProperty(profile, "_motion._thrusterAcceleration").floatValue = 0.8f;
            EditorAssetUtility.FindProperty(profile, "_motion._thrusterMaximumSpeed").floatValue = 2.5f;
            EditorAssetUtility.FindProperty(profile, "_motion._maneuverRangeUnits").floatValue = 40f;
            EditorAssetUtility.FindProperty(profile, "_motion._arrivalToleranceUnits").floatValue = 1.5f;
            EditorAssetUtility.FindProperty(profile, "_motion._alignToleranceDegrees").floatValue = 4f;
            EditorAssetUtility.FindProperty(profile, "_jumpChargeSeconds").floatValue = 12f;
        }

        /// <summary>The most agile human ship (GDD: big ships feel heavy, fighters nimble).</summary>
        static void ConfigureFighterMovement(SerializedObject profile)
        {
            EditorAssetUtility.FindProperty(profile, "_motion._maximumSpeed").floatValue = 60f;
            EditorAssetUtility.FindProperty(profile, "_motion._mainDriveAcceleration").floatValue = 24f;
            EditorAssetUtility.FindProperty(profile, "_motion._turnRateDegreesPerSecond").floatValue = 180f;
            EditorAssetUtility.FindProperty(profile, "_motion._thrusterAcceleration").floatValue = 7f;
            EditorAssetUtility.FindProperty(profile, "_motion._thrusterMaximumSpeed").floatValue = 8f;
            EditorAssetUtility.FindProperty(profile, "_jumpChargeSeconds").floatValue = 3f;
        }

        /// <summary>A little faster than a human fighter, so civilian ships cannot simply outrun it.</summary>
        static void ConfigureProninFighterMovement(SerializedObject profile)
        {
            EditorAssetUtility.FindProperty(profile, "_motion._maximumSpeed").floatValue = 65f;
            EditorAssetUtility.FindProperty(profile, "_motion._mainDriveAcceleration").floatValue = 26f;
            EditorAssetUtility.FindProperty(profile, "_motion._turnRateDegreesPerSecond").floatValue = 200f;
            EditorAssetUtility.FindProperty(profile, "_motion._thrusterAcceleration").floatValue = 8f;
            EditorAssetUtility.FindProperty(profile, "_motion._thrusterMaximumSpeed").floatValue = 9f;
            EditorAssetUtility.FindProperty(profile, "_jumpChargeSeconds").floatValue = 3f;
        }

        static void ConfigureAgent(ShipAgent agent, string className, ShipSize size, ShipMovementProfile profile)
        {
            SerializedObject agentData = new SerializedObject(agent);
            EditorAssetUtility.FindProperty(agentData, "_className").stringValue = className;
            EditorAssetUtility.FindProperty(agentData, "_size").enumValueIndex = (int)size;
            EditorAssetUtility.FindProperty(agentData, "_movementProfile").objectReferenceValue = profile;
            agentData.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Adds weapons with the given balance values; the settings type validates them first.</summary>
        static void AddWeapons(GameObject root, WeaponSettings settings)
        {
            ShipWeapons weapons = root.AddComponent<ShipWeapons>();
            SerializedObject serialized = new SerializedObject(weapons);
            EditorAssetUtility.FindProperty(serialized, "_weapon._rangeUnits").floatValue = settings.RangeUnits;
            EditorAssetUtility.FindProperty(serialized, "_weapon._damagePerHit").floatValue = settings.DamagePerHit;
            EditorAssetUtility.FindProperty(serialized, "_weapon._secondsBetweenShots").floatValue = settings.SecondsBetweenShots;
            EditorAssetUtility.FindProperty(serialized, "_weapon._roundsPerBurst").intValue = settings.RoundsPerBurst;
            EditorAssetUtility.FindProperty(serialized, "_weapon._secondsBetweenBursts").floatValue = settings.SecondsBetweenBursts;
            EditorAssetUtility.FindProperty(serialized, "_weapon._closeHitChance").floatValue = settings.HitChance(0f);
            EditorAssetUtility.FindProperty(serialized, "_weapon._longHitChance").floatValue = settings.HitChance(settings.RangeUnits);
            EditorAssetUtility.FindProperty(serialized, "_weapon._firingArcDegrees").floatValue = settings.FiringArcDegrees;
            EditorAssetUtility.FindProperty(serialized, "_weapon._projectileSpeedUnitsPerSecond").floatValue = settings.ProjectileSpeedUnitsPerSecond;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddShield(GameObject root, float maximum, float rechargePerSecond, float rechargeDelaySeconds)
        {
            ShipShield shield = root.AddComponent<ShipShield>();
            SerializedObject serialized = new SerializedObject(shield);
            EditorAssetUtility.FindProperty(serialized, "_maximum").floatValue = maximum;
            EditorAssetUtility.FindProperty(serialized, "_rechargePerSecond").floatValue = rechargePerSecond;
            EditorAssetUtility.FindProperty(serialized, "_rechargeDelaySeconds").floatValue = rechargeDelaySeconds;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Human projectile weapons (GDD: muzzle flashes and tracers): one tracer streak template the
        /// view copies for every round in flight, and a warm flash it moves to the firing muzzle.
        /// </summary>
        static void AddProjectileView(GameObject root, Transform[] muzzles, float streakWidth, float streakLengthUnits, float flashSize)
        {
            LineRenderer streak = CreateWorldLine(root.transform, "Tracer Round", "HumanTracer", HumanTracerColor, streakWidth);
            // Bright at the head, fading towards the tail like a tracer round.
            streak.widthCurve = AnimationCurve.Linear(0f, 0.4f, 1f, 1f);
            Transform flash = ShipPrefabBuilder.CreatePart(root.transform, PrimitiveType.Sphere, "Muzzle Flash",
                ShipPrefabBuilder.LoadOrCreateMaterial("MuzzleFlash", UnlitShaderName, MuzzleFlashColor), muzzles[0].localPosition,
                Vector3.one * flashSize).transform;

            ProjectileShotView view = root.AddComponent<ProjectileShotView>();
            SerializedObject serialized = new SerializedObject(view);
            SerializedProperty muzzleArray = EditorAssetUtility.FindProperty(serialized, "_muzzles");
            muzzleArray.arraySize = muzzles.Length;
            for (int i = 0; i < muzzles.Length; i++)
            {
                muzzleArray.GetArrayElementAtIndex(i).objectReferenceValue = muzzles[i];
            }
            EditorAssetUtility.FindProperty(serialized, "_streakTemplate").objectReferenceValue = streak;
            EditorAssetUtility.FindProperty(serialized, "_muzzleFlash").objectReferenceValue = flash;
            EditorAssetUtility.FindProperty(serialized, "_streakLengthUnits").floatValue = streakLengthUnits;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Pronin energy weapon: a clean beam line from the emitter (GDD: clear, clean beams).</summary>
        static LineRenderer CreateBeam(Transform parent, Vector3 emitterLocalPosition)
        {
            LineRenderer beam = CreateWorldLine(parent, "Energy Beam", "ProninBeam", ProninBeamColor, 0.05f);
            beam.transform.localPosition = emitterLocalPosition;
            beam.enabled = false;
            return beam;
        }

        static Transform CreateMuzzle(Transform parent, string name, Vector3 localPosition)
        {
            Transform muzzle = new GameObject(name).transform;
            muzzle.SetParent(parent, false);
            muzzle.localPosition = localPosition;
            return muzzle;
        }

        /// <summary>A bright world-space line for weapon fire (GDD: weapon fire stays visible at every zoom level).</summary>
        static LineRenderer CreateWorldLine(Transform parent, string name, string materialName, Color color, float width)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = ShipPrefabBuilder.LoadOrCreateMaterial(materialName, UnlitShaderName, color);
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = width;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        static Material LitMaterial(string name, Color color)
        {
            return ShipPrefabBuilder.LoadOrCreateMaterial(name, LitShaderName, color);
        }
    }
}
