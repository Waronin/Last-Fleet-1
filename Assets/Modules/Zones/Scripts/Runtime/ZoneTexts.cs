using LastFleet.Shared.Text;

namespace LastFleet.Zones
{
    /// <summary>Display names of zone content, in the string table "Zones".</summary>
    public static class ZoneTexts
    {
        const string Table = "Zones";

        public static readonly EnumTextKeys<ResourceKind> Resources = new EnumTextKeys<ResourceKind>(Table, "resource");
        public static readonly EnumTextKeys<DepositShape> Shapes = new EnumTextKeys<DepositShape>(Table, "shape");
        public static readonly EnumTextKeys<DepositHazard> Hazards = new EnumTextKeys<DepositHazard>(Table, "hazard");
        public static readonly EnumTextKeys<DepositSizeClass> SizeClasses = new EnumTextKeys<DepositSizeClass>(Table, "size");
        public static readonly EnumTextKeys<StationType> StationTypeNames = new EnumTextKeys<StationType>(Table, "station-type");
    }
}
