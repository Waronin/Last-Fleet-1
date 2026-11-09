# Repairs

Working notes for hull repairs: the decisions behind them, how they are built and what is still
open. Add to the notes when repairs change or are tested.

GDD references: *Subsysteme* (Reparatur), *Kapitalschiffe*, *Kleine Schiffe*, *Spezialisten* (Ingenieur).

## Decisions

Agreed with the designer on 2026-11-09. Change them only on purpose.

| Topic | Decision |
| --- | --- |
| Trigger | **Nothing is repaired automatically.** The player orders every repair in the ship window. |
| Scope | **Hull only.** Subsystems do not exist yet; when they come, they join the same shipyard queue. |
| Engineers | **One shipyard per capital ship.** Its engineers work on **one ship at a time**: the capital ship itself or a small ship in its hangar. The player sets the order. Engineers are abstract, not named specialists yet. |
| Small ships | Repaired **only while docked in a hangar** (GDD). The carrier's shipyard does the work. A ship that launches leaves the queue with a notice; landing again does not queue it again. |
| Under fire | Repairs **always run**, also during combat. |
| Cost and pace | **0.5 metal per hull point.** Small ships 2 hull points/s, capital ships 10 hull points/s. A fighter (60 hull) needs 30 s and 30 metal; a cruiser with 1,000 damage needs 100 s and 500 metal. |
| No metal | The current repair stops and waits; it goes on by itself once metal is back. One notice per shortage. |
| Losses | Repairs restore hull only. People who died from damage stay dead; a destroyed ship stays lost. |

## How it is built

- `ShipHull.Repair` (Ships module) restores points up to the maximum and never revives a destroyed hull.
- `RepairWork` (plain C#) plans one step: rate × time, no more than missing, no more than the metal pays for.
- `RepairQueue<TShip>` (plain C#) is the player's order: only the first entry is repaired.
- `Shipyard` is the queue of one capital ship plus whether it waits for metal.
- `RepairService` (scene component next to the ship registry) owns all shipyards, takes metal through
  `FleetSupplies`, drops finished, destroyed and launched ships, and raises notices. Its serialized
  fields hold the balancing values above.
- The ship window (`ShipRepairInspection` in the Inspection module) shows the repair state of a ship
  ("in progress", "waiting, place N", "waiting for metal") and of a capital ship's shipyard, and offers
  **Repair**, **Repair first** and **Cancel repair** for the selection. The last two show only while a
  selected ship is queued.
- Texts are in the string table `Repairs`.

## Open

- **Subsystems** and their failures (main drive, jump drive, hangar, weapons, life support, production).
- **Named engineers** (specialists) and whether their number or skill changes the pace.
- No hotkey for repairs yet; the ship window is the only way.
- Hangar UI that lists the docked ships with their hull, so damaged ones are easier to find.
- Stranded capital ships that must be repaired before they join (GDD: *Neue Kapitalschiffe*).
