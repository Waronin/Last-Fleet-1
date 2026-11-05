namespace LastFleet.Zones
{
    /// <summary>
    /// A change to a zone's generated contents that comes from outside the generator, such as a quest
    /// station. It is applied once, right after the zone is generated, or at once if the zone is
    /// already revealed. The result is run state like everything else in the zone.
    /// </summary>
    public interface IZoneAmendment
    {
        void Apply(ZoneRecord zone, ZoneContents contents);
    }
}
