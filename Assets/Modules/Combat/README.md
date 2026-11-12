# Combat

Working notes for the combat prototype: how it is built, the current numbers, what playtests showed
and what is still open. Add to the playtest log and the notes whenever combat changes or is tested.

GDD references: *Kampf*, *Kleine Schiffe*, *Kapitalschiffe*, *Der Feind*, *Ressourcen (Munition)*.

## How it is built

| Piece | Where | Role |
| --- | --- | --- |
| `Allegiance` | Ships | Side of a ship (`HumanFleet`, `Pronin`), set by the registry that owns it. |
| `ShipRegistry` (one per side) | Ships | Fleet systems (mining, recall, selection, population) only see their own side. `TransferTo` moves a ship to the other side, e.g. on capture; `ShipRemoved` means "no longer ours", losses come through `ShipLost`. |
| `ShipOrder.Intercept` + `InterceptRun` | Ships | Attack run: aim beside the target, fly past at run speed, hold course to the break-off distance, come about. Follows the target across zones. |
| `ShipMotion.SetCourse(direction, maximumSpeed)` | Ships | Course without braking and with a speed limit; used by attack runs, station keeping and jump charging. |
| `ObstacleField` + `ObstacleAvoidance` + `Obstacle` | Ships | Rocks and wrecks, stations and capital ships of both sides as circles per zone. Ships under main drive steer past them (look-ahead, tangent past the nearer side); the obstacle holding the destination (mining slot, docking port, landing bay) is never avoided. Small ships are not obstacles. |
| `ShipOrder.Follow` | Ships | Station keeping at an offset in the followed ship's frame, at its speed plus a share of the gap; across zones. |
| Cover (`CombatService.HasLineOfFire`) | Combat | Weapons only fire with a free line of fire; obstacles between shooter and target block it (the two themselves do not). Chasing and self-defense still pick targets in cover and fly around. |
| `EscortDuty` | Combat | Whom an escort protects: one ship or its whole squad (led by the current leader). While the lead sits in a hangar the escort waits at the carrier and picks the lead up again on launch. |
| `CombatService` | Combat | The only place that knows both registries: finds targets, resolves hits (shield first, then hull), carries out attack orders, reports enemy losses. |
| `ShipWeapons` + `WeaponSettings` + `WeaponCycle` | Combat | Range, damage, bursts, firing arc, linear hit falloff, round speed. A hit is rolled on firing and lands when the round arrives. |
| `ShipShield` + `ShieldCharge` | Combat | Absorbs weapon hits only; recharges after a delay without hits. Hazards bypass it. |
| `FighterCombat` | Combat | Attack runs after an order; follow-up target after a kill; self-defense when idle (`_defendWhenIdle`). Escort: keep station around the protected ships, attack enemies within the guard radius of any of them, break a chase off at the leash and return. |
| `AutonomousHunting` + `HuntingRules` | Combat | Enemy AI: small ships before capital ships, isolated before escorted, near before far; or random targets for carrier waves. Off for ships on the player's side. |
| `ProjectileShotView` / `BeamShotView` | Combat/View | Looks only: human tracer rounds with muzzle flash, Pronin energy beams. |
| `HitFeedbackView` | Combat/View | Shield hit: a ring around the ship flares and fades. Hull hit (weapons or hazards): sparks. |
| `DestructionView` + `ExplosionEffect` | Combat/View | Explosion (fireball, shock ring, debris) in the zone where a ship is destroyed. Prefab: `Combat/Prefabs/Explosion`. |
| `ShipWreckage` | Ships | On destruction the ship leaves a wreck: a `ResourceDeposit` of shape `Wreck` and resource `Metal`, added to the zone's run state. Harvesters salvage it with their normal mining loop and deliver the metal to the refinery. |
| `CombatShipPrefabBuilder` + `CombatEffectsBuilder` | Ships/Editor | Build Cruiser, Fighter, ProninFighter and the explosion; all balance values below live there. |
| `DeveloperRaidInput` | GenerationPreview | **P** drops 3 Pronin fighters into the viewed zone. |
| `PursuitClock` + `ProninAttack` | Pursuit | Countdown (13 min) per system arrival. When it runs out, a Pronin carrier appears in a random zone (not the one with most of our capital ships) and launches 6 fighters that hunt random targets (`HuntTargetChoice.Random`: small or capital ships), then another wave every 60 s up to 12 living fighters (`CarrierSchedule`). After 30 s the carrier hunts: every 2 s it checks the zone with the most of our capital ships (counting ships still jumping there), jumps after them, stops 120 units short and fires its beams; it closes in again when they move away inside the zone. **T** skips the countdown 1 min ahead. |

