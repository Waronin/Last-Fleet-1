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
| `ShipMotion.SetCourse(direction, maximumSpeed)` | Ships | Course without braking and with a speed limit; used by attack runs and jump charging. |
| `CombatService` | Combat | The only place that knows both registries: finds targets, resolves hits (shield first, then hull), carries out attack orders, reports enemy losses. |
| `ShipWeapons` + `WeaponSettings` + `WeaponCycle` | Combat | Range, damage, bursts, firing arc, linear hit falloff, round speed. A hit is rolled on firing and lands when the round arrives. |
| `ShipShield` + `ShieldCharge` | Combat | Absorbs weapon hits only; recharges after a delay without hits. Hazards bypass it. |
| `FighterCombat` | Combat | Attack runs after an order; follow-up target after a kill; self-defense when idle (`_defendWhenIdle`). |
| `AutonomousHunting` + `HuntingRules` | Combat | Enemy AI: small ships before capital ships, isolated before escorted, near before far. Off for ships on the player's side. |
| `ProjectileShotView` / `BeamShotView` | Combat/View | Looks only: human tracer rounds with muzzle flash, Pronin energy beams. |
| `CombatShipPrefabBuilder` | Ships/Editor | Builds Cruiser, Fighter and ProninFighter; all balance values below live there. |
| `DeveloperRaidInput` | GenerationPreview | **P** drops 3 Pronin fighters into the viewed zone. |

Controls: right click on an enemy attacks it with the selected ships. Fighters without orders attack
enemies in weapon range on their own; ships following an order (e.g. returning to the hangar) do not.

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
- **Feedback**: no shield flash, hull sparks, explosions or wrecks yet (GDD: wrecks give metal).
- **Stances** (aggressive, defensive, escort, retreat) and **target priorities** for capital ships
  are not built; `FighterCombat._defendWhenIdle` is the first switch towards them.
- **Ammunition** is not consumed yet (GDD: projectile weapons need ammunition from the cruiser).
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
  saved several components disabled. Only the Pronin beam's `LineRenderer` is meant to be disabled.

## Notes

Free space for ideas, observations and decisions from later sessions.

-
