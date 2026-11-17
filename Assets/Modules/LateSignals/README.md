# Late signals

Working notes for late signals: the decisions behind them, how they are built and what is still open.
Add to the notes when late signals change or are tested.

GDD references: *Gameloop – Späte Signale*, *Sonnensystem – Späte Signale*, *Anreize zu bleiben*.

## Decisions

Agreed with the designer on 2026-11-17. Change them only on purpose.

| Topic | Decision |
| --- | --- |
| What comes in | A **distress call from a stranded ship** or a **station breaking its radio silence**, half and half. A wreck field as a place of its own may follow later. |
| How often | **0–2 per system** (none 20 %, one 55 %, two 25 %), on top of the places shown on arrival. |
| When | Each at a random point **between 40 % and 90 % of the countdown** to the Pronin's arrival (about 5–12 minutes at 13 minutes), some shortly before the attack. They follow time compression. |
| Worth | Better than what is known on arrival. Stranded ship: **40–75 % hull** instead of 20–50 %, **always survivors**, never ruined. Station: **never hostile, never ruined**, and **half again as many finds** when searched. Searching needs an empty station, so the extra finds count only on abandoned ones; on inhabited ones the value is the friendlier people. |
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
| `LateSignalSchedule` | LateSignals | Plain rule: which signals are due at a share of the countdown. |
| `LateSignalReceiver` | LateSignals | Follows the `PursuitClock`, receives due signals, sends a notice and an event with the zone index. |
| Controller | GenerationPreview | Hides unknown places on showing a system, reveals them on `SignalReceived`. |

Quest stations never go to a late place, since it may not be known yet.

## Open

- A wreck field as its own place (GDD: "ein Wrack, das in Sensorreichweite treibt").
- An event popup for the distress call ("Shuttle schicken oder ignorieren", GDD: events) once events exist.
- Balancing of counts, timing and worth after playtests.
