using System;
using LastFleet.Shared.Blockout;
using LastFleet.Shared.Text;
using LastFleet.ShipControl;
using LastFleet.Ships;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastFleet.TacticalWeapons
{
    /// <summary>
    /// Aiming an EMP missile. M (or the ship window's button) starts aiming with a selected warship
    /// that has a missile; a ring the size of the blast follows the pointer, a left click fires at that
    /// point, a right click or M again cancels. Meanwhile the mouse does not select or order ships.
    /// </summary>
    public sealed class EmpTargetingInput : MonoBehaviour
    {
        const int RingSegments = 64;

        [SerializeField] Camera _camera;
        [SerializeField] ShipCommandInput _commandInput;
        [SerializeField] EmpService _emp;
        [SerializeField] Material _previewMaterial;
        [SerializeField, Min(0.01f)] float _previewWidthUnits = 1.5f;

        LineRenderer _preview;
        EmpLauncher _launcher;
        // Kept apart from the launcher: a destroyed launcher compares equal to null, aiming must still end.
        bool _isTargeting;

        public bool IsTargeting => _isTargeting;

        /// <summary>Feedback for the player: how to aim, cancelled, or why aiming cannot start.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_camera == null || _commandInput == null || _emp == null || _previewMaterial == null)
            {
                Debug.LogError("EmpTargetingInput needs the camera, the ship command input, the EMP service and a preview material.", this);
                enabled = false;
                return;
            }
            _preview = BlockoutPrimitives.CreateLine(transform, _previewMaterial, "EMP Target Preview", _previewWidthUnits);
            BlockoutPrimitives.SetCircle(_preview, Vector3.zero, 1f, RingSegments);
            _preview.enabled = false;
        }

        void OnDisable()
        {
            EndTargeting();
        }

        /// <summary>Starts aiming with the first selected warship that can fire; returns feedback for the player.</summary>
        public GameText BeginTargeting()
        {
            EmpLauncher launcher = null;
            LaunchReadiness reason = LaunchReadiness.NoMissile;
            foreach (ShipAgent ship in _commandInput.Selection.Ships)
            {
                if (!ship.TryGetComponent(out EmpLauncher candidate))
                {
                    continue;
                }
                reason = _emp.CheckLaunch(candidate);
                if (reason == LaunchReadiness.Ready && candidate.Ship.Zone == _commandInput.ActiveZone)
                {
                    launcher = candidate;
                    break;
                }
                if (reason == LaunchReadiness.Ready)
                {
                    reason = LaunchReadiness.NotInZone;
                }
            }
            if (launcher == null)
            {
                return ReasonText(reason);
            }

            _launcher = launcher;
            _isTargeting = true;
            _commandInput.SuspendMouseCommands(this);
            return TacticalWeaponTexts.TargetingHint;
        }

        public static GameText ReasonText(LaunchReadiness reason)
        {
            return reason switch
            {
                LaunchReadiness.Stunned => TacticalWeaponTexts.LauncherStunned,
                LaunchReadiness.NotInZone => TacticalWeaponTexts.NotInViewedZone,
                _ => TacticalWeaponTexts.NoMissile
            };
        }

        void Update()
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (mouse == null || keyboard == null)
            {
                return;
            }

            if (keyboard.mKey.wasPressedThisFrame)
            {
                if (IsTargeting)
                {
                    Cancel();
                }
                else if (_commandInput.ActiveZone != null && _commandInput.Selection.Count > 0 && SelectionHasLauncher())
                {
                    Notice?.Invoke(BeginTargeting());
                }
                return;
            }
            if (!IsTargeting)
            {
                return;
            }

            // The launcher was lost, stunned or the player looks at another zone: aiming makes no sense any more.
            if (_launcher == null || _emp.CheckLaunch(_launcher) != LaunchReadiness.Ready || _launcher.Ship.Zone != _commandInput.ActiveZone)
            {
                Cancel();
                return;
            }

            Vector2 pointer = mouse.position.ReadValue();
            bool hasPoint = _commandInput.ActiveZone.Zone.TryRaycastPlane(_camera.ScreenPointToRay(pointer), out Vector3 worldPoint);
            UpdatePreview(hasPoint && !_commandInput.IsOverUi(pointer), worldPoint);

            if (mouse.rightButton.wasPressedThisFrame)
            {
                Cancel();
                return;
            }
            if (mouse.leftButton.wasPressedThisFrame && hasPoint && !_commandInput.IsOverUi(pointer))
            {
                LaunchReadiness result = _emp.Launch(_launcher, worldPoint);
                if (result != LaunchReadiness.Ready)
                {
                    Notice?.Invoke(ReasonText(result));
                }
                EndTargeting();
            }
        }

        bool SelectionHasLauncher()
        {
            foreach (ShipAgent ship in _commandInput.Selection.Ships)
            {
                if (ship.TryGetComponent(out EmpLauncher _))
                {
                    return true;
                }
            }
            return false;
        }

        void UpdatePreview(bool visible, Vector3 worldPoint)
        {
            _preview.enabled = visible;
            if (!visible)
            {
                return;
            }
            _preview.transform.position = worldPoint;
            _preview.transform.rotation = _commandInput.ActiveZone.transform.rotation;
            _preview.transform.localScale = Vector3.one * _emp.BlastRadiusUnits;
        }

        void Cancel()
        {
            EndTargeting();
            Notice?.Invoke(TacticalWeaponTexts.TargetingCancelled);
        }

        void EndTargeting()
        {
            if (_commandInput != null)
            {
                _commandInput.ResumeMouseCommands(this);
            }
            if (_preview != null)
            {
                _preview.enabled = false;
            }
            _launcher = null;
            _isTargeting = false;
        }
    }
}
