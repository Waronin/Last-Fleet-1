using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastFleet.Shared.Blockout
{
    /// <summary>
    /// Creates collider-free primitive placeholders and line renderers for blockout views,
    /// colored per instance through a property block so all of them can share one material.
    /// </summary>
    public static class BlockoutPrimitives
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public static Renderer CreatePrimitive(PrimitiveType type, Transform parent, Material material, string name)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            DestroyObject(primitive.GetComponent<Collider>());

            Renderer renderer = primitive.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        public static LineRenderer CreateLine(Transform parent, Material material, string name, float widthUnits)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.widthMultiplier = widthUnits;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        /// <summary>Writes a circle on the XZ plane of the line's local space.</summary>
        public static void SetCircle(LineRenderer line, Vector3 localCenter, float radius, int segmentCount)
        {
            if (segmentCount < 3)
            {
                throw new ArgumentOutOfRangeException(nameof(segmentCount), "A circle needs at least 3 segments.");
            }

            line.loop = true;
            line.positionCount = segmentCount;
            for (int i = 0; i < segmentCount; i++)
            {
                float angle = i * Mathf.PI * 2f / segmentCount;
                line.SetPosition(i, localCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        public static void SetColor(Renderer renderer, Color color, MaterialPropertyBlock propertyBlock)
        {
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(propertyBlock);
        }

        /// <summary>Destroys every child of a view root, in edit mode as well as in play mode.</summary>
        public static void DestroyChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                DestroyObject(root.GetChild(i).gameObject);
            }
        }

        static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
