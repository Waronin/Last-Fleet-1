# Fleet modules

Working notes for fleet modules: the decisions behind them, how they are built and what is still open.
Add to the notes when fleet modules change or are tested.

Fleet modules **replace the GDD's upgrade system** (*Upgrades*: one upgrade module + metal + engineer time per
capital ship). Found upgrade modules are no longer fitted to single ships; each becomes one fleet-wide module.

## Decisions

Agreed with the designer on 2026-11-17. Change them only on purpose.

| Topic | Decision |
| --- | --- |
| Slots | **3–5 fleet-wide slots, to be tested.** Set on `FleetModuleService` in the Inspector (`_slotCount`, default **4**). |
| Getting one | Finds and request rewards still give an **upgrade module** (`ItemKind.UpgradeModule`). Each one offers a **choice of 3** different modules that are not installed yet; the player picks one. |
| When | The choice **waits**: a notice says a module is ready, the top-middle button shows the count, **U** opens the window. The game keeps running. The offer is rolled once and kept until answered. |
| Installing | **Instant and free.** No metal, no engineer time. |
| Full slots | The new module **replaces** an installed one, which is **destroyed**. No storage, no swapping back. The player may also **discard** a waiting module. |
| Stacking | A module can be installed only once; shares of the same stat from different modules add up (cargo bays −10 % and plating +30 % hull = +20 %). |

### Modules

| Module | Effect | Drawback |
| --- | --- | --- |
| Gas extractors / Ore drills / Ice cutters | +20 % mining speed for that raw material | – |
| Extended cargo bays | +30 % harvester cargo | −10 % hull, all ships |
| Reinforced plating | +30 % hull, all ships | −10 % repair speed |
| Radiation shielding | No crew deaths from radiation while **mining or docked**. The Meltdown quest dose still applies. | – |
| Envoy protocols | Hostile station crews **count as wary while docked**: no attack, no capture. Hail answers and supply requests are unchanged. | – |
| Capital ship drives | Capital ships 50 % faster | +10 % fuel use (travel and zone jumps) |
| Small craft thrusters | Small ships 40 % faster | +8 % fuel use (travel and zone jumps) |
| Signal amplifier | Late signals come in **twice as early** (at half their countdown share) | – |

Speed is "time-scaled" flight: top speeds and turn rate × factor, accelerations × factor², so every
manoeuvre keeps its path and takes 1/factor of the time. Interstellar fleet jumps are not affected.
Hull changes keep a ship's intact share (50 % stays 50 %).

## How it is built

| Piece | Where | Role |
| --- | --- | --- |
| `FleetEffects`, `FleetStat`, `FleetAbility` | Ships | The shared contract: per-side stat shares and abilities with a `Changed` event. Each `ShipRegistry` owns one; ships read it as `ShipAgent.Effects` and raise `EffectsChanged`, also when they change sides (a stranded ship that joins gets the fleet's modules). Pronin and stranded registries stay neutral. |
| Consumers | Ships, Mining, Repairs, Stations, LateSignals | `ShipAgent` (speed, fuel), `ShipDurability` (hull), `HarvesterMining` (mining rate, cargo), `MiningCoordinator` (radiation), `DockingHazardRules.IsWardedOff` (docking radiation, hostile crews), `RepairService` (repair rate), `LateSignalReceiver` (signal timing). |
| `FleetModuleKind`, `FleetModuleCatalog` | FleetModules | The modules and their values; `ApplyTo` writes the installed modules into the fleet's `FleetEffects`. |
| `FleetModuleSlots` | FleetModules | Plain slot state: install, replace. |
| `FleetModuleOffer` | FleetModules | Rolls the 3 offered modules. |
| `FleetModuleService` | FleetModules | Takes waiting modules from `FleetInventory`, keeps the offer, installs, applies effects, sends notices. |
| `FleetModuleWindow` | FleetModules | Prototype IMGUI: top-middle button and the window (U / Esc). |

## Open

- Slot count 3, 4 or 5 still needs playtesting.
- Modules are kept across a new galaxy seed, like the fleet inventory; there is no run reset yet.
- Values are balancing starting points.
