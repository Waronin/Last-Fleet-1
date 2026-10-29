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
        public static readonly EnumTextKeys<StationAtmosphere> Atmospheres = new EnumTextKeys<StationAtmosphere>(Table, "atmosphere");
        public static readonly EnumTextKeys<StationCondition> Conditions = new EnumTextKeys<StationCondition>(Table, "condition");
        public static readonly EnumTextKeys<StationRadiation> Radiation = new EnumTextKeys<StationRadiation>(Table, "radiation");
        public static readonly EnumTextKeys<StationAttitude> Attitudes = new EnumTextKeys<StationAttitude>(Table, "attitude");
    }
}
