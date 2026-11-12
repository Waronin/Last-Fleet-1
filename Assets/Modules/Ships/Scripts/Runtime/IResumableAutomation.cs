using System;

namespace LastFleet.Ships
{
    /// <summary>
    /// An automation that can pick up its task again after an interruption, e.g. when the ship flew
    /// back to a hangar to refuel. Whoever interrupts suspends the automation first and starts the
    /// task over with the returned action later.
    /// </summary>
    public interface IResumableAutomation
    {
        /// <summary>
        /// Stops the current task so the interruption can take the ship over, and returns an action that
        /// starts the task over; null when the automation was not active. The action must cope with a
        /// world that changed meanwhile, e.g. a depleted deposit or a lost escort target.
        /// </summary>
        Action Suspend();
    }
}
