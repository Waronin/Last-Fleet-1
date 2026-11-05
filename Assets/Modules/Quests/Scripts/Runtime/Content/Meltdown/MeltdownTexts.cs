using LastFleet.Shared.Text;

namespace LastFleet.Quests.Meltdown
{
    /// <summary>Texts of the Meltdown quest chain in the string table "Meltdown".</summary>
    public static class MeltdownTexts
    {
        const string Table = "Meltdown";

        public static readonly GameTextKey ChainTitle = new GameTextKey(Table, "chain.title");

        public static readonly GameTextKey PlatformTitle = new GameTextKey(Table, "step.platform.title");
        public static readonly GameTextKey PlatformObjective = new GameTextKey(Table, "step.platform.objective");
        public static readonly GameTextKey ScientistTitle = new GameTextKey(Table, "step.scientist.title");
        public static readonly GameTextKey ScientistObjective = new GameTextKey(Table, "step.scientist.objective");
        public static readonly GameTextKey SacredRockTitle = new GameTextKey(Table, "step.sacred-rock.title");
        public static readonly GameTextKey SacredRockObjective = new GameTextKey(Table, "step.sacred-rock.objective");
        public static readonly GameTextKey RefiningTitle = new GameTextKey(Table, "step.refining.title");
        public static readonly GameTextKey RefiningObjective = new GameTextKey(Table, "step.refining.objective");
        public static readonly GameTextKey IgnitionTitle = new GameTextKey(Table, "step.ignition.title");
        public static readonly GameTextKey IgnitionObjective = new GameTextKey(Table, "step.ignition.objective");

        /// <summary>{station}</summary>
        public static readonly GameTextKey SearchPartySpeaker = new GameTextKey(Table, "speaker.search-party");

        /// <summary>{seconds}, {manufacturer}</summary>
        public static readonly GameTextKey PlatformAlarm = new GameTextKey(Table, "platform.alarm");

        /// <summary>{station}, {scientist}</summary>
        public static readonly GameTextKey ScientistOffer = new GameTextKey(Table, "scientist.offer");

        public static readonly GameTextKey ScientistAccept = new GameTextKey(Table, "scientist.accept");
        public static readonly GameTextKey ScientistDecline = new GameTextKey(Table, "scientist.decline");
        public static readonly GameTextKey ScientistLaunching = new GameTextKey(Table, "scientist.launching");
        public static readonly GameTextKey ScientistWaiting = new GameTextKey(Table, "scientist.waiting");
        public static readonly GameTextKey ScientistArrived = new GameTextKey(Table, "scientist.arrived");
    }
}
