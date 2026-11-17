using System;
using System.Collections.Generic;

namespace LastFleet.FleetModules
{
    /// <summary>
    /// The fleet's module slots (designer: 3-5, to be tested). Every module works for the whole fleet and
    /// sits in at most one slot. When all slots are taken, a new module replaces an installed one, which
    /// is lost for good.
    /// </summary>
    public sealed class FleetModuleSlots
    {
        readonly List<FleetModuleKind> _installed = new List<FleetModuleKind>();

        public FleetModuleSlots(int slotCount)
        {
            if (slotCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount), "The fleet needs at least one module slot.");
            }
            SlotCount = slotCount;
        }

        public int SlotCount { get; }

        /// <summary>Installed modules in slot order; slots past the end are free.</summary>
        public IReadOnlyList<FleetModuleKind> Installed => _installed;

        public bool HasFreeSlot => _installed.Count < SlotCount;

        /// <summary>Raised after a module was installed or replaced.</summary>
        public event Action Changed;

        public bool IsInstalled(FleetModuleKind kind)
        {
            return _installed.Contains(kind);
        }

        /// <summary>Puts the module into the next free slot.</summary>
        public void Install(FleetModuleKind kind)
        {
            if (!HasFreeSlot)
            {
                throw new InvalidOperationException("All module slots are taken; replace a module instead.");
            }
            RequireNotInstalled(kind);
            _installed.Add(kind);
            Changed?.Invoke();
        }

        /// <summary>Puts the module into an occupied slot and returns the module it destroys.</summary>
        public FleetModuleKind Replace(int slotIndex, FleetModuleKind kind)
        {
            if (slotIndex < 0 || slotIndex >= _installed.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Only an occupied slot can be replaced.");
            }
            RequireNotInstalled(kind);
            FleetModuleKind removed = _installed[slotIndex];
            _installed[slotIndex] = kind;
            Changed?.Invoke();
            return removed;
        }

        void RequireNotInstalled(FleetModuleKind kind)
        {
            if (_installed.Contains(kind))
            {
                throw new InvalidOperationException($"{kind} is already installed.");
            }
        }
    }
}
