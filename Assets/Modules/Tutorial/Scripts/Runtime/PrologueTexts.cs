using LastFleet.Shared.Text;

namespace LastFleet.Tutorial
{
    /// <summary>Texts of the prologue in the string table "Tutorial".</summary>
    public static class PrologueTexts
    {
        const string Table = "Tutorial";

        public static readonly GameTextKey LinerCaptain = new GameTextKey(Table, "speaker.liner-captain");
        public static readonly GameTextKey Admiral = new GameTextKey(Table, "speaker.admiral");

        public static readonly GameTextKey OfferLine = new GameTextKey(Table, "offer.line");
        public static readonly GameTextKey OfferPlay = new GameTextKey(Table, "offer.play");
        public static readonly GameTextKey OfferSkip = new GameTextKey(Table, "offer.skip");
        public static readonly GameTextKey SkippedLine = new GameTextKey(Table, "skipped.line");

        public static readonly GameTextKey ExploreLine = new GameTextKey(Table, "step.explore.line");
        public static readonly GameTextKey ExploreObjective = new GameTextKey(Table, "step.explore.objective");
        public static readonly GameTextKey DockLine = new GameTextKey(Table, "step.dock.line");
        public static readonly GameTextKey DockObjective = new GameTextKey(Table, "step.dock.objective");
        public static readonly GameTextKey SearchLine = new GameTextKey(Table, "step.search.line");
        public static readonly GameTextKey SearchObjective = new GameTextKey(Table, "step.search.objective");
        public static readonly GameTextKey MineLine = new GameTextKey(Table, "step.mine.line");
        public static readonly GameTextKey MineObjective = new GameTextKey(Table, "step.mine.objective");
        public static readonly GameTextKey DeliverLine = new GameTextKey(Table, "step.deliver.line");
        public static readonly GameTextKey DeliverObjective = new GameTextKey(Table, "step.deliver.objective");
        public static readonly GameTextKey RaidLine = new GameTextKey(Table, "step.raid.line");
        public static readonly GameTextKey RaidObjective = new GameTextKey(Table, "step.raid.objective");
        public static readonly GameTextKey MilitaryLine = new GameTextKey(Table, "step.military.line");
        public static readonly GameTextKey MilitaryObjective = new GameTextKey(Table, "step.military.objective");
        public static readonly GameTextKey FightLine = new GameTextKey(Table, "step.fight.line");
        public static readonly GameTextKey FightObjective = new GameTextKey(Table, "step.fight.objective");
        public static readonly GameTextKey JumpLine = new GameTextKey(Table, "step.jump.line");
        public static readonly GameTextKey JumpObjective = new GameTextKey(Table, "step.jump.objective");
        public static readonly GameTextKey FarewellLine = new GameTextKey(Table, "farewell.line");

        /// <summary>{count}</summary>
        public static readonly GameTextKey RaidNotice = new GameTextKey(Table, "notice.raid");

        /// <summary>Why the drive does not charge before the prologue lets it.</summary>
        public static readonly GameTextKey JumpHeld = new GameTextKey(Table, "notice.jump-held");

        public static readonly GameTextKey PanelHeader = new GameTextKey(Table, "panel.header");

        /// <summary>{step}, {count}</summary>
        public static readonly GameTextKey PanelStep = new GameTextKey(Table, "panel.step");

        public static readonly GameTextKey NextStepButton = new GameTextKey(Table, "button.next-step");
        public static readonly GameTextKey EndPrologueButton = new GameTextKey(Table, "button.end-prologue");
    }
}
