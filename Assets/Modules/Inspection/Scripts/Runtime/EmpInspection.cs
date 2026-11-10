using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.TacticalWeapons;

namespace LastFleet.Inspection
{
    /// <summary>
    /// The tactical weapon part of the ship window: missiles and build progress of a warship, how long
    /// a ship stays stunned, and the buttons to fire an EMP missile and to build a new one.
    /// </summary>
    public sealed class EmpInspection
    {
        static readonly GameText FireHint = GameText.Verbatim("M");

        readonly EmpService _emp;
        readonly EmpTargetingInput _targeting;

        public EmpInspection(EmpService emp, EmpTargetingInput targeting)
        {
            _emp = emp;
            _targeting = targeting;
        }

        public void Describe(ShipAgent ship, List<GameText> lines)
        {
            if (ship.IsStunned)
            {
                lines.Add(TacticalWeaponTexts.StunnedLine.Format(("seconds", ship.StunSecondsLeft)));
            }
            if (!ship.TryGetComponent(out EmpLauncher launcher))
            {
                return;
            }
            MissileMagazine magazine = launcher.Magazine;
            lines.Add(TacticalWeaponTexts.MagazineLine.Format(("count", magazine.Count), ("capacity", magazine.Capacity)));
            if (magazine.IsBuilding)
            {
                lines.Add(TacticalWeaponTexts.BuildingLine.Format(("seconds", magazine.BuildSecondsLeft)));
            }
        }

        public void CollectActions(IReadOnlyList<ShipAgent> ships, List<InspectionAction> actions)
        {
            EmpLauncher launcher = FirstLauncher(ships);
            if (launcher == null)
            {
                return;
            }

            LaunchReadiness launch = _emp.CheckLaunch(launcher);
            actions.Add(new InspectionAction(TacticalWeaponTexts.FireAction, FireHint, launch == LaunchReadiness.Ready,
                EmpTargetingInput.ReasonText(launch), _targeting.BeginTargeting));

            BuildReadiness build = _emp.CheckBuild(launcher);
            actions.Add(new InspectionAction(TacticalWeaponTexts.BuildAction, null, build == BuildReadiness.Ready, BuildReason(build, launcher),
                () => Build(launcher)));
        }

        static EmpLauncher FirstLauncher(IReadOnlyList<ShipAgent> ships)
        {
            foreach (ShipAgent ship in ships)
            {
                if (ship.TryGetComponent(out EmpLauncher launcher))
                {
                    return launcher;
                }
            }
            return null;
        }

        GameText Build(EmpLauncher launcher)
        {
            BuildReadiness result = _emp.OrderBuild(launcher);
            return result == BuildReadiness.Ready
                ? TacticalWeaponTexts.BuildStarted.Format(("metal", launcher.MetalPerMissile), ("fuel", launcher.FuelPerMissile))
                : BuildReason(result, launcher);
        }

        static GameText BuildReason(BuildReadiness readiness, EmpLauncher launcher)
        {
            return readiness switch
            {
                BuildReadiness.MagazineFull => TacticalWeaponTexts.MagazineFull,
                BuildReadiness.AlreadyBuilding => TacticalWeaponTexts.AlreadyBuilding,
                _ => TacticalWeaponTexts.NotEnoughSupplies.Format(("metal", launcher.MetalPerMissile), ("fuel", launcher.FuelPerMissile))
            };
        }
    }
}
