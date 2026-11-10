using LastFleet.Shared.Blockout;
using LastFleet.Shared.Editor;
using LastFleet.TacticalWeapons;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastFleet.Ships.Editor
{
    /// <summary>
    /// Blockout tactical weapons: the EMP launcher of the cruiser, the EMP missile and its blast ring,
    /// and the aiming ring's material. EMP effects are cold blue-white, apart from the warm explosions.
    /// </summary>
    public static class TacticalWeaponPrefabBuilder
    {
        const string PrefabFolder = "Assets/Modules/TacticalWeapons/Prefabs";
        const string MissilePrefabPath = PrefabFolder + "/EmpMissile.prefab";
        const string BlastPrefabPath = PrefabFolder + "/EmpBlast.prefab";
        const string UnlitShaderName = ShipPrefabBuilder.UnlitShaderName;
        const int RingSegments = 96;

        static readonly Color MissileColor = new Color(1.6f, 2.4f, 4f);
        static readonly Color MissileTrailColor = new Color(0.6f, 1.2f, 3f);
        static readonly Color BlastColor = new Color(0.9f, 1.8f, 4f);
        static readonly Color TargetingColor = new Color(0.5f, 1.1f, 2f);

        /// <summary>The cruiser's missile bay; all balance values live on the component's defaults.</summary>
        public static void AddEmpLauncher(GameObject root)
        {
            root.AddComponent<EmpLauncher>();
        }

        public static void RebuildAll()
        {
            AssetDatabase.DeleteAsset(MissilePrefabPath);
            AssetDatabase.DeleteAsset(BlastPrefabPath);
            LoadOrCreateMissile();
            LoadOrCreateBlast();
        }

        /// <summary>The ring that marks stunned ships, in the blast's color.</summary>
        public static Material LoadOrCreateStunMarkerMaterial()
        {
            return ShipPrefabBuilder.LoadOrCreateMaterial("EmpStunMarker", UnlitShaderName, BlastColor);
        }

        public static Material LoadOrCreateTargetingMaterial()
        {
            return ShipPrefabBuilder.LoadOrCreateMaterial("EmpTargeting", UnlitShaderName, TargetingColor);
        }

        public static EmpMissile LoadOrCreateMissile()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(MissilePrefabPath);
            if (existing != null)
            {
                return existing.GetComponent<EmpMissile>();
            }

            GameObject root = new GameObject("EmpMissile");
            Material glow = ShipPrefabBuilder.LoadOrCreateMaterial("EmpMissile", UnlitShaderName, MissileColor);
            Renderer body = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Capsule, root.transform, glow, "Body");
            // The capsule's long axis is Y; lay it along the flight direction.
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.6f, 1.2f, 0.6f);
            body.shadowCastingMode = ShadowCastingMode.Off;
            ShipPrefabBuilder.CreateTrail(root.transform, new Vector3(0f, 0f, -1.2f), 0.8f, 1.5f,
                ShipPrefabBuilder.LoadOrCreateMaterial("EmpMissileTrail", UnlitShaderName, MissileTrailColor));
            root.AddComponent<EmpMissile>();
            return Save(root, MissilePrefabPath).GetComponent<EmpMissile>();
        }

        public static EmpBlastEffect LoadOrCreateBlast()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(BlastPrefabPath);
            if (existing != null)
            {
                return existing.GetComponent<EmpBlastEffect>();
            }

            GameObject root = new GameObject("EmpBlast");
            LineRenderer ring = BlockoutPrimitives.CreateLine(root.transform,
                ShipPrefabBuilder.LoadOrCreateMaterial("EmpBlast", UnlitShaderName, BlastColor), "Blast Ring", 2f);
            BlockoutPrimitives.SetCircle(ring, Vector3.zero, 1f, RingSegments);
            EmpBlastEffect effect = root.AddComponent<EmpBlastEffect>();
            EditorAssetUtility.SetReferences(effect, ("_ring", ring));
            return Save(root, BlastPrefabPath).GetComponent<EmpBlastEffect>();
        }

        static GameObject Save(GameObject root, string path)
        {
            EditorAssetUtility.EnsureFolder(PrefabFolder);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
