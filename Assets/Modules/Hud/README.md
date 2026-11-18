# HUD

Prototype HUD in IMGUI: supply bar, threat display, jump status, jump dialog and success toasts.

## Success toasts

Small moment-to-moment wins pop up at the top centre, below the jump status and the fleet module button
(`SuccessToasts`, timing in `SuccessToastQueue`).

Agreed behaviour:

- Up to 3 toasts at once, 4 s each with a short fade in and out, on real time (time compression does not rush them).
  Further toasts wait; in a burst at most 8 wait and the oldest waiting ones give way.
- Toasts only show; clicks pass through to the ships. The matching notice still goes to the notice feed.
- Sources raise a `Success` event next to their `Notice` event; `SuccessToastSources` (GenerationPreview) connects them:
  - People rescued: every time a ferry sets people down on a carrier (`FleetPopulation`), and survivors aboard a
    recovered stranded ship.
  - New ship in the fleet: a recovered stranded ship (`StrandedShipRecovery`) or a recrewed abandoned ship (`CrewTransferService`).
  - Water looks good: only after a low-water warning, once the water lasts at least 15 minutes again (`FleetWaterSupply`).
  - Zone secured: the last enemy in a zone with fleet ships falls (`CombatService`).
  - Finds secured: finds unloaded into the fleet (`RewardDelivery`).
  - Deposit mined out: reported by the harvester whose extraction emptied it (`MiningCoordinator`).
