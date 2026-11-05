# Quests and dialogues

Working notes for quest chains and station dialogues: how they are built, how to add a new chain,
the decisions behind them and the state of the Meltdown chain. Add to the playtest log and the
notes when quests change or are tested.

GDD references: *Quests*, *Stationen*, *Der Sprung*.

## Decisions

Agreed with the designer on 2026-11-05. Change them only on purpose.

| Topic | Decision |
| --- | --- |
| Authoring | Quest chains and dialogues are **C# definitions** (like the balancing values elsewhere), not ScriptableObjects. Texts live in string tables. |
| Chain shape | A chain is a list of steps; **every step takes place in its own star system**. |
| Start | Opening chains begin at the start of a run, with step 1 **in the start system**. |
| Placement | The next step's system is chosen **when the previous step is done**: an unvisited system within one jump, else the nearest unvisited one, else any system in range (`QuestSystemPicker`, seeded). |
| Visibility | The galaxy map rings systems with a waiting step. The jump warning and the galaxy overlay name it ("Quest: Meltdown – step 2/5"). In the step's system, the system map rings the zone of the quest station (magenta) and the quest log names it ("Zone: …"), so the player knows where to go before the zone is explored. The quest log also shows step, objective, system and live status. |
| Failure | **Each step decides** (`QuestFailure.EndsChain` or `RetryStep`). |
| Dialogue time | Dialogues are radio calls: **the game keeps running**. The window only blocks clicks on itself. |
| Dialogue shape | **Branching**: nodes with speaker, line and answers. An answer can carry a consequence (e.g. accept a quest). |

## How it is built

| Piece | Where | Role |
| --- | --- | --- |
| `DialogueScript`, `DialogueNode`, `DialogueChoice` | Dialogue/Model | A conversation as a small graph. Every node reference is checked when the script is built. |
| `DialogueBuilder` | Dialogue/Model | Writes a script in reading order: `Say(id, speaker, line)`, then `Choice(label, next, consequence)` or `Then(next)`. `Message(speaker, line)` for a single radio call. |
| `DialogueConversation` | Dialogue/Model | A running conversation: `Continue`, `Choose`. A choice's consequence runs before the next node shows. |
| `DialogueService` | Dialogue | The shown conversation and a queue of waiting ones. |
| `DialogueWindow` | Dialogue | IMGUI window in the lower middle. Answers are buttons only, because the number keys select control groups. |
| `QuestChainDefinition`, `QuestStepDefinition` | Quests/Model | What the author writes: title, objective, failure rule, factory for the step logic (null = written down, not playable yet). |
| `QuestChain` | Quests/Model | Progress in a run: current step, its system, state, attempt. |
| `QuestSystemPicker`, `QuestTimer` | Quests/Model | Next-step placement; a countdown on game time. |
| `IQuestStep` + `QuestStepContext` | Quests | Contract of a step attempt: `Begin`, `OnSystemEntered`, `OnSystemLeft`, `Tick`, `End`, `Status`. It reports through `context.Complete()` / `context.Fail(reason)`; late reports are ignored. `context.MarkZone(index)` shows the player where to go. |
| `QuestServices`, `QuestPrefabs` | Quests | The scene systems and prefabs a step may use, handed over explicitly. |
| `QuestDirector` | Quests | Runs the chains: places steps, drives them, applies failure rules, raises `Notice` and `MarkersChanged`. The run owner calls `BeginRun` and `SystemEntered`. |
| `QuestStationSpec`, `QuestStationSite` | Quests/Sites | A quest station (proper name, type, hidden profile) and where it lies. The site picks a zone of the step's system (never the jump point or star; unexplored station zones first) and joins the zone's contents **when the zone is revealed** (`IZoneAmendment`), so it is ordinary run state. |
| `QuestStationContact` | Quests/Sites | A quest giver: a hail opens their dialogue instead of the plain answer (`IStationContact`). |
| `SearchTrapStep` | Quests/Steps | Reusable: searching a station sets off a fuse instead of the first find. When it runs out, every ship flying in the zone (both sides) takes the blast, the station is destroyed and leaves metal wrecks. |
| `EscortStep` | Quests/Steps | Reusable: hail, accept, a shuttle with passengers launches and flies to a fleet hangar by itself; Pronin fighters go for it after a delay. Done on landing, failed if the shuttle is lost. |
| `QuestLogPanel` | Quests/View | IMGUI quest log at the bottom middle. |
| `MeltdownQuest`, `MeltdownTexts` | Quests/Content/Meltdown | The Meltdown chain: settings, story dialogues, numbers. |

Hooks added to other modules for quests, all generic:

- `Zones`: `IStationContact` (`ZoneStation.SetContact`, `HasContactToTalkTo`), `ZoneStation.MarkDestroyed()`
  (no docking, search, picking, obstacle or model), `IZoneAmendment` + `ZoneRecord.Amend`,
  `ZoneContents.AddStation`, `ZoneContentGenerator.TryFindFreeSpot`, `ZoneView.ShowStation` / `RemoveStationView`.
