using System;
using System.Collections.Generic;
using LastFleet.Rewards;
using LastFleet.Shared.Randomness;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.FleetModules
{
    /// <summary>
    /// Turns found upgrade modules into fleet modules (designer, 2026-11-17). A found module waits in the
    /// fleet inventory until the player picks one of three offered modules; it is installed at once and
    /// for free and works for the whole fleet. With all slots taken, the pick replaces an installed module,
    /// which is lost. The offer for a waiting module is rolled once and kept until it is answered.
    /// </summary>
    public sealed class FleetModuleService : MonoBehaviour
    {
        [SerializeField] ShipRegistry _fleet;
        [SerializeField] RewardDelivery _rewards;
        [Tooltip("Module slots of the fleet; the designer wants to test 3 to 5.")]
        [SerializeField, Range(3, 5)] int _slotCount = 4;

        readonly List<FleetModuleKind> _offer = new List<FleetModuleKind>();
        FleetModuleSlots _slots;
        DeterministicRandom _random;
        int _reportedWaiting;

        public FleetModuleSlots Slots => _slots;

        /// <summary>Found upgrade modules that still wait for the player's choice.</summary>
        public int WaitingModules => _rewards.Inventory.CountOf(ItemKind.UpgradeModule);

        /// <summary>The choice for the next waiting module; empty when none waits.</summary>
        public IReadOnlyList<FleetModuleKind> Offer
        {
            get
            {
                if (_offer.Count == 0 && WaitingModules > 0)
                {
                    FleetModuleOffer.Roll(_random, _slots, _offer);
                }
                return _offer;
            }
        }

        public event Action<GameText> Notice;

        void Awake()
        {
            if (_fleet == null || _rewards == null)
            {
                Debug.LogError("FleetModuleService needs the fleet and the reward delivery.", this);
                enabled = false;
                return;
            }
            _slots = new FleetModuleSlots(_slotCount);
            _random = new DeterministicRandom((ulong)Environment.TickCount);
        }

        void OnEnable()
        {
            _rewards.Inventory.Changed += OnInventoryChanged;
            _slots.Changed += ApplyEffects;
            _reportedWaiting = WaitingModules;
            ApplyEffects();
        }

        void OnDisable()
        {
            _rewards.Inventory.Changed -= OnInventoryChanged;
            _slots.Changed -= ApplyEffects;
        }

        /// <summary>Installs an offered module into a free slot.</summary>
        public void Install(int offerIndex)
        {
            FleetModuleKind kind = TakeOffered(offerIndex);
            _slots.Install(kind);
            Notice?.Invoke(FleetModuleTexts.Installed.Format(("module", FleetModuleTexts.Names.For(kind))));
        }

        /// <summary>Installs an offered module in place of an installed one, which is destroyed.</summary>
        public void Replace(int offerIndex, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Installed.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Only an occupied slot can be replaced.");
            }
            FleetModuleKind kind = TakeOffered(offerIndex);
            FleetModuleKind removed = _slots.Replace(slotIndex, kind);
            Notice?.Invoke(FleetModuleTexts.Replaced.Format(("module", FleetModuleTexts.Names.For(kind)),
                ("removed", FleetModuleTexts.Names.For(removed))));
        }

        /// <summary>Throws the waiting module away without installing anything.</summary>
        public void Discard()
        {
            if (!_rewards.Inventory.TryTake(ItemKind.UpgradeModule))
            {
                throw new InvalidOperationException("No upgrade module is waiting.");
            }
            _offer.Clear();
            Notice?.Invoke(FleetModuleTexts.Discarded);
        }

        /// <summary>
        /// A new run starts with empty slots and no offer (roguelike: nothing carries over). The waiting modules live in
        /// the fleet inventory, which the run start empties as well.
        /// </summary>
        public void ResetForNewRun()
        {
            _offer.Clear();
            _slots.Clear();
            _reportedWaiting = WaitingModules;
        }

        FleetModuleKind TakeOffered(int offerIndex)
        {
            IReadOnlyList<FleetModuleKind> offer = Offer;
            if (offerIndex < 0 || offerIndex >= offer.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offerIndex), offerIndex, "No such module on offer.");
            }
            FleetModuleKind kind = offer[offerIndex];
            if (!_rewards.Inventory.TryTake(ItemKind.UpgradeModule))
            {
                throw new InvalidOperationException("No upgrade module is waiting.");
            }
            _offer.Clear();
            return kind;
        }

        void ApplyEffects()
        {
            FleetModuleCatalog.ApplyTo(_slots.Installed, _fleet.Effects);
        }

        void OnInventoryChanged(ItemKind item)
        {
            if (item != ItemKind.UpgradeModule)
            {
                return;
            }
            int waiting = WaitingModules;
            if (waiting > _reportedWaiting)
            {
                Notice?.Invoke(FleetModuleTexts.ModuleReady.Format(("count", waiting)));
            }
            _reportedWaiting = waiting;
        }
    }
}
