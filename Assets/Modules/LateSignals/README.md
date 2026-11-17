# Late signals

Working notes for late signals: the decisions behind them, how they are built and what is still open.
Add to the notes when late signals change or are tested.

GDD references: *Gameloop – Späte Signale*, *Sonnensystem – Späte Signale*, *Anreize zu bleiben*.

## Decisions

Agreed with the designer on 2026-11-17. Change them only on purpose.

| Topic | Decision |
| --- | --- |
| What comes in | A **distress call from a stranded ship** (40 %), a **station breaking its radio silence** (40 %) or a **wreck field drifting into sensor range** (20 %, added 2026-11-17). Wreck fields only come as late signals. |
| How often | **0–2 per system** (none 20 %, one 55 %, two 25 %), on top of the places shown on arrival. |
| When | Each at a random point **between 40 % and 90 % of the countdown** to the Pronin's arrival (about 5–12 minutes at 13 minutes), some shortly before the attack. They follow time compression. |
| Worth | Better than what is known on arrival. Stranded ship: **40–75 % hull** instead of 20–50 %, **always survivors**, never ruined. Station: **never hostile, never ruined**, and **half again as many finds** when searched. Searching needs an empty station, so the extra finds count only on abandoned ones; on inhabited ones the value is the friendlier people. |
| Wreck field | **8–14 drifting wrecks with 100–300 metal** each (about 1,500–3,000 in all), salvaged like other wrecks but **unscanned**: about **a third carries a hazard**, a leaking reactor (`Radioactive`) or fuel left in the tanks (`GasPockets`), using the mining hazard rules. In the middle lies a **derelict** (`StationType.Derelict`), a dead cruiser broken in two: never inhabited, never repaired, **searched like an abandoned station** for 4–8 finds (fuel, modules, archive records, resources). About a quarter leak poison or radiation and stay closed. |
| Expiry | **None.** A received signal stays until the fleet leaves; the only pressure is the countdown. |
| Revisits | A received signal stays known for the run. Signals not yet received wait for the next countdown in that system. |

## How it is built

| Piece | Where | Role |
| --- | --- | --- |
| `PointOfInterest.LateSignalFraction` | StarSystem | Share of the countdown after which the place shows up; 0 for places known on arrival. |
| `StarSystemGenerator.AddLateSignals` | StarSystem | Rolls the late places from their own seed stream, so systems keep every other roll. They sit in the layout from the start, before the jump point, and keep stable indices. |
| `StarSystemView.SetPointOfInterestHidden` | StarSystem | Hides body and marker of an unknown place; it cannot be picked. |
| `ZoneRecord.IsKnown` / `ReceiveSignal` | Zones | Run state: whether the signal has come in. |
| `StationGeneration` / `StationSearchRules` | Zones, Stations | The better rolls for late places. |
| `ZoneContentGenerator.AddWreckField`, `ResourceDeposit.DriftingWreck` | Zones | Derelict and hazardous wrecks of a wreck field. |
| `DerelictPrefabBuilder` | Stations (Editor) | Blockout derelict from the cruiser prefab, broken in two. |
| `LateSignalSchedule` | LateSignals | Plain rule: which signals are due at a share of the countdown. |
| `LateSignalReceiver` | LateSignals | Follows the `PursuitClock`, receives due signals, sends a notice and an event with the zone index. |
| Controller | GenerationPreview | Hides unknown places on showing a system, reveals them on `SignalReceived`. |

Quest stations never go to a late place, since it may not be known yet.

## Open

- An event popup for the distress call ("Shuttle schicken oder ignorieren", GDD: events) once events exist.
- Balancing of counts, timing and worth after playtests.