- `Stations`: `IStationFindInterceptor` (`SearchService.AddInterceptor`): takes a find before it is handed out.
- `Ships`: `ShipRegistry.DamageShipsInZone`.
- `Galaxy`: `GalaxyMapView.SetHighlightedSystems`.
- `StarSystem`: `StarSystemView.SetHighlightedPointsOfInterest`.
- `Inspection`: a hail goes to the station's contact while it has something to say, and can be repeated.
- `Hud`: `FleetJumpDialog` names the quest step in the target system (optional reference).
- `GenerationPreview`: the controller starts Meltdown in `StartNewRun` and calls `SystemEntered` after
  `ZoneWorld.Show`. The scene builder adds a *Quests* object (dialogue service and window, director, log).

## Adding a quest chain

1. Make a folder `Quests/Scripts/Runtime/Content/<Chain>/` with a `<Chain>Texts` class (its own string
   table) and a static `<Chain>Quest.Create()` that returns the `QuestChainDefinition`.
2. For each step, reuse a step type (`SearchTrapStep`, `EscortStep`, …) with its settings, or write a
   new `IQuestStep` in `Steps/` when the mechanic is new. Keep the new step generic: stations, texts and
   numbers come in through a settings class, so later chains can use it too.
3. Write dialogues with `DialogueBuilder`; speakers that are proper names use `GameText.Verbatim`.
4. Add the string table entries in English and German (`StringTableCoverageTests` fails on missing
   keys or placeholders that differ between languages). Entries with `{placeholders}` must be Smart.
5. Start the chain: today the preview controller passes the opening chains to `QuestDirector.BeginRun`.
6. Add EditMode tests for new rules. A step's settings can be checked without a scene.

Rules for step logic:

- Subscribe in `Begin` / `OnSystemEntered` and release everything in `End`, which always comes last.
- Place the station once: `_site ??= QuestStationSite.Place(...)` in `OnSystemEntered`, then
  `context.MarkZone(_site.PointOfInterestIndex)` so the system map and quest log point there.
- Use `context.CreateRandom(salt)` for the step's own choices, so placement does not depend on frame timing.
- Zone views only exist while the fleet is in the system; check `ZoneWorld.TryGetView` before using one.

## Meltdown

| # | Step | State | Notes |
| --- | --- | --- | --- |
| 1 | The Ascalon platform (`SearchTrapStep`) | playable | Start system. Abandoned industrial station. The search sets off a 30 s fuse instead of the first find; the radio call names the maker (Halvorsen Kinetics) and points to the research station. The blast deals 150 hull damage to every ship flying in the zone (destroys small ships, dents capital ships); ships in hangars are safe. 6 wrecks of 80 metal. Cannot fail. |
| 2 | The scientist (`EscortStep`) | playable | Research station Meridian (friendly, 24 aboard). Hailing it reaches Dr. Ilka Morrow; accepting launches a shuttle with 4 people that flies to a fleet hangar; 3 Pronin fighters jump in 8 s later and go for it. Landing gains the shuttle; then the supernova story call. Losing the shuttle ends the chain. |
| 3 | The sacred rock | written | Mine a marked asteroid for a mineral item. Fighters of a nearby station defend it (they see it as sacred). |
| 4 | Refining | written | Bring the mineral to an industrial station, hold it 5 min, bring the refined mineral back. The station is radioactive: the shuttle has to undock now and then. |
| 5 | Ignition | written | Bring the refined mineral to a power station and hold it 10 min; then the fleet jumps. |

Open questions for steps 3–5 (ask before building them):

- **Hostile station fighters (3)**: a new side, or fighters in the Pronin registry with their own class?
  Today `CombatService` only knows the fleet and the Pronin.
- **Quest items (3–5)**: `Reward`/`ItemKind` only count items in the fleet inventory. The mineral has to be
  carried in a ship's hold (lost with the ship) and handed over at a station: probably a new reward kind
  "quest item" with an id.
- **"Hold the station" (4, 5)**: does the time only run while a ship is docked, and is it lost when
  everyone undocks? Is there an attack while holding?
- **Radiation (4)**: reuse `DockingHazardRules.Radiation` (crew deaths) or a separate dose that builds up?
- **Ending (5)**: the story after the fleet jump is not written yet ("Nach dem Sprung der Flotte …").
  Is the jump forced, and where does it go?

## Playtest log

Newest first.

| Date | Setup | Result | Notes |
| --- | --- | --- | --- |
| 2026-11-05 | Seed 1, driven through the Unity CLI: Ascalon zone revealed, a shuttle docked and searched; the shuttle stayed docked | The fuse started instead of the first find (0:27 shown), the alarm call opened; after 30 s the station vanished, 6 wrecks appeared, the docked shuttle was destroyed ("destroyed by a station explosion"); step 2 was placed in a system within one jump, shown in the jump warning | All notices in German as selected. |
| 2026-11-05 | Fleet jumped there, Meridian zone revealed, hailed, offer accepted at 4× time | The shuttle launched and landed in the liner within about 25 s of game time; the story call followed; step 3 was placed and shows "not playable yet". The 3 Pronin fighters then went for the liner | The station lay close to the arrival zone, so the escort was short. |

## Known issues and open points

- **Escort difficulty** depends on how far the station lies from the fleet. A nearby station makes the
  escort trivial. The attackers stay after the shuttle lands and hunt the fleet.
- Steps of a chain are only **saved for the run**. There is no save game yet.
- The quest log and dialogue window are blockout IMGUI with no sound or portraits. A long log can grow into
  the dialogue window.
- `StationHailResponse` still always tells the truth. Lying stations can now be built as an `IStationContact`.

## Notes

Free space for ideas, observations and decisions from later sessions.

-
