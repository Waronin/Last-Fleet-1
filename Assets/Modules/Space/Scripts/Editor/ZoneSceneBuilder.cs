using LastFleet.CameraControl.Editor;
using LastFleet.Shared.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LastFleet.Space.Editor
{
    /// <summary>
    /// Builds the zone scene in which ships are placed: sun, starfield, zone grid, RTS camera and
    /// post-processing. Menu: Last Fleet / Create Zone Scene.
    /// </summary>
    public static class ZoneSceneBuilder
    {
        const string ScenePath = "Assets/Modules/Space/Scenes/Zone.unity";
        const float CameraStartDistance = 120f;

        [MenuItem("Last Fleet/Create Zone Scene")]
        static void CreateFromMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Zone scene exists", $"Overwrite {ScenePath}?", "Overwrite", "Cancel"))
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
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Light sun = SpaceSceneFactory.CreateSun(Quaternion.Euler(35f, -60f, 0f));
            SpaceSceneFactory.CreateEnvironment(sun);
            SpaceSceneFactory.CreateGlobalVolume();
            SpaceZone zone = CreateZone();
            CameraRigFactory.Create(zone.Center, zone.RadiusUnits, CameraStartDistance);

            EditorAssetUtility.EnsureFolder("Assets/Modules/Space/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorAssetUtility.AddSceneToBuildSettings(ScenePath, false);
            Debug.Log($"Zone scene created at {ScenePath}.");
        }

        static SpaceZone CreateZone()
        {
            GameObject zoneObject = new GameObject("Zone");
            SpaceZone zone = zoneObject.AddComponent<SpaceZone>();

            GameObject grid = GameObject.CreatePrimitive(PrimitiveType.Quad);
            grid.name = "Grid";
            Object.DestroyImmediate(grid.GetComponent<Collider>());
            grid.transform.SetParent(zoneObject.transform, false);
            grid.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            MeshRenderer gridRenderer = grid.GetComponent<MeshRenderer>();
            gridRenderer.sharedMaterial = SpaceSceneFactory.LoadOrCreateGridMaterial();
            gridRenderer.shadowCastingMode = ShadowCastingMode.Off;
            gridRenderer.receiveShadows = false;

            EditorAssetUtility.SetReferences(zone, ("_gridRenderer", gridRenderer));
            return zone;
        }
    }
}
