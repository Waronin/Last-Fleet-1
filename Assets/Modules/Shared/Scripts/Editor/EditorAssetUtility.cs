using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastFleet.Shared.Editor
{
    /// <summary>Asset and serialization helpers shared by the scene builders of all modules.</summary>
    public static class EditorAssetUtility
    {
        public static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetPath));
        }

        public static Material LoadOrCreateMaterial(string assetPath, string shaderName)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Shader '{shaderName}' was not found, cannot create {assetPath}.");
            }

            EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        public static void SetReferences(Object target, params (string FieldName, Object Value)[] references)
        {
            SerializedObject serialized = new SerializedObject(target);
            foreach ((string fieldName, Object value) in references)
            {
                FindProperty(serialized, fieldName).objectReferenceValue = value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static SerializedProperty FindProperty(SerializedObject serialized, string fieldName)
        {
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new ArgumentException($"{serialized.targetObject.GetType().Name} has no serialized field '{fieldName}'.");
            }
            return property;
        }

        /// <summary>Adds a scene to the build settings, optionally as the first (startup) scene.</summary>
        public static void AddSceneToBuildSettings(string scenePath, bool makeFirst)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path != scenePath)
                {
                    scenes.Add(scene);
                }
            }

            EditorBuildSettingsScene entry = new EditorBuildSettingsScene(scenePath, true);
            if (makeFirst)
            {
                scenes.Insert(0, entry);
            }
            else
            {
                scenes.Add(entry);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
