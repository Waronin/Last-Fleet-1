using System;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Stations
{
    /// <summary>One model per station type; zones create station views through it.</summary>
    [CreateAssetMenu(menuName = "Last Fleet/Station Prefab Catalog", fileName = "StationPrefabs")]
    public sealed class StationPrefabCatalog : StationViewSource
    {
        [SerializeField] StationView[] _prefabs = new StationView[0];

        public bool TryGetPrefab(StationType type, out StationView prefab)
        {
            foreach (StationView candidate in _prefabs)
            {
                if (candidate != null && candidate.Type == type)
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
            if (!TryGetPrefab(station.Type, out StationView prefab))
            {
                throw new InvalidOperationException($"{name} has no prefab for station type {station.Type}.");
            }

            StationView view = Instantiate(prefab, zone, false);
            view.name = station.Name;
            view.transform.localPosition = station.LocalPosition;
            view.transform.localRotation = Quaternion.Euler(0f, station.HeadingDegrees, 0f);
            return view.transform;
        }
    }
}
