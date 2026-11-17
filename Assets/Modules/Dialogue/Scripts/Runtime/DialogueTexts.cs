using LastFleet.Shared.Text;

namespace LastFleet.Dialogue
{
    /// <summary>Dialogue window texts in the string table "Dialogue".</summary>
    public static class DialogueTexts
    {
        const string Table = "Dialogue";

        public static readonly GameTextKey ContinueButton = new GameTextKey(Table, "button.continue");
        public static readonly GameTextKey EndButton = new GameTextKey(Table, "button.end");

        /// <summary>{seconds}, {choice} taken when the time runs out</summary>
        public static readonly GameTextKey TimeLeftLine = new GameTextKey(Table, "line.time-left");

        /// <summary>{count}</summary>
        public static readonly GameTextKey WaitingLine = new GameTextKey(Table, "line.waiting");
    }
}
