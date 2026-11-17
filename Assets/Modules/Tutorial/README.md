# Tutorial (prologue)

Working notes for the optional prologue: the decisions behind it, how it is built and what is open.
Add to the playtest log and the notes when the prologue changes or is tested.

GDD reference: *Tutorial*.

## Decisions

Agreed with the designer on 2026-11-17. Change them only on purpose.

| Topic | Decision |
| --- | --- |
| Optional | At the start of **every run** a radio call from the liner's captain offers "Play the prologue" / "Skip the prologue". There is no save game yet, so "mandatory on the first run" (GDD) waits for the meta progression. |
| Place | The prologue plays in the **start system**. While it runs: no pursuit countdown, no late signals, no quests, the jump drive is held. |
| Meltdown | Played: Meltdown step 1 is placed in the **system of the first jump**. Skipped or ended early: in the start system, as before. |
| Progress | Every step is a radio call plus an **objective the game detects**. The objective box has "Skip step" (for when the game cannot see it done) and "End prologue". |
| Scope | Two phases. **1 Exploration and mining** without the cruiser: explore a place with a station, dock, search or hail it, send harvesters to mine, deliver the first load. **2 Military**: Pronin fighters jump in at the jump point, the cruiser follows shortly after; the first fight is easy. The GDD learning goals that have no mechanic yet (defense plan, fire mode, announcements) are left out. |
| Military arrival | **Timed**, 20 s after the raid, also when every small ship is already home (added in the first test: an instant arrival took the raid's tension away). |
| End | After the fight the drive is released and its cooldown finished; the **first fleet jump** ends the prologue and starts the normal game loop in the target system. |

## Steps

| # | Speaker | Objective | Done when |
| --- | --- | --- | --- |
| 1 | Captain Ellis (liner) | Explore a place with a station | a revealed zone of the system holds a station (not a stranded hulk, not destroyed) |
| 2 | Ellis | Dock a ship at the station | such a station was visited (docked) |
| 3 | Ellis | Search the station or hail it | a search made a find, or the station was hailed (inhabited stations cannot be searched) |
| 4 | Ellis | Send harvesters to mine | a harvester's mining loop is active |
| 5 | Ellis | Deliver the first load | fuel, metal or water rose above the amount at the step's start |
| 6 | Ellis | Recall the small ships | 20 s pass; **3 Pronin fighters** jump in at the jump point zone when the step begins |
| 7 | Admiral Voss (cruiser) | Launch the cruiser's fighters | a fighter of the fleet is out of its hangar; the cruiser with two squads of 4 fighters jumps in when the step begins |
| 8 | Voss | Destroy the Pronin fighters | none of the raid's fighters is left |
| 9 | Voss | Jump to another system | the fleet jumped; the drive is released when the step begins |

After the jump Ellis says the clock is running now.

## How it is built

| Piece | Where | Role |
| --- | --- | --- |
| `TutorialStep`, `TutorialSequence` | Tutorial/Model | Plain C#: steps with speaker, line, objective, condition, optional time limit and begin action. The sequence ends at most one step per tick, so every radio call is shown. Steps without a condition wait for `Advance`. |
| `PrologueConditions` | Tutorial | The objectives' checks against the fleet registry, the zone world, search and mining. |
| `PrologueDirector` | Tutorial | Offers the prologue, builds and runs the sequence, opens the radio calls (`DialogueService`), spawns the raid. Raises `Skipped`, `MilitaryRequested`, `JumpCleared` and `Notice`; `CompleteOnFleetJump()` ends it. |
| `PrologueObjectivePanel` | Tutorial | IMGUI box at the top middle below the jump status: step, objective, "Skip step", "End prologue". |
| `PrologueTexts` | Tutorial | String table "Tutorial" (`Tutorial/Data/Localization`), English and German. |

Hooks in other modules, all generic:

- `FleetJump`: `FleetJumpService.SetHold(reason)` keeps the drive from charging and names the reason; `FinishCooldown()`.
- `Quests`: `QuestDirector.StartChains(chains, systemIndex)` starts chains later in the run; `BeginRun` uses it.
- `GenerationPreview`: the controller (run owner) splits the starting fleet into civilian ships and the military
  (`BringInMilitary`), holds countdown, late signals, quests and the jump while the prologue is offered or played,
  and starts them on `Skipped` (in the start system) or on the prologue's jump (in the target system). The scene
  builder adds a *Prologue* object. Without a `PrologueDirector` reference every run starts as before.
- `Dialogue`, `Inspection`: their lazily built `GUIStyle` fields are now `[NonSerialized]`; after a reload the radio
  calls and the window titles were invisible (same issue as `RunEndWindow`).

## Playtest log

Newest first.

| Date | Setup | Result | Notes |
| --- | --- | --- | --- |
| 2026-11-17 | Seeds 1–3, driven through the Unity CLI | Run start: civilian fleet only, no countdown, offer open, jump held. Revealing a station zone ended step 1. Skipping to step 6 spawned 3 Pronin fighters; the cruiser and 8 fighters arrived about 20 s later. Launching fighters and destroying the raid led to step 9, the drive was free; the jump ended the prologue, the countdown started and Meltdown was placed in the target system. Skip at the offer: whole fleet, countdown, Meltdown in the start system. "End prologue" during step 7: no second cruiser, game loop in the start system | First version let the cruiser arrive at once when every small ship was home; changed to a fixed delay. Steps 2–5 and the fight were not played by hand. |

## Known issues and open points

- Not played by hand yet: the texts, the 20 s delay and the raid's strength need a real playtest.
- Radio calls queue: a player who does not close a call sees the next one only afterwards. A call still waiting
  stays after "End prologue".
- The raid fighters hunt the nearest small ships anywhere; harvesters left out mining can be lost (intended: losses
  are permanent, GDD).
- If no station can be searched or hailed in the start system, step 3 needs "Skip step".
- The military simply appears at the jump point; there is no jump-in effect yet.
- "Mandatory on the first run" needs a saved flag once there is meta progression.

## Notes

Free space for ideas, observations and decisions from later sessions.

-
