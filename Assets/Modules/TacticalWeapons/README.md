# Tactical weapons

Working notes for the warships' tactical weapons: the decisions behind them, how they are built and
what is still open. Add to the notes when tactical weapons change or are tested.

GDD references: *Kampf*, *Ressourcen (Munition)*, *Kreuzer 1* (Munitionsfabrik).

## Decisions

Agreed with the designer on 2026-11-10. Change them only on purpose.

| Topic | Decision |
| --- | --- |
| Ammunition | Only **tactical weapons** use ammunition. Standard guns of fighters and capital ships never do (decided 2026-11-09, see Combat README). |
| First weapon | **EMP missile** for the **cruiser**: at most **2** aboard, **1** at the start. |
| Flight | Fired at a **point in the cruiser's zone**. The missile flies visibly at 80 units/s and **cannot be shot down**. |
| Blast | Stuns **every ship of both sides** within **20 km (200 units)**, own ships and the cruiser included. The stun time falls **linearly** from the center to nothing at the edge: **Pronin 10 s**, **human ships 7 s** (they recover faster). A longer stun already running is kept. Docked ships are safe inside their carrier. |
| Stun | **Drives and weapons off**: main drive and thrusters stop, the ship drifts on its momentum; weapons hold fire; orders, AI and zone-jump charging wait and carry on afterwards. Shields, hangars and other work (mining, scanning) are not affected. |
| Building | **"Build EMP missile"** in the cruiser's ship window: **200 metal and 100 fuel** from the fleet's stores, paid when the build starts; **one at a time, 60 s**; only while the magazine is not full. Gas is not a stored good (it becomes fuel on delivery), so the recipe uses fuel. |
| Marker | Stunned ships of both sides get a **flickering ring** that closes in on the hull as the stun wears off (added 2026-11-10). |
| Aiming | **M** or the ship window button starts aiming with the selected cruiser; a ring the size of the blast follows the pointer; **left click** fires, **right click** or **M** again cancels. While aiming, the mouse neither selects nor orders ships. |

## How it is built

- `ShipAgent.Stun(seconds)` and `ShipMotion.Drift` (Ships module) hold the stun state; `ShipWeapons` (Combat) does not fire while stunned.
- `EmpBlast` (plain C#): stun time per side and distance.
- `MissileMagazine` (plain C#): missiles aboard, capacity and the build in progress.
- `EmpLauncher` (on the cruiser prefab): the magazine and the balancing values (capacity, start, build time, cost).
- `EmpService` (scene, next to the ship registries): checks and fires launches, pays for and ticks builds, detonates missiles against both registries, raises notices.
- `EmpMissile` and `EmpBlastEffect`: the missile in flight and the expanding blast ring. Blockout prefabs in `TacticalWeapons/Prefabs`, built by `TacticalWeaponPrefabBuilder` (Ships/Editor), which also adds the launcher to the cruiser.
- `EmpTargetingInput`: aiming with the preview ring; it suspends the mouse commands of `ShipCommandInput` while active.
- `StunMarkerView` (scene): a flickering ring in the EMP color around every stunned ship of both sides; it starts wider and closes in on the hull as the stun wears off. Rings are pooled, no ship prefab changes.
- Ship window (`EmpInspection`, Inspection module): magazine, build progress and stun time lines; fire and build buttons with reasons when disabled.
- Texts are in the string table `TacticalWeapons`.

## Playtest log

| Date | Setup | Result | Notes |
| --- | --- | --- | --- |
| 2026-11-10 | Cruiser at the jump point, Pronin fighters 60, 160 and 320 units away, EMP on the nearest | Detonation after 0.75 s; nearest Pronin stunned 10 s, the one 100 units from the center 5 s, the one 260 away untouched; the cruiser itself (60 away) 4.9 s. Stunned fighters drifted on their momentum | Values match the linear falloff exactly. |

## Open

- **Readability**: the 20 km ring is larger than the view at the default zoom; zoom out to see the whole blast area.
- Stunned capital ships still count for the **fleet jump**; whether a stun delays it is not decided.
- **More tactical weapons** and Pronin EMPs.
- No AI reaction to EMPs (the Pronin do not spread out or evade missiles).