Controls: right click on an enemy attacks it with the selected ships. Right click with fighters selected
on another of our ships makes them escort it, or its squad, spread evenly around it. Fighters without
orders attack enemies in weapon range on their own; ships following an order (e.g. returning to the
hangar) do not. Any new order ends an escort.

## Current values

Scale: 1 unit = 100 m, a zone is 50 km (500 units) in radius.

| | Fighter | Pronin fighter | Cruiser |
| --- | --- | --- | --- |
| Hull / shield | 60 / 60 | 50 / 40 | 3500 / – |
| Shield recharge | 6/s after 4 s | 5/s after 5 s | – |
| Crew | 2 | – (machine) | – |
| Top speed / acceleration / turn rate | 60 / 24 / 180°/s | 65 / 26 / 200°/s | 14 / 1.2 / 14°/s |
| Range | 250 | 250 | 250 |
| Damage per hit | 4 | 7 | 3 |
| Fire pattern | bursts of 6, 0.1 s apart, 1.2 s pause | single beam every 0.9 s | bursts of 8, 0.15 s apart, 2.5 s pause |
| Hit chance close → edge (linear) | 85 % → 5 % | 80 % → 5 % | 60 % → 5 % |
| Firing arc | 30° forward | 40° forward | 360° |
| Round speed | 150 units/s | instant | 150 units/s |
| Attack run speed / break-off | 30 / 25 | 30 / 25 | – |
| Self-defense when idle | on | off (hunts instead) | always fires (turrets) |
| Hangar | – | – | 8 slots, 1 launch per second |
| Wreck on destruction | 25 metal (1 salvage slot) | 25 metal (1 salvage slot) | 6 pieces × 150 metal |
| Explosion size | 1.2 | 1.2 | 14 |
| Obstacle / cover radius | – | – | 6 (liner 6.5, refinery 4.5) |
| Escort: distance / guard radius / leash | 8 / 150 / 250 | – (does not escort) | – |

Obstacle avoidance: ships look ahead 2.5 s of travel (at least 15 units) and keep 2 units plus their
own radius clear. Rocks and wrecks count with their rock radius, stations with their footprint.

Metal: the refinery stores up to 5000. A harvester holds 120, so one Pronin wreck is about a fifth of
a load; harvesters move on to the next wreck in the zone by themselves.

Derived numbers (raw, assuming the target stays in the arc):

| | Fighter | Pronin fighter | Cruiser |
| --- | --- | --- | --- |
| Damage per second, every shot hits | 14.1 | 7.8 | 6.8 |
| Damage per second at point blank | 12.0 | 6.2 | 4.1 |
| Damage per second at half range (125) | 6.4 | 3.3 | 2.2 |
| Effective hit points (hull + shield) | 120 | 90 | 3500 |

So in a close duel a fighter kills a Pronin fighter in about 7.5 s, while the Pronin needs about 19 s.

## Playtest log

Newest first. Note the date, the setup, what happened and the version of the values.

| Date | Setup | Result | Notes |
| --- | --- | --- | --- |
| 2026-11-03 | Capital ships sent to Belt A, then the countdown skipped; later sent back to the jump point | Carrier appeared in a moon zone, followed the fleet to Belt A and later back to the jump point; second wave at 60 s brought it to 12 fighters | Before the chase the carrier struck the empty jump point once and then idled there. |
| 2026-11-03 | Countdown skipped to arrival, fleet docked at the jump point | Pronin carrier (hull 4000, beams 9 per 1.2 s, range 250) appeared in a moon zone, launched 6 fighters in ~4 s; they jumped to the jump point and spread over refinery and liner. After 30 s the carrier jumped in, stopped ~120 units from our capital ships and hit the liner; its hull was still full a minute later | The cruiser keeps shooting the nearest target, so it ignores the carrier while fighters are close. |
| 2026-11-01 | Fighter sent straight through the liner (radius 6.5) | Passed beside it, closest 7.3 units from its center | Without avoidance it would have flown through the hull. Closest point came while flipping, where avoidance does not steer. |
| 2026-11-01 | Pronin fighter parked behind the liner as seen from the cruiser | No line of fire from the cruiser or the refinery | Cover works for capital ships as cover; rocks were not in the test zone. |
| 2026-11-01 | 4 fighters escort a harvester squad of 3 on a 200-unit trip, then 3 Pronin appear 120 units away | Escorts stayed within ~45 units while moving and settled at 8; all went for the raid, destroyed it and took their stations again; after the harvesters landed the escort waited at the refinery | First try without station keeping overshot by up to 130 units; now they match the protected ship's speed. |
| 2026-10-30 | 8 fighters + cruiser vs 6 Pronin, then 3 harvesters sent to the wrecks | Pronin destroyed in ~12 s (1 fighter lost); 6 wrecks of 25 metal; the harvesters salvaged all of them in ~40 s, 151 metal arrived at the refinery | Explosions, shield rings, sparks and wreck plates all visible. The extra metal is the harvesters' mining bonus. |
| 2026-10-30 | 8 idle fighters, 3 Pronin from behind | Fighters engaged on their own after ~3 s; Pronin went for the outermost fighters | Before self-defense idle fighters never fired: the 30° arc never pointed at the attacker. |
| 2026-10-30 | 8 fighters + cruiser vs 6 Pronin, 160 units away, range 250, linear falloff | All Pronin destroyed in ~12 s, 1 fighter lost | Long range plus linear falloff is very lethal; the cruiser joins at once. Too short again. |
| 2026-10-29 | 8 fighters vs 6 Pronin, ranges 35/40/70, falloff quadratic, attack runs | ~90 s, no losses | Runs visible as oscillating distance (5–80 units). Felt long; fighters too strong. |
| 2026-10-29 | Same, with a catch-up speed for fleeing targets | Passes up to 200 units wide | Both sides chased each other faster and faster; catch-up removed. |
| 2026-10-29 | 8 fighters vs 3 Pronin, first version (instant hits, 360° fire, range 12) | All Pronin dead in < 6 s, no losses | Focus fire: ~1.3 s per kill. No decisions possible. |

