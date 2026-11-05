using System;
using UnityEngine;

namespace LastFleet.Zones
{
    /// <summary>
    /// A station inside a station zone. It lies on the zone plane, where ships fly, so its docking
    /// ports can be reached. What it really is (<see cref="Profile"/>) stays hidden; a scan reveals
    /// part of it (<see cref="ScanReport"/>), a hail its attitude (<see cref="HailResponse"/>), and a docked
    /// crew sees the environment on site (<see cref="IsEnvironmentKnown"/>). All of this is run state and
    /// stays when the fleet leaves the zone.
    /// </summary>
    public sealed class ZoneStation : IScanTarget
    {
        /// <summary>Scanning ships keep about 2 km from the station's outer hull.</summary>
        public const float ScanStandoffMarginUnits = 20f;

        public ZoneStation(string name, StationType type, Vector3 localPosition, float headingDegrees, StationProfile profile)
        {
            Name = name;
            Type = type;
            LocalPosition = localPosition;
            HeadingDegrees = headingDegrees;
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public string Name { get; }
        public StationType Type { get; }
        public Vector3 LocalPosition { get; }
        public float HeadingDegrees { get; }
        public StationProfile Profile { get; }
        public float FootprintRadiusUnits => StationTypes.FootprintRadiusUnits(Type);
        public int DockingPortCount => StationTypes.DockingPortCount(Type);

        /// <summary>Null until the station has been scanned.</summary>
        public StationScanReport ScanReport { get; private set; }

        public bool IsScanned => ScanReport != null;
        public float ScanStandoffUnits => FootprintRadiusUnits + ScanStandoffMarginUnits;
        public float ScanDurationFactor => StationTypes.ScanDurationFactor(Type);

        public void CompleteScan(float accuracyRoll)
        {
            ScanReport ??= StationScanReport.FromProfile(Profile, accuracyRoll);
        }

        /// <summary>A ship of the fleet has docked: its crew has seen the air, radiation and condition on site.</summary>
        public bool WasVisited { get; private set; }

        /// <summary>Atmosphere, condition and radiation are known exactly, from a scan or from a docked crew.</summary>
        public bool IsEnvironmentKnown => IsScanned || WasVisited;

        public void MarkVisited()
        {
            WasVisited = true;
        }

        /// <summary>Null until the station has been hailed.</summary>
        public StationHailResponse HailResponse { get; private set; }

        public bool IsHailed => HailResponse != null;

        /// <summary>
        /// Hails the station once and returns its answer. The crew tells the truth for now; this is
        /// where a lying crew will answer with a false attitude later.
        /// </summary>
        public StationHailResponse Hail()
        {
            HailResponse ??= new StationHailResponse(Profile.Attitude);
            return HailResponse;
        }

        /// <summary>
        /// Someone aboard with something to say, e.g. a quest giver; null for an ordinary station.
        /// While it has something to say, a hail goes to it instead of the plain answer.
        /// </summary>
        public IStationContact Contact { get; private set; }

        public bool HasContactToTalkTo => Contact != null && Contact.HasSomethingToSay && !IsDestroyed;

        public void SetContact(IStationContact contact)
        {
            Contact = contact;
        }

        /// <summary>
        /// The radiation aboard follows someone else's rule, e.g. a quest's dose that builds up while docked;
        /// docked crews then suffer no random radiation deaths.
        /// </summary>
        public bool HasScriptedRadiation { get; private set; }

        public void UseScriptedRadiation()
        {
            HasScriptedRadiation = true;
        }

        /// <summary>Blown up, e.g. by a quest event; the station cannot be docked at, searched or hailed anymore.</summary>
        public bool IsDestroyed { get; private set; }

        public void MarkDestroyed()
        {
            IsDestroyed = true;
            Contact = null;
        }
    }
}
