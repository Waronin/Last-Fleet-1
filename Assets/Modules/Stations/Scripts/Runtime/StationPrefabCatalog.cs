using System;
using System.Collections.Generic;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Stations
{
    /// <summary>One model per station type, and one per class of stranded ship; zones create station views through it.</summary>
    [CreateAssetMenu(menuName = "Last Fleet/Station Prefab Catalog", fileName = "StationPrefabs")]
    public sealed class StationPrefabCatalog : StationViewSource
    {
        [SerializeField] StationView[] _prefabs = new StationView[0];

        public IReadOnlyList<StationView> Prefabs => _prefabs;

        public bool TryGetPrefab(ZoneStation station, out StationView prefab)
        {
            foreach (StationView candidate in _prefabs)
            {
                if (candidate != null && candidate.Shows(station))
                {
                    prefab = candidate;
                    return true;
                }
            }
            prefab = null;
            return false;
        }

        public override Transform CreateView(ZoneStation station, Transform zone)
        {
            if (!TryGetPrefab(station, out StationView prefab))
            {
                string kind = station.IsStrandedShip ? $"stranded {station.StrandedHull.Class}" : $"station type {station.Type}";
                throw new InvalidOperationException($"{name} has no prefab for {kind}.");
            }

            StationView view = Instantiate(prefab, zone, false);
            view.name = station.Name;
            view.transform.localPosition = station.LocalPosition;
            view.transform.localRotation = Quaternion.Euler(0f, station.HeadingDegrees, 0f);
            return view.transform;
        }
    }
}