## Known issues and open points

- **Balancing**: fights are decided by range and numbers; the player's fighters are clearly stronger.
  Target for a fighter duel: 10–20 s. Levers: hull and shield, damage per hit, long-range hit chance,
  burst pause, cruiser range.
- **Focus fire**: all ships take the nearest enemy, so squads gang up on one target. Spreading
  targets could reuse the claims pattern (`MiningSlotClaims`, `DockingPortClaims`).
- **Wrecks** (decided 2026-11-10): ships of both sides leave metal wrecks. Small ships leave one piece
  of 25 metal (radius 0.8); capital ships, the Pronin carrier included, break up into 6 hull sections
  of 150 metal with a radius of 2.5, spread over their hull radius plus 4.5 (`WreckScatter`), so the
  sections and their salvage slots keep apart. Each section still has one salvage slot. Only metal:
  cargo, finds and people are lost; docked, captured or abandoned ships leave nothing. The larger
  sections are obstacles and give cover like rocks. Wrecks stay for the whole run
  and nothing announces them, the player has to spot them. Metal pays for hull repairs (see the
  Repairs module); upgrades come later.
- **Effects** are blockout: no sound, no camera shake, the shock ring is a flat circle.
- **Stances** (decided 2026-11-09): escort *is* the fighters' stance. No defensive or retreat stances
  and no target priorities for capital ships; fighters that hold back would leave the fleet unprotected.
  `FighterCombat._defendWhenIdle` is the switch for idle self-defense.
- **Avoidance limits**: only while the main drive carries the ship; braking, flipping and thruster
  maneuvers fly straight. Capital ships are circles, so long hulls block more than they look. Pronin
  hunters chase into cover but do not seek it themselves; nobody looks for cover on purpose yet.
- **Escort**: an escort counts its distance from the lead only; it can lose a squad member that
  strays far from the leader.
- **Ammunition and tactical weapons** (decided 2026-11-09): standard guns of fighters and capital ships
  never use ammunition, so no fighter has to be resupplied. Instead, warships get extra **tactical
  weapons** (for example **EMP missiles**), and only those use ammunition. The cruiser's EMP missile is
  built (see the TacticalWeapons module); stunned ships drift and hold their fire (`ShipAgent.Stun`).
- **Hazards** (shards, gas, radiation) only hit the player's registry; the GDD wants them for both
  sides. Shields do not block hazards.
- **Capture**: `TransferTo` exists, but what happens to ships docked in a captured carrier and to its
  passengers is undecided.
- **Rounds in flight** are lost when the shooter lands or is destroyed.

## Working notes

- Prefabs are generated: change values in `CombatShipPrefabBuilder`, then rebuild the three combat
  prefabs and the preview scene (*Last Fleet / Rebuild Ship Prefabs*, *Last Fleet / Create Generation
  Preview Scene*). Serialized values in a prefab override new field defaults.
- When rebuilding through the Unity CLI, build each prefab in its own call and check that no
  `MonoBehaviour` was saved with `m_Enabled: 0`; one combined rebuild right after a script reload
  saved several components disabled. Only `LineRenderer`s are meant to be saved disabled: the Pronin
  beam and the shield flash rings.

## Notes

Free space for ideas, observations and decisions from later sessions.

-
