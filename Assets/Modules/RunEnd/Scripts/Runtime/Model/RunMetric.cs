namespace LastFleet.RunEnd
{
    /// <summary>What a line of the run's account counts; the key for comparing runs.</summary>
    public enum RunMetric
    {
        PeopleRescued,
        Survivors,
        SmallShipsGained,
        CapitalShipsGained,
        CluesFound,
        Victory,
        PeopleLost,
        ShipsLost
    }

    public static class RunMetrics
    {
        /// <summary>Losses improve when they shrink; everything else when it grows.</summary>
        public static bool IsLowerBetter(RunMetric metric)
        {
            return metric == RunMetric.PeopleLost || metric == RunMetric.ShipsLost;
        }
    }
}
