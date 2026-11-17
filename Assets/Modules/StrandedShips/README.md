# Stranded ships

Working notes for stranded capital ships: the decisions behind them, how they are built and what is
still open. Add to the notes when stranded ships change or are tested.

GDD references: *Neue Kapitalschiffe*, *Stationen, verlassene Schiffe und Wracks*, *Späte Signale*.

## Decisions

Agreed with the designer on 2026-11-17. Change them only on purpose.

| Topic | Decision |
| --- | --- |
| What it is | A **mix of station and capital ship**: until it is repaired it is a station of type `StrandedShip` (scan, hail, dock, search, evacuate). Once whole, the fleet's own ship of its class takes its place. |
| Classes | The fleet's four capital classes, weighted: **Liner 40 %, Refinery 25 %, Tanker 25 %, Cruiser 10 %**. |
| Frequency | Its own point of interest, **20 % per system, at most one**, in orbit (60 %) or in deep space; visible from the start. Stations come 1–4 per system. Later, late signals will reveal some of them during the cycle. |
| Damage | Found with **20–50 % hull**, never intact. It must be repaired **to a whole hull** before it joins. |
| Repair | **The player orders it** in the hulk's window ("Repair hulk"), as with repairs nothing runs on its own. It runs **while a ship with a crew is docked** and pauses without one or without metal; the progress stays. Pace and price of a capital ship's shipyard: **10 hull/s, 0.5 metal per hull point**. A liner at 30 % needs 175 s and 875 metal. |
| Joining | The new ship appears where the hulk lay, **with a whole hull, dry tanks, empty stores and an empty hangar**. Docked ships cast off. The tanker refuels it through the usual order; a ship without fuel stays behind at a fleet jump. |
| Survivors | About **60 % carry 20–400 survivors**, **never hostile** (friendly or wary). **All of them want to leave.** Shuttles can evacuate them before; whoever is still aboard becomes the new ship's passengers. Toxic air or radiation work as on stations. |
| Search | Like a station: only when nobody lives aboard and the air and radiation allow it. 3–6 finds, mostly resources and fuel. |
| Wrecks | 2–4 pieces of hull drift nearby as metal wrecks (60–140 metal each). |
| Pronin | (2026-11-17) The Pronin **attack stranded ships like the fleet**: they are a side of their own (`Allegiance.Stranded`), hostile to the Pronin and neutral to the fleet. Hits lower the hull and so undo repairs. |
| Destroyed | At 0 hull the hulk **explodes like a capital ship** and leaves 6 metal wrecks; the survivors aboard die, docked ships cast off. |
| Factions | Chosen over a general combat-target interface because quest factions will need the same: **who fights whom** is one table (`FactionRelations`), every side has its own registry. |

## How it is built

| Piece | Where | Role |
| --- | --- | --- |
| `PointOfInterestKind.StrandedShip` | StarSystem | The point of interest. `StarSystemGenerator` rolls it from its own seed stream, so systems without one keep all other rolls. The system map draws a rust-red sliver. |
| `StationType.StrandedShip`, `StrandedShipClass`, `StrandedShipClasses` | Zones/Model | The type, the classes, their odds, footprints and lengths. `StationTypes` throws for this type: its size depends on the class, so ask the `ZoneStation`. |
| `StrandedHull` | Zones/Model | Run state of the hull: share left, repair ordered, recovered. |
| `ZoneStation` | Zones/Model | Second constructor with a `StrandedHull`; `IsStrandedShip`, `IsRecovered` and `IsGone` (destroyed or recovered: no docking, search, picking or obstacle). Survivors aboard are all willing. |
| `StationGeneration.RollStrandedHull`, `RollProfile(…, StrandedShip)` | Zones/Generation | Class, hull share and the hidden profile. `ZoneContentGenerator` places the hulk and its wrecks. |
| `StrandedShipPrefabBuilder` | Stations/Editor | Builds `Stranded<Class>.prefab` from the fleet's ship prefab: hull parts only, dark windows, breaches, a red beacon, two docking ports midship. `StationPrefabBuilder.LoadOrCreateCatalog` adds them to the catalog. |
| `StationView.StrandedClass`, `StationPrefabCatalog.TryGetPrefab(ZoneStation)` | Stations | The catalog picks the model by type and, for hulks, by class. |
| `StrandedRepairPlan` | StrandedShips | Plain C#: hull points, seconds and metal left; one repair step through `RepairWork` (Repairs). |
| `StrandedShipRecovery` | StrandedShips | Scene component next to the ship registry: orders, the repair while a crew is docked (`StationDocking.IsDockedWithCrew`), the metal notice, and the swap to a real ship. Holds the four capital ship prefabs. |
| `StrandedHulkBodies` | StrandedShips | Gives every hulk in a shown zone a stand-in ship of the stranded side (`StrandedHulk` prefab, built by `CombatShipPrefabBuilder`): no model, never moves, hull sized per class. Its hits lower the `StrandedHull`; repairs raise its hull. Shot down, the station is destroyed, survivors die and its wreckage and explosion come from the stand-in. Removed on recovery or with the zones. |
| `ShipAgent.StandInFor` | Ships | The stand-in counts as its station for line of fire, so the station's own footprint does not hide it. |
| `StationInspection` | Inspection | Shows class and length, hull share (after a scan or a docked crew), cost and state, and the **Repair hulk** / **Stop repairs** action. |
| Texts | String table `StrandedShips`; `station-type.StrandedShip` in `Zones`, `point-of-interest.StrandedShip` in `StarSystem`. |

## Verified

- EditMode tests: generation odds and limits, hull rules, repair plan, prefab footprints and ports, string tables.
- Play mode (2026-11-17): a liner hulk with 120 survivors in the arrival zone; a shuttle docked, the repair waited for
  metal with one notice, then finished; "Liner 2" appeared in its place with 0 fuel, a whole hull and 120 passengers,
  and the shuttle cast off.

- Play mode (2026-11-17): three Pronin fighters next to a liner hulk at 6 % hull destroyed it within seconds; 6 wrecks,
  50 survivors dead, one notice; the fighters then went for the fleet.

## Open

- **Late signals** (next step): some stranded ships, wrecks or stations appear only during the cycle.
- The Pronin carrier still only hunts the zone with most of the fleet's capital ships; only fighters go for hulks.
- Named stranded ships (e.g. "ISV Meridian") instead of "<System> Stranded Ship".
- Subsystems: once they exist, a stranded ship could come with broken drives instead of only hull damage.
