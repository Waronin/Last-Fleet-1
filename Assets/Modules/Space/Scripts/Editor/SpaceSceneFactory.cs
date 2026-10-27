using LastFleet.Shared.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastFleet.Space.Editor
{
    /// <summary>Creates the shared space backdrop (sky, sun, post-processing) for scene builders.</summary>
    public static class SpaceSceneFactory
    {
        const string MaterialFolder = "Assets/Modules/Space/Graphics/Materials";
        const string SkyboxMaterialPath = MaterialFolder + "/StarfieldSkybox.mat";
        const string GridMaterialPath = MaterialFolder + "/ZoneGrid.mat";
        const string SkyboxShaderName = "LastFleet/StarfieldSkybox";
        const string GridShaderName = "LastFleet/ZoneGrid";
        const string VolumeProfilePath = "Assets/Settings/SampleSceneProfile.asset";

        public static Light CreateSun(Quaternion rotation)
        {
            GameObject sunObject = new GameObject("Sun");
            sunObject.transform.rotation = rotation;
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.88f);
            sun.intensity = 1.6f;
            sun.shadows = LightShadows.Soft;
            return sun;
        }

        /// <param name="sun">Optional directional light shown as the sun disc in the sky.</param>
        public static SpaceEnvironment CreateEnvironment(Light sun)
        {
            Material skybox = EditorAssetUtility.LoadOrCreateMaterial(SkyboxMaterialPath, SkyboxShaderName);
            // Created inactive so OnEnable runs only after the references are assigned.
            GameObject environmentObject = new GameObject("Space Environment");
            environmentObject.SetActive(false);
            SpaceEnvironment environment = environmentObject.AddComponent<SpaceEnvironment>();
            EditorAssetUtility.SetReferences(environment, ("_sun", sun), ("_skybox", skybox));
            environmentObject.SetActive(true);
            return environment;
        }

        /// <summary>Adds a global volume with the project's bloom and tonemapping profile.</summary>
        public static Volume CreateGlobalVolume()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                Debug.LogWarning($"Volume profile {VolumeProfilePath} not found, scene is created without post-processing.");
                return null;
            }

            GameObject volumeObject = new GameObject("Global Volume");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            return volume;
        }

        public static Material LoadOrCreateGridMaterial()
        {
            return EditorAssetUtility.LoadOrCreateMaterial(GridMaterialPath, GridShaderName);
        }
    }
}
