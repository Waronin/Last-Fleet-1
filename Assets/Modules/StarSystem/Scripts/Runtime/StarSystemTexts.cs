using LastFleet.Shared.Text;

namespace LastFleet.StarSystem
{
    /// <summary>Display names of star system content, in the string table "StarSystem".</summary>
    public static class StarSystemTexts
    {
        const string Table = "StarSystem";

        public static readonly EnumTextKeys<PointOfInterestKind> PointOfInterestKinds =
            new EnumTextKeys<PointOfInterestKind>(Table, "point-of-interest");
    }
}
