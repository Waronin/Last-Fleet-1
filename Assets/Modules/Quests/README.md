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
| Quest items | A quest item (e.g. the mineral) rides **aboard one ship** (`QuestCargo`), through hangars and jumps, and is lost with it (destroyed, captured, left behind, or its crew dead). The quest log names the carrier. Not part of the fleet inventory. |
| Station guards | Fighters of a station that defends something are on the **Pronin side with their own class** (`StationGuard`, bronze). They do not hunt; the step keeps them near what they guard. |
| Holding a station | Work at a station (refining) only runs **while a fleet ship with a crew is docked**; it pauses and keeps its progress when none is. |
| Ending | Meltdown's last step sets off a **supernova**: after ignition, a countdown to jump out. Escaping **wins the run**; staying **loses it** (the only defeat so far). |
| Winning | A won or lost run ends with an **end screen** (people saved, ships, time) and only "New run" goes on. No general defeat rule (e.g. fleet destroyed) yet. |
| Station radiation | A quest station can take over radiation (`ZoneStation.UseScriptedRadiation`): a **dose builds up while docked and wears off away**; at the limit crew members die. The generic random radiation deaths are off for that station. |

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
| `IQuestStep` + `QuestStepContext` | Quests | Contract of a step attempt: `Begin`, `OnSystemEntered`, `OnSystemLeft`, `Tick`, `End`, `Status`. It reports through `context.Complete()` / `context.Fail(reason)`; late reports are ignored. `context.MarkZone(index)` shows the player where to go; `context.LoseRun(reason)` ends the run in defeat. |
| `QuestServices`, `QuestPrefabs` | Quests | The scene systems and prefabs a step may use, handed over explicitly. |
| `QuestDirector` | Quests | Runs the chains: places steps, drives them, applies failure rules, raises `Notice` and `MarkersChanged`. The run owner calls `BeginRun` and `SystemEntered`. |
| `QuestStationSpec`, `QuestStationSite` | Quests/Sites | A quest station (proper name, type, hidden profile) and where it lies. The site picks a zone of the step's system (never the jump point or star; unexplored station zones first) and joins the zone's contents **when the zone is revealed** (`IZoneAmendment`), so it is ordinary run state. |
| `QuestStationContact` | Quests/Sites | A quest giver: a hail opens their dialogue instead of the plain answer (`IStationContact`). |
| `SearchTrapStep` | Quests/Steps | Reusable: searching a station sets off a fuse instead of the first find. When it runs out, every ship flying in the zone (both sides) takes the blast, the station is destroyed and leaves metal wrecks. |
| `EscortStep` | Quests/Steps | Reusable: hail, accept, a shuttle with passengers launches and flies to a fleet hangar by itself; Pronin fighters go for it after a delay. Done on landing, failed if the shuttle is lost. |
| `QuestItem`, `QuestCargo` | Quests/Model, Quests | A quest item and which ship carries it. The director clears it per run and reports removed fleet ships; steps read it through `context.Cargo` and listen to `Lost`. |
| `RadiationDose` | Quests/Model | One crew's dose: `Expose` (returns deaths at the limit), `Recover`. |
| `QuestDepositSite` | Quests/Sites | A quest rock right next to a quest station (amend after the station's site). `ZoneView.ShowDeposit` shows it in a zone that is already built. |
| `GuardedDepositStep` + `DepositGuards` | Quests/Steps | Reusable: hail warns; mining the rock launches the station's guards, who defend the area around it. After enough mining the item is aboard the miner; done when it lands or leaves the zone. A lost item has to be mined again within the same attempt; only a mined-out rock fails the step. |
| `StationProcessingStep` + `DockedRadiation` | Quests/Steps | Reusable: the carrier docks and hands the item over; work runs while a crewed ship is docked; docked crews take a dose. The result boards the docked crewed ship with the lowest dose; done when it lands in a hangar. A crewless carrier at the station gives the result back to the station; anywhere else it fails the step. |
| `SupernovaStep` | Quests/Steps | Reusable finale: an inner `StationProcessingStep` without result (its own context reports to this step) holds the generator station; then a countdown. Leaving the system in time completes the step, running out calls `context.LoseRun`. |
| `QuestChainDefinition.WinsRun`, `QuestDirector.RunWon` / `RunLost` | Quests | A winning chain's completion raises `RunWon`; `context.LoseRun(reason)` ends every step and raises `RunLost`. The run owner shows the end. |
| `RunEndWindow`, `RunSummary`, `RunOutcome` | RunEnd (own module) | IMGUI end screen over a dark veil: won/lost, reason, people, ships, game time, "New run". Knows nothing about quests. |
| `RunEndFlow` | GenerationPreview | Glue: on `RunWon` waits until no radio call is open, then shows the end screen and stops time (`Time.timeScale = 0`); on `RunLost` at once, with 0 people and ships. "New run" starts the next galaxy seed (`GenerationPreviewController.StartNextRun`). |
| `QuestLogPanel` | Quests/View | IMGUI quest log at the bottom middle; also lists accepted station requests. |
| `SupplyRequest`, `SupplyRequestRules` | Quests/Requests | Small quests: a station wants an amount of fuel, metal or water and pays another supply at 1.5× or (20 %) an upgrade module. About every third inhabited, not hostile station asks; the amount grows with its people (100–600). |
| `StationRequestBoard`, `SupplyRequestContact`, `StationRequestEntry` | Quests/Requests | Rolls requests when a zone is revealed (seeded by zone and station) and makes the station's people its contact: a hail or a docked crew hears the offer and can accept. Once accepted, the next crewed ship docked there hands the goods over from the industrial ships' stores as soon as there is enough, and the station pays. Jumping away lets open requests expire. Stations with a quest contact are skipped. |
| `MeltdownQuest`, `MeltdownTexts` | Quests/Content/Meltdown | The Meltdown chain: settings, story dialogues, numbers. |

Hooks added to other modules for quests, all generic:

- `Zones`: `IStationContact` (`ZoneStation.SetContact`, `HasContactToTalkTo`), `ZoneStation.MarkDestroyed()`
  (no docking, search, picking, obstacle or model), `IZoneAmendment` + `ZoneRecord.Amend`,
  `ZoneContents.AddStation`, `ZoneContentGenerator.TryFindFreeSpot`, `ZoneView.ShowStation` / `RemoveStationView` / `ShowDeposit`,
  `ZoneStation.UseScriptedRadiation()` (docking skips the random radiation hazard).
- `Stations`: `IStationFindInterceptor` (`SearchService.AddInterceptor`): takes a find before it is handed out.
- `Ships`: `ShipRegistry.DamageShipsInZone`; ship class `StationGuard`, built by `CombatShipPrefabBuilder.LoadOrCreateStationGuard`
  (the Pronin fighter frame in bronze, without `AutonomousHunting`).
- `Galaxy`: `GalaxyMapView.SetHighlightedSystems`.
- `StarSystem`: `StarSystemView.SetHighlightedPointsOfInterest`.
- `Inspection`: a hail goes to the station's contact while it has something to say, and can be repeated.
- `Hud`: `FleetJumpDialog` names the quest step in the target system (optional reference).
- `GenerationPreview`: the controller starts Meltdown in `StartNewRun` and calls `SystemEntered` after
  `ZoneWorld.Show`. The scene builder adds a *Quests* object (dialogue service and window, director, log).

## Small quests (station requests)

Agreed on 2026-11-08: delivery requests only, paid from the fleet's stores when a ship docks, about one in
three stations, told on hail or docking, expiring with the jump. Rewards: another supply or an upgrade
module (which has no effect yet); more people joining was not chosen as a reward. Later ideas: other request
kinds (scan a rock, drive off raiders, fetch a sick person) and conditions for rescue ("bring ice, then we come").

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
| 3 | The sacred rock (`GuardedDepositStep`) | playable | Outpost Kethran Vigil (wary, 12 aboard) with a large ore rock (radius 8, 3000 units) right beside it. Hailing warns (two answers). The first harvester that mines the rock sets off 4 station guards (once per visit) who defend 6 km around it. After 12 harvester-seconds the solarite is aboard the miner (within the first load); done when it lands or leaves the zone, then Dr. Morrow's call. Lost solarite: mine again. Unescorted harvesters do not survive the guards. |
| 4 | Refining (`StationProcessingStep`) | playable | Industrial station Tamsin Forge: **abandoned** (nobody aboard, no attitude, damaged, radiation elevated, scripted) – the radiation drove its crew out, so docked crews keep the furnace running themselves. A hail reaches its automatic control system; the docked crew reports handover and result. The carrier docks and hands over the solarite; 4 min of refining while a crewed ship is docked. Dose: limit after 40 s docked, then one death per 8 s; 60 s away clears it; warning at 75 %. The refined solarite boards the docked ship with the lowest dose; done when it lands in a hangar. No extra attack. Losing either item ends the chain. |
| 5 | Ignition (`SupernovaStep`) | playable | Power station Calder Array (Halvorsen Kinetics), **abandoned** (Industrial model, no radiation); a hail reaches its control system. The carrier of the refined solarite docks and hands it over; 7 min of ignition sequence while a crewed ship is docked. No extra threat: the normal Pronin pursuit (about 13 min per system) is the pressure. At ignition Dr. Morrow calls and the star collapses in **3 min**; the player charges the jump to any system in range. Getting out completes the chain and **wins the run** (Morrow's epilogue, then the end screen). Still in the system when it runs out: the fleet is gone and the **run is lost**. |

The chain is marked `winsRun: true`.

## Playtest log

Newest first.

| Date | Setup | Result | Notes |
| --- | --- | --- | --- |
| 2026-11-05 | Step 5 moved into the start system by script, refined solarite loaded onto a shuttle, ignition and countdown fast-forwarded | Handover on docking, ignition sequence counted down while docked; at ignition the countdown showed 3:00 in the quest log. Jumping out completed the chain; the end screen waited for the three queued radio calls, then showed "won" (7236 people, 19 ships) with time stopped. "New run" started seed 2 with Meltdown at step 1. Letting the countdown run out showed "lost" with 0/0 | The end screen's labels were invisible at first: private `GUIStyle` fields came back as empty defaults after a reload; fixed with `[NonSerialized]`. The docked shuttle stayed behind at the jump, as any ship outside a hangar does. |
| 2026-11-05 | Steps 3 and 4 moved into the start system by script (seed 1), 3–5× time | Step 3, unescorted harvester: the guards launched at mining start; the solarite came aboard and was lost with the harvester; the step reset as intended. With 8 escorting fighters: all 4 guards shot down, solarite aboard, secured when the harvester left the zone; step 4 placed. Step 4: handover on docking, radiation alarm about 30 s later; the harvester's crew died at the limit and refining paused (3:57 left). After fast-forwarding, the result boarded a shuttle; its crew died docked, the forge kept the result; a second shuttle took it and landed; step 5 placed | Bug found and fixed in the run: crewless docked ships kept refining going and could strand the result. A Pronin pursuit raid happened at the same time and was fought off. |
| 2026-11-05 | Seed 1, driven through the Unity CLI: Ascalon zone revealed, a shuttle docked and searched; the shuttle stayed docked | The fuse started instead of the first find (0:27 shown), the alarm call opened; after 30 s the station vanished, 6 wrecks appeared, the docked shuttle was destroyed ("destroyed by a station explosion"); step 2 was placed in a system within one jump, shown in the jump warning | All notices in German as selected. |
| 2026-11-05 | Fleet jumped there, Meridian zone revealed, hailed, offer accepted at 4× time | The shuttle launched and landed in the liner within about 25 s of game time; the story call followed; step 3 was placed and shows "not playable yet". The 3 Pronin fighters then went for the liner | The station lay close to the arrival zone, so the escort was short. |

## Known issues and open points

- **Escort difficulty** depends on how far the station lies from the fleet. A nearby station makes the
  escort trivial. The attackers stay after the shuttle lands and hunt the fleet.
- **Guard difficulty**: 4 guards kill an unescorted harvester within seconds; an escort of fighters handles them.
  Only tested with 8 fighters. The guards only relaunch on a later visit to the system.
- **Refining with one shuttle** is not possible: 4 min of work against 40 s to the dose limit needs ships taking
  turns. Intended, but tune the numbers once it is played by hand.
- The **sacred rock** is only marked by size and its place beside the station; it has no own marker in the zone.
- **Supernova**: the star does not change on any map, and after a lost run the chain stays "active" in the
  quest log behind the end screen. The supernova only matters for the fleet; Pronin ships are not removed.
- IMGUI panels with lazily created private `GUIStyle` fields need `[NonSerialized]` (see `RunEndWindow`).
- Steps of a chain are only **saved for the run**. There is no save game yet.
- The quest log and dialogue window are blockout IMGUI with no sound or portraits. A long log can grow into
  the dialogue window.
- `StationHailResponse` still always tells the truth. Lying stations can now be built as an `IStationContact`.

## Notes

Free space for ideas, observations and decisions from later sessions.

-
