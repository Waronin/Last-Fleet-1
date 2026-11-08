using System.Collections.Generic;
using LastFleet.Localization;
using LastFleet.Mining;
using LastFleet.Provisions;
using LastFleet.ShipControl;
using UnityEngine;

namespace LastFleet.Hud
{
    /// <summary>
    /// Resource bar at the top of the screen (GDD: HUD, resource bar) with the fleet's stock of fuel,
    /// metal and water. Prototype UI in IMGUI; a colored swatch stands in for each supply's icon.
    /// Texts are resolved once per frame on Layout.
    /// </summary>
    public sealed class SupplyBar : MonoBehaviour
    {
        const float Margin = 10f;
        const float SwatchSizePixels = 12f;
        const float EntrySpacingPixels = 18f;

        static readonly Supply[] ShownSupplies = { Supply.Fuel, Supply.Metal, Supply.Water };

        [SerializeField] MiningCoordinator _mining;
        [SerializeField] ShipCommandInput _shipInput;
        [Tooltip("Optional. Adds how long the water lasts at the current population.")]
        [SerializeField] FleetWaterSupply _water;

        readonly List<string> _resolvedEntries = new List<string>();

        void Awake()
        {
            if (_mining == null || _shipInput == null)
            {
                Debug.LogError("SupplyBar needs the mining coordinator for the fleet's supplies and the ship command input to block clicks behind the bar.", this);
                enabled = false;
            }
        }

        void OnDisable()
        {
            if (_shipInput != null)
            {
                _shipInput.ClearBlockedScreenArea(this);
            }
        }

        void OnGUI()
        {
            // Refresh only on Layout: IMGUI needs the same controls in every event of a frame.
            if (Event.current.type == EventType.Layout)
            {
                ResolveEntries();
            }

            GUILayout.BeginArea(new Rect(0f, Margin, Screen.width, Screen.height - Margin * 2f));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(GUI.skin.box);
            for (int i = 0; i < _resolvedEntries.Count; i++)
            {
                if (i > 0)
                {
                    GUILayout.Space(EntrySpacingPixels);
                }
                DrawSwatch(ShownSupplies[i]);
                GUILayout.Label(_resolvedEntries[i]);
            }
            GUILayout.EndHorizontal();

            if (Event.current.type == EventType.Repaint)
            {
                Rect bar = GUILayoutUtility.GetLastRect();
                // GUI coordinates start top left, Input System screen coordinates bottom left.
                _shipInput.SetBlockedScreenArea(this, new Rect(bar.x, Screen.height - Margin - bar.yMax, bar.width, bar.height));
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void ResolveEntries()
        {
            FleetSupplies supplies = _mining.Supplies;
            _resolvedEntries.Clear();
            foreach (Supply supply in ShownSupplies)
            {
                string entry = GameTextResolver.Resolve(MiningTexts.SupplyAmount.Format(("supply", MiningTexts.Supplies.For(supply)),
                    ("amount", supplies.AmountOf(supply)), ("capacity", supplies.CapacityOf(supply))));
                if (supply == Supply.Water && _water != null && !float.IsInfinity(_water.MinutesLeft))
                {
                    entry += " " + GameTextResolver.Resolve(ProvisionsTexts.WaterMinutesLeft.Format(("minutes", Mathf.FloorToInt(_water.MinutesLeft))));
                }
                _resolvedEntries.Add(entry);
            }
        }

        static void DrawSwatch(Supply supply)
        {
            Rect swatch = GUILayoutUtility.GetRect(SwatchSizePixels, SwatchSizePixels, GUILayout.ExpandWidth(false));
            swatch.y += (GUI.skin.label.lineHeight - SwatchSizePixels) * 0.5f + GUI.skin.label.margin.top;
            swatch.height = SwatchSizePixels;
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(swatch, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, ColorOf(supply), 0f, 2f);
            }
        }

        static Color ColorOf(Supply supply)
        {
            return supply switch
            {
                Supply.Fuel => new Color(1f, 0.6f, 0.25f),
                Supply.Metal => new Color(0.7f, 0.74f, 0.8f),
                _ => new Color(0.4f, 0.7f, 1f)
            };
        }
    }
}
