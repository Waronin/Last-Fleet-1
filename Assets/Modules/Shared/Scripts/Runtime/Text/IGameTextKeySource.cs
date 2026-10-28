using System.Collections.Generic;

namespace LastFleet.Shared.Text
{
    /// <summary>Declares text keys, so a test can check that each one exists in every language.</summary>
    public interface IGameTextKeySource
    {
        IEnumerable<GameTextKey> Keys { get; }
    }
}
