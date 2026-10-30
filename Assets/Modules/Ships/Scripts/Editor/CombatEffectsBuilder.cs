using LastFleet.Combat;
using LastFleet.Shared.Blockout;
using LastFleet.Shared.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastFleet.Ships.Editor
{
    /// <summary>
    /// Blockout combat effects for the ship prefabs: shield flash ring and hull sparks on a ship, and
    /// the shared explosion prefab that destroyed ships leave behind. Used by
    /// <see cref="CombatShipPrefabBuilder"/>.
    /// </summary>
    public static class CombatEffectsBuilder
    {
        const string ExplosionFolder = "Assets/Modules/Combat/Prefabs";
        const string ExplosionPrefabPath = ExplosionFolder + "/Explosion.prefab";
        const string UnlitShaderName = ShipPrefabBuilder.UnlitShaderName;
        const string ParticleShaderName = "Universal Render Pipeline/Particles/Unlit";
        const int RingSegments = 40;

        static readonly Color ShieldFlashColor = new Color(0.8f, 1.8f, 3f);
        static readonly Color SparkColor = new Color(4f, 2.2f, 0.6f);
        static readonly Color FireballColor = new Color(6f, 3f, 0.8f);
        static readonly Color ShockRingColor = new Color(3f, 2.4f, 1.6f);
        static readonly Color DebrisColor = new Color(0.35f, 0.35f, 0.38f);

        /// <summary>Shield ring (only with a shield radius above zero) and hull sparks.</summary>
        public static void AddHitFeedback(GameObject root, float shieldRadiusUnits, float sparkSize)
        {
            LineRenderer ring = null;
            if (shieldRadiusUnits > 0f)
            {
                ring = BlockoutPrimitives.CreateLine(root.transform,
                    ShipPrefabBuilder.LoadOrCreateMaterial("ShieldFlash", UnlitShaderName, ShieldFlashColor), "Shield Flash", shieldRadiusUnits * 0.12f);
                BlockoutPrimitives.SetCircle(ring, Vector3.zero, shieldRadiusUnits, RingSegments);
                ring.enabled = false;
            }

            ParticleSystem sparks = CreateBurstParticles(root.transform, "Hull Sparks",
                ShipPrefabBuilder.LoadOrCreateMaterial("HitSparks", ParticleShaderName, Color.white), SparkColor,
                sparkSize * 4f, sparkSize * 12f, sparkSize, 0.25f, 0.6f, 60);

            HitFeedbackView view = root.AddComponent<HitFeedbackView>();
            EditorAssetUtility.SetReferences(view, ("_shieldRing", ring), ("_sparks", sparks));
        }

        /// <summary>Explosion of the given size where the ship is destroyed.</summary>
        public static void AddDestruction(GameObject root, float explosionSize)
        {
            DestructionView view = root.AddComponent<DestructionView>();
            SerializedObject serialized = new SerializedObject(view);
            EditorAssetUtility.FindProperty(serialized, "_explosionPrefab").objectReferenceValue = LoadOrCreateExplosion();
            EditorAssetUtility.FindProperty(serialized, "_explosionSize").floatValue = explosionSize;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static ExplosionEffect LoadOrCreateExplosion()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPrefabPath);
            if (existing != null)
            {
                return existing.GetComponent<ExplosionEffect>();
            }

            GameObject root = new GameObject("Explosion");
            Renderer fireball = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Sphere, root.transform,
                ShipPrefabBuilder.LoadOrCreateMaterial("ExplosionFire", UnlitShaderName, FireballColor), "Fireball");
            fireball.shadowCastingMode = ShadowCastingMode.Off;
            fireball.transform.localScale = Vector3.zero;

            LineRenderer ring = BlockoutPrimitives.CreateLine(root.transform,
                ShipPrefabBuilder.LoadOrCreateMaterial("ExplosionRing", UnlitShaderName, ShockRingColor), "Shock Ring", 0.15f);
            BlockoutPrimitives.SetCircle(ring, Vector3.zero, 1f, RingSegments);

            ParticleSystem debris = CreateBurstParticles(root.transform, "Debris",
                ShipPrefabBuilder.LoadOrCreateMaterial("ExplosionDebris", ParticleShaderName, Color.white), DebrisColor,
                1.5f, 6f, 0.25f, 1f, 2f, 80);

            ExplosionEffect effect = root.AddComponent<ExplosionEffect>();
            EditorAssetUtility.SetReferences(effect, ("_fireball", fireball.transform), ("_shockRing", ring), ("_debris", debris));

            EditorAssetUtility.EnsureFolder(ExplosionFolder);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ExplosionPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<ExplosionEffect>();
        }

        /// <summary>
        /// Particles that only appear when emitted by code, flying off in all directions in world space,
        /// so they drift away instead of following the ship.
        /// </summary>
        static ParticleSystem CreateBurstParticles(Transform parent, string name, Material material, Color color,
            float minimumSpeed, float maximumSpeed, float size, float minimumLifetime, float maximumLifetime, int maximumParticles)
        {
            GameObject particleObject = new GameObject(name);
            particleObject.transform.SetParent(parent, false);

            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = true;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minimumLifetime, maximumLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minimumSpeed, maximumSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = maximumParticles;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = size;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particles;
        }
    }
}
