using UnityEngine;

namespace LastFleet.Galaxy
{
    /// <summary>
    /// A star system as a point in the galaxy. Its contents are generated later from
    /// <see cref="SystemSeed"/> when the fleet jumps in.
    /// </summary>
    public sealed class StarSystemNode
    {
        public StarSystemNode(int index, string name, Vector3 positionLightYears, ulong systemSeed)
        {
            Index = index;
            Name = name;
            PositionLightYears = positionLightYears;
            SystemSeed = systemSeed;
        }

        public int Index { get; }
        public string Name { get; }
        public Vector3 PositionLightYears { get; }
        public ulong SystemSeed { get; }
    }
}
