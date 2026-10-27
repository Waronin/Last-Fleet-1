namespace LastFleet.StarSystem
{
    public sealed class Moon
    {
        public Moon(string name, float orbitRadiusAU, float orbitAngleDegrees)
        {
            Name = name;
            OrbitRadiusAU = orbitRadiusAU;
            OrbitAngleDegrees = orbitAngleDegrees;
        }

        public string Name { get; }

        /// <summary>Distance from the parent planet.</summary>
        public float OrbitRadiusAU { get; }

        public float OrbitAngleDegrees { get; }
    }
}
