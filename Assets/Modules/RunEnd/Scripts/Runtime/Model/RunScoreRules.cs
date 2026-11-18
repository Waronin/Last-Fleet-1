using System;
using UnityEngine;

namespace LastFleet.RunEnd
{
    /// <summary>
    /// Points per achievement (GDD: Wertung, values are proposals for balancing). Only achievements score;
    /// losses appear in the account without taking points away, so a lost run still ends on what was saved.
    /// </summary>
    [Serializable]
    public sealed class RunScoreRules
    {
        [SerializeField, Min(0)] int _pointsPerRescuedPerson = 1;
        [SerializeField, Min(0)] int _pointsPerSurvivor = 1;
        [SerializeField, Min(0)] int _pointsPerSmallShipGained = 50;
        [SerializeField, Min(0)] int _pointsPerCapitalShipGained = 500;
        [SerializeField, Min(0)] int _pointsPerClue = 200;
        [SerializeField, Min(0)] int _victoryBonus = 5000;

        public RunScoreRules()
        {
        }

        public RunScoreRules(int pointsPerRescuedPerson, int pointsPerSurvivor, int pointsPerSmallShipGained, int pointsPerCapitalShipGained,
            int pointsPerClue, int victoryBonus)
        {
            if (pointsPerRescuedPerson < 0 || pointsPerSurvivor < 0 || pointsPerSmallShipGained < 0 || pointsPerCapitalShipGained < 0 ||
                pointsPerClue < 0 || victoryBonus < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pointsPerRescuedPerson), "Only achievements score; points cannot be negative.");
            }
            _pointsPerRescuedPerson = pointsPerRescuedPerson;
            _pointsPerSurvivor = pointsPerSurvivor;
            _pointsPerSmallShipGained = pointsPerSmallShipGained;
            _pointsPerCapitalShipGained = pointsPerCapitalShipGained;
            _pointsPerClue = pointsPerClue;
            _victoryBonus = victoryBonus;
        }

        public int PointsPerRescuedPerson => _pointsPerRescuedPerson;
        public int PointsPerSurvivor => _pointsPerSurvivor;
        public int PointsPerSmallShipGained => _pointsPerSmallShipGained;
        public int PointsPerCapitalShipGained => _pointsPerCapitalShipGained;
        public int PointsPerClue => _pointsPerClue;
        public int VictoryBonus => _victoryBonus;
    }
}
