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
            : this(name, type, localPosition, headingDegrees, profile, null)
        {
        }

        /// <summary>A stranded capital ship: a station of type <see cref="StationType.StrandedShip"/> with a hull to repair.</summary>
        public ZoneStation(string name, Vector3 localPosition, float headingDegrees, StationProfile profile, StrandedHull strandedHull)
            : this(name, StationType.StrandedShip, localPosition, headingDegrees, profile,
                strandedHull ?? throw new ArgumentNullException(nameof(strandedHull)))
        {
        }

        ZoneStation(string name, StationType type, Vector3 localPosition, float headingDegrees, StationProfile profile, StrandedHull strandedHull)
        {
            if ((type == StationType.StrandedShip) != (strandedHull != null))
            {
                throw new ArgumentException("Exactly the stranded ships have a stranded hull.", nameof(type));
            }
            Name = name;
            StrandedHull = strandedHull;
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

        /// <summary>The hull of a stranded capital ship; null for an ordinary station.</summary>
        public StrandedHull StrandedHull { get; }

        public bool IsStrandedShip => StrandedHull != null;

        /// <summary>
        /// The people aboard during the run, who may join the fleet; starts from the profile's occupants.
        /// Survivors of a stranded ship all want to leave it.
        /// </summary>
        public StationResidents Residents => _residents ??= new StationResidents(Profile, IsStrandedShip);

        StationResidents _residents;

        public float FootprintRadiusUnits =>
            IsStrandedShip ? StrandedShipClasses.FootprintRadiusUnits(StrandedHull.Class) : StationTypes.FootprintRadiusUnits(Type);

        public int DockingPortCount => IsStrandedShip ? StrandedShipClasses.DockingPortCount : StationTypes.DockingPortCount(Type);

        /// <summary>Rough length for players, e.g. "about 5 km".</summary>
        public float NominalLengthKlicks =>
            IsStrandedShip ? StrandedShipClasses.NominalLengthKlicks(StrandedHull.Class) : StationTypes.NominalLengthKlicks(Type);

        /// <summary>Null until the station has been scanned.</summary>
        public StationScanReport ScanReport { get; private set; }

        public bool IsScanned => ScanReport != null;
        public float ScanStandoffUnits => FootprintRadiusUnits + ScanStandoffMarginUnits;
        public float ScanDurationFactor => IsStrandedShip ? StrandedShipClasses.ScanDurationFactor : StationTypes.ScanDurationFactor(Type);

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

        public bool HasContactToTalkTo => Contact != null && Contact.HasSomethingToSay && !IsGone;

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

        /// <summary>A stranded ship that joined the fleet; like a destroyed station, the hulk is no longer in the zone.</summary>
        public bool IsRecovered => IsStrandedShip && StrandedHull.IsRecovered;

        /// <summary>Destroyed or recovered: nothing to dock at, search, hail, pick or fly around anymore.</summary>
        public bool IsGone => IsDestroyed || IsRecovered;
    }
}
