namespace LastFleet.Zones
{
    /// <summary>
    /// What the station said when hailed (GDD: hail the crew to find out whether they are hostile).
    /// For now the answer is always the truth; lies come with the quest system. Silence does not prove
    /// a station is empty: a scan shows the life signs.
    /// </summary>
    public sealed class StationHailResponse
    {
        public StationHailResponse(StationAttitude claimedAttitude)
        {
            ClaimedAttitude = claimedAttitude;
        }

        /// <summary>The attitude the station claims; None means nobody answered.</summary>
        public StationAttitude ClaimedAttitude { get; }

        public bool Answered => ClaimedAttitude != StationAttitude.None;
    }
}
