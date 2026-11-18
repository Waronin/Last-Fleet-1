using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;

namespace LastFleet.RunEnd
{
    /// <summary>
    /// Turns a finished run into the end screen's account (GDD: Wertung): achievements with points first, the
    /// victory bonus for a won run, then the losses without points.
    /// </summary>
    public static class RunScore
    {
        public static List<RunEndLine> BuildLines(RunOutcome outcome, int survivors, RunTally tally, RunScoreRules rules)
        {
            if (tally == null)
            {
                throw new ArgumentNullException(nameof(tally));
            }
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }
            if (survivors < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(survivors), "Survivors cannot be negative.");
            }

            List<RunEndLine> lines = new List<RunEndLine>
            {
                Scoring(RunMetric.PeopleRescued, RunEndTexts.RescuedLine, tally.PeopleRescued, rules.PointsPerRescuedPerson),
                Scoring(RunMetric.Survivors, RunEndTexts.SurvivorsLine, survivors, rules.PointsPerSurvivor),
                Scoring(RunMetric.SmallShipsGained, RunEndTexts.SmallShipsGainedLine, tally.SmallShipsGained, rules.PointsPerSmallShipGained),
                Scoring(RunMetric.CapitalShipsGained, RunEndTexts.CapitalShipsGainedLine, tally.CapitalShipsGained, rules.PointsPerCapitalShipGained),
                Scoring(RunMetric.CluesFound, RunEndTexts.CluesLine, tally.CluesFound, rules.PointsPerClue)
            };
            if (outcome == RunOutcome.Won)
            {
                lines.Add(RunEndLine.Scoring(RunMetric.Victory, RunEndTexts.VictoryLine.Format(), 1, rules.VictoryBonus));
            }
            lines.Add(RunEndLine.Fact(RunMetric.PeopleLost, RunEndTexts.PeopleLostLine.Format(("count", tally.PeopleLost)), tally.PeopleLost));
            lines.Add(RunEndLine.Fact(RunMetric.ShipsLost, RunEndTexts.ShipsLostLine.Format(("count", tally.ShipsLost)), tally.ShipsLost));
            return lines;
        }

        public static int Total(IReadOnlyList<RunEndLine> lines)
        {
            int total = 0;
            foreach (RunEndLine line in lines)
            {
                total += line.Points;
            }
            return total;
        }

        static RunEndLine Scoring(RunMetric metric, GameTextKey label, int count, int pointsEach)
        {
            return RunEndLine.Scoring(metric, label.Format(("count", count)), count, count * pointsEach);
        }
    }
}
