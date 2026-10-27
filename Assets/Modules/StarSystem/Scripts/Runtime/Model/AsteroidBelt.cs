namespace LastFleet.StarSystem
{
    public enum AsteroidBeltComposition
    {
        Metallic,
        Icy,
        GasRich,
        Mixed
    }

    public sealed class AsteroidBelt
    {
        public AsteroidBelt(string name, float innerRadiusAU, float outerRadiusAU, AsteroidBeltComposition composition)
        {
            Name = name;
            InnerRadiusAU = innerRadiusAU;
            OuterRadiusAU = outerRadiusAU;
            Composition = composition;
        }

        public string Name { get; }
        public float InnerRadiusAU { get; }
        public float OuterRadiusAU { get; }
        public AsteroidBeltComposition Composition { get; }
        public float CenterRadiusAU => (InnerRadiusAU + OuterRadiusAU) * 0.5f;
        public bool IsFuelSource => Composition == AsteroidBeltComposition.GasRich;
    }
}
