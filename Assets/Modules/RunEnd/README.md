# Run end

The end screen of a run, its account (GDD: *Spielende*, *Wertung*) and the defeat when the fleet's capital ships are gone.

## Decisions

Agreed with the designer on 2026-11-18. Change them only on purpose.

| Topic | Decision |
| --- | --- |
| Victory | A quest chain that wins the run is complete (unchanged); the screen waits until the last radio call is closed. |
| Defeat | A quest step ends the run (e.g. the supernova catches the fleet), or **the fleet's last capital ship is destroyed** (GDD). The defeat shows 2.5 game seconds after the loss, so the explosion can be seen. A lost run counts no survivors. |
| Account | Rescued people, survivors at the end, small and capital ships gained, clues found (completed quest steps), the victory bonus; then people and ships lost. |
| Points | **Only achievements score**; losses are listed without points. Proposal values, tunable on `RunEndFlow`: 1 per rescued person, 1 per survivor, 50 per small ship, 500 per capital ship, 200 per clue, 5000 for a victory. |
| Presentation | Golden title "Die Flotte ist entkommen" or red "Die Flotte ist verloren", a mood line and the reason. Lines appear one after another (0.35 s), then the total counts up (1.2 s); a click skips. The button "Neuer Lauf" appears at the end. The game is paused behind the screen; the build-up runs on real time. |
| Winning chain | A won run names the quest chain that won it ("Sieg durch die Questreihe „Meltdown“") instead of a reason. |
| Run history | Every finished run is saved at once (before the screen shows) as JSON in `Application.persistentDataPath/run-history.json`, **without limit**. JSON over XML: built into Unity (`JsonUtility`), compact and readable. |
| Comparison | Wins and losses compare together. Each line shows its change since the last run (▲/▼, green when better, red when worse; fewer losses are better) and "Rekord!" when it beats every earlier run; a tie is no record. The total shows the best earlier total or "Neuer Rekord!". The victory bonus is not compared. |
| Latest runs | Once the build-up is done, the 5 latest runs follow, this one first and highlighted: date, victory with its chain or defeat, points. |
| Broken history | A file that cannot be read as a history (bad JSON, bad record, newer version) is moved aside as `run-history.unreadable-<time>.json` and a new history begins. A file that exists but cannot be opened is left alone and this session saves nothing, so earlier runs are never overwritten. |

## How it is built

| Piece | Where | Role |
| --- | --- | --- |
| `RunTally` | RunEnd | Counts of the run. |
| `RunScoreRules`, `RunScore`, `RunEndLine` | RunEnd | Points per achievement and the account's lines. |
| `RunSummary` | RunEnd | Outcome, reason or winning chain, a frozen copy of the tally and the account. |
| `RunMetric`, `RunRecord`, `RunHistory` | RunEnd | What a line counts; a saved run; all saved runs (the JSON format, version 1). |
| `RunHistoryStore` | RunEnd | Reads and writes the history file; reports unreadable files. |
| `RunComparison`, `RunEndReport` | RunEnd | Changes since the last run, records and best total; everything the screen shows. |
| `RunEndReveal` | RunEnd | Timing of the build-up. |
| `RunEndWindow` | RunEnd | The screen (IMGUI). |
| `CapitalShipLossWatch` | RunEnd | Reports the defeat when the last capital ship falls. |
| `RunTallyRecorder` | GenerationPreview | Feeds the tally from the population, water, recoveries, crew transfers, quests and fleet losses. |
| `RunEndFlow` | GenerationPreview | Builds the summary, saves and compares it, shows the screen, pauses the game and starts the next run. |

Known limits: a ship that docks and sets down passengers it already carried at the start counts them as rescued.
