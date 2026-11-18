using System;

namespace LastFleet.RunEnd
{
    /// <summary>What happened during a run, counted as it happens, for the end screen's account (GDD: Wertung).</summary>
    public sealed class RunTally
    {
        public int PeopleRescued { get; private set; }
        public int PeopleLost { get; private set; }
        public int SmallShipsGained { get; private set; }
        public int CapitalShipsGained { get; private set; }
        public int SmallShipsLost { get; private set; }
        public int CapitalShipsLost { get; private set; }
        public int CluesFound { get; private set; }

        public int ShipsLost => SmallShipsLost + CapitalShipsLost;

        public void AddRescued(int people)
        {
            PeopleRescued += RequireNotNegative(people);
        }

        public void AddPeopleLost(int people)
        {
            PeopleLost += RequireNotNegative(people);
        }

        public void AddShipGained(bool isCapital)
        {
            if (isCapital)
            {
                CapitalShipsGained++;
            }
            else
            {
                SmallShipsGained++;
            }
        }

        public void AddShipLost(bool isCapital)
        {
            if (isCapital)
            {
                CapitalShipsLost++;
            }
            else
            {
                SmallShipsLost++;
            }
        }

        public void AddClue()
        {
            CluesFound++;
        }

        public void Reset()
        {
            PeopleRescued = 0;
            PeopleLost = 0;
            SmallShipsGained = 0;
            CapitalShipsGained = 0;
            SmallShipsLost = 0;
            CapitalShipsLost = 0;
            CluesFound = 0;
        }

        /// <summary>A frozen copy for the end screen, so a new run can count again.</summary>
        public RunTally Copy()
        {
            return new RunTally
            {
                PeopleRescued = PeopleRescued,
                PeopleLost = PeopleLost,
                SmallShipsGained = SmallShipsGained,
                CapitalShipsGained = CapitalShipsGained,
                SmallShipsLost = SmallShipsLost,
                CapitalShipsLost = CapitalShipsLost,
                CluesFound = CluesFound
            };
        }

        static int RequireNotNegative(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Counts cannot be negative.");
            }
            return count;
        }
    }
}
