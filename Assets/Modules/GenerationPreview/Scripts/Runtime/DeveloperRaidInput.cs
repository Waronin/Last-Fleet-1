using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastFleet.GenerationPreview
{
    /// <summary>
    /// Developer stand-in for enemy waves until attacks are scheduled: P drops a squad of Pronin
    /// fighters at the edge of the viewed zone, which then hunt on their own.
    /// </summary>
    public sealed class DeveloperRaidInput : MonoBehaviour
    {
        const float ArrivalDistanceFraction = 0.6f;

        [SerializeField] GenerationPreviewController _controller;
        [SerializeField] ShipRegistry _pronin;
        [SerializeField] ShipAgent _proninFighterPrefab;
        [SerializeField, Min(1)] int _fightersPerRaid = 3;

        void Awake()
        {
            if (_controller == null || _pronin == null || _proninFighterPrefab == null)
            {
                Debug.LogError("DeveloperRaidInput needs the preview controller, the Pronin registry and the Pronin fighter prefab.", this);
                enabled = false;
            }
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            ZoneView zone = _controller.CurrentZone;
            if (keyboard == null || zone == null || !keyboard.pKey.wasPressedThisFrame)
            {
                return;
            }

            // A random point towards the zone's edge, facing the center.
            Vector2 direction = Random.insideUnitCircle.normalized;
            Vector3 position = new Vector3(direction.x, 0f, direction.y) * (zone.Zone.RadiusUnits * ArrivalDistanceFraction);
            float headingDegrees = ShipMotion.DirectionToHeading(-direction);
            _pronin.SpawnSquad(_proninFighterPrefab, zone, position, headingDegrees, _fightersPerRaid);
            _controller.ShowNotice(GameText.Verbatim($"{_fightersPerRaid} Pronin fighters jumped into {zone.Record.PointOfInterest.Name}."));
        }
    }
}
