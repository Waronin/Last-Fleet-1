using LastFleet.Shared.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LastFleet.CameraControl.Editor
{
    /// <summary>Creates a configured RTS camera rig in the active scene for scene builders.</summary>
    public static class CameraRigFactory
    {
        const float DefaultPitch = 50f;
        const float DefaultYaw = 45f;
        const float FieldOfViewDegrees = 50f;

        public static RtsCameraRig Create(Vector3 boundsCenter, float boundsRadius, float startDistance)
        {
            GameObject rigObject = new GameObject("Camera Rig");
            RtsCameraRig rig = rigObject.AddComponent<RtsCameraRig>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(rigObject.transform, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = FieldOfViewDegrees;
            cameraObject.AddComponent<AudioListener>();
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            SerializedObject serialized = new SerializedObject(rig);
            EditorAssetUtility.FindProperty(serialized, "_camera").objectReferenceValue = camera;
            EditorAssetUtility.FindProperty(serialized, "_boundsCenter").vector3Value = boundsCenter;
            EditorAssetUtility.FindProperty(serialized, "_boundsRadius").floatValue = boundsRadius;
            EditorAssetUtility.FindProperty(serialized, "_startDistance").floatValue = startDistance;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Match the play-mode start view so the scene already looks right in the editor.
            rigObject.transform.position = boundsCenter;
            Quaternion rotation = Quaternion.Euler(DefaultPitch, DefaultYaw, 0f);
            cameraObject.transform.SetPositionAndRotation(boundsCenter - rotation * Vector3.forward * startDistance, rotation);
            camera.farClipPlane = startDistance + boundsRadius * 2f + 500f;
            return rig;
        }
    }
}
