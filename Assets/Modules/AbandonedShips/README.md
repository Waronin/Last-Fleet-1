# Abandoned ships and crew transfer

Working notes for small ships without crew: found ones adrift and the fleet's own whose last crew member died.
Add to the notes when this changes or is tested.

GDD references: *Kleine Schiffe* (only found, never built), *Stationen, verlassene Schiffe und Wracks*, *Menschen und Crew*.

## Decisions

Agreed with the designer on 2026-11-18. Change them only on purpose.

| Topic | Decision |
| --- | --- |
| One mechanic | An own small ship without crew and an abandoned one found adrift are the **same problem**: a small ship nobody can fly. Both are saved the same way. |
| Crew transfer | Right-click such a ship with selected small ships: the nearest one with **at least two aboard** flies alongside (follow order) and **one person climbs over** within 6 units. The donor stops there, one person short; the saved ship flies with one person (slower) until its hangar fills the posts (`CrewRecruitment`). Another order to the donor cancels the transfer. |
| Own ships | Losing the last crew member still makes a ship a derelict that drifts and takes no orders; the player now gets a notice, and a transfer brings it back with its squad and home hangar. Left outside a hangar at a fleet jump, it is lost as before. |
| Where found | Adrift in revealed zones: **wreck fields 60 % (1–2 ships)**, **stranded hulks 25 %** and **stations 25 %** (1 ship). Rolled after the rest of the zone, so nothing else changes. |
| Condition | **Shuttle 40 %, harvester 35 %, fighter 25 %**; **30–80 % hull**, **5–20 % fuel**, no crew. |
| Joining | A found ship joins as a **squad of its own**, homed on a carrier with room (home hangar search as for landing); the player regroups it as usual. Without room it flies without a home. |
| Pronin | Until recovered, found ships belong to the **stranded side** and are hunted like stranded hulks; destroyed, they leave their wreckage and are gone. |

## How it is built

| Piece | Where | Role |
| --- | --- | --- |
| `AbandonedShip`, `AbandonedShipClass` | Zones | Run state of a found ship: class, spot, hull, fuel, recovered or destroyed. |
| `ZoneContentGenerator.AddAbandonedShips` | Zones | Places them in wreck fields, hulk and station zones. |
| `AbandonedShipBodies` | AbandonedShips | Spawns them as real ships of the stranded side in shown zones, without crew; writes hits back; forgets them when the zone goes. |
| `CrewTransferRules` | AbandonedShips | Plain rules: donors keep one, transfer range. |
| `CrewTransferService` | AbandonedShips | Picks receivers and donors, runs the transfer, hands found ships to the fleet (`ShipRegistry.TransferTo`, `FormSquad`), announces own ships losing their crew. |
| `ShipCommandInput` | ShipControl | Right-click on a small ship without crew orders the transfer. |

## Open

- Showing found ships in the inspection panel (class, hull, fuel) before they are recovered.
- Pilots as named specialists (GDD: *Spezialisten*): the transfer moves a count today, not a person.
