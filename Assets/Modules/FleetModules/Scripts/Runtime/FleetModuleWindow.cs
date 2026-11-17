using System.Collections.Generic;
using LastFleet.Localization;
using LastFleet.ShipControl;
using LastFleet.Shared.Text;
using UnityEngine;

namespace LastFleet.FleetModules
{
    /// <summary>
    /// The fleet's module slots and the choice for a found module. A button at the top middle shows the
    /// slots in use and how many modules wait; it or U opens the window, Esc or U closes it. The game keeps
    /// running while it is open (designer: no pop-up in the middle of a fight). Prototype UI in IMGUI.
    /// </summary>
    public sealed class FleetModuleWindow : MonoBehaviour
    {
        const float WidthPixels = 480f;

        // Top middle, below the supply bar, the threat line and the jump drive status.
        const float ButtonTopPixels = 112f;

        [SerializeField] FleetModuleService _modules;
        [SerializeField] ShipCommandInput _shipInput;

        // The button and the window block clicks separately, so they need owners of their own.
        readonly object _buttonOwner = new object();
        readonly List<FleetModuleKind> _offer = new List<FleetModuleKind>();
        bool _isOpen;
        int _replacingOfferIndex = -1;

        // Not serialized: Unity would otherwise restore an empty default style after a reload.
        [System.NonSerialized] GUIStyle _wrapStyle;
        [System.NonSerialized] GUIStyle _nameStyle;

        public bool IsOpen => _isOpen;

        void Awake()
        {
            if (_modules == null || _shipInput == null)
            {
                Debug.LogError("FleetModuleWindow needs the fleet module service and the ship command input to block clicks behind it.", this);
                enabled = false;
            }
        }

        void OnDisable()
        {
            if (_shipInput != null)
            {
                _shipInput.ClearBlockedScreenArea(this);
                _shipInput.ClearBlockedScreenArea(_buttonOwner);
            }
        }

        public void Open()
        {
            _isOpen = true;
            _replacingOfferIndex = -1;
        }

        public void Close()
        {
            _isOpen = false;
            _replacingOfferIndex = -1;
            _shipInput.ClearBlockedScreenArea(this);
        }

        void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown && HandleKey(Event.current.keyCode))
            {
                Event.current.Use();
                return;
            }

            _wrapStyle ??= new GUIStyle(GUI.skin.label) { wordWrap = true };
            _nameStyle ??= new GUIStyle(_wrapStyle) { fontStyle = FontStyle.Bold };
            DrawHudButton();
            if (_isOpen)
            {
                DrawWindow();
            }
        }

        bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.U)
            {
                if (_isOpen)
                {
                    Close();
                }
                else
                {
                    Open();
                }
                return true;
            }
            if (key == KeyCode.Escape && _isOpen)
            {
                Close();
                return true;
            }
            return false;
        }

        void DrawHudButton()
        {
            FleetModuleSlots slots = _modules.Slots;
            int waiting = _modules.WaitingModules;
            GameText label = waiting > 0
                ? FleetModuleTexts.HudButtonWaiting.Format(("used", slots.Installed.Count), ("slots", slots.SlotCount), ("count", waiting))
                : FleetModuleTexts.HudButton.Format(("used", slots.Installed.Count), ("slots", slots.SlotCount));
            GUIContent content = new GUIContent(GameTextResolver.Resolve(label));
            Vector2 size = GUI.skin.button.CalcSize(content);
            Rect rect = new Rect((Screen.width - size.x) * 0.5f, ButtonTopPixels, size.x, size.y);
            if (GUI.Button(rect, content))
            {
                if (_isOpen)
                {
                    Close();
                }
                else
                {
                    Open();
                }
            }
            if (Event.current.type == EventType.Repaint)
            {
                // GUI coordinates start top left, Input System screen coordinates bottom left.
                _shipInput.SetBlockedScreenArea(_buttonOwner, new Rect(rect.x, Screen.height - rect.yMax, rect.width, rect.height));
            }
        }

        void DrawWindow()
        {
            FleetModuleSlots slots = _modules.Slots;
            // Copied once per event: answering the offer changes the service's list during the layout.
            _offer.Clear();
            _offer.AddRange(_modules.Offer);
            if (_replacingOfferIndex >= _offer.Count)
            {
                _replacingOfferIndex = -1;
            }

            Rect area = new Rect((Screen.width - WidthPixels) * 0.5f, Screen.height * 0.2f, WidthPixels, Screen.height * 0.7f);
            GUILayout.BeginArea(area);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(Resolve(FleetModuleTexts.WindowTitle.Format(("used", slots.Installed.Count), ("slots", slots.SlotCount))));

            // Decided once, so a click handled below cannot change which controls this event draws.
            bool replacing = _replacingOfferIndex >= 0;
            int action = replacing ? DrawSlotChoice(slots) : DrawSlots(slots);
            if (!replacing)
            {
                DrawOffer(slots);
            }
            bool closed = GUILayout.Button(Resolve(FleetModuleTexts.CloseButton));
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                Rect box = GUILayoutUtility.GetLastRect();
                _shipInput.SetBlockedScreenArea(this, new Rect(area.x + box.x, Screen.height - area.y - box.yMax, box.width, box.height));
            }
            GUILayout.EndArea();

            if (action >= 0 && _replacingOfferIndex >= 0)
            {
                int offerIndex = _replacingOfferIndex;
                _replacingOfferIndex = -1;
                _modules.Replace(offerIndex, action);
            }
            else if (closed)
            {
                Close();
            }
        }

        /// <summary>The installed modules and free slots; never asks for an action.</summary>
        int DrawSlots(FleetModuleSlots slots)
        {
            for (int i = 0; i < slots.SlotCount; i++)
            {
                if (i < slots.Installed.Count)
                {
                    DrawModule(slots.Installed[i]);
                }
                else
                {
                    GUILayout.Label(Resolve(FleetModuleTexts.EmptySlot));
                }
            }
            return -1;
        }

        /// <summary>One button per installed module to replace it; returns the chosen slot or -1.</summary>
        int DrawSlotChoice(FleetModuleSlots slots)
        {
            GUILayout.Label(Resolve(FleetModuleTexts.ChooseSlotToReplace.Format(("module", FleetModuleTexts.Names.For(_offer[_replacingOfferIndex])))), _wrapStyle);
            int chosen = -1;
            for (int i = 0; i < slots.Installed.Count; i++)
            {
                FleetModuleKind kind = slots.Installed[i];
                if (GUILayout.Button(Resolve(FleetModuleTexts.Names.For(kind)) + " – " + Resolve(FleetModuleTexts.Effects.For(kind))))
                {
                    chosen = i;
                }
            }
            if (GUILayout.Button(Resolve(FleetModuleTexts.BackButton)))
            {
                _replacingOfferIndex = -1;
            }
            return chosen;
        }

        void DrawOffer(FleetModuleSlots slots)
        {
            GUILayout.Space(8f);
            int waiting = _modules.WaitingModules;
            if (waiting == 0 || _offer.Count == 0)
            {
                GUILayout.Label(Resolve(FleetModuleTexts.NoModuleWaiting), _wrapStyle);
                return;
            }

            GUILayout.Label(Resolve(FleetModuleTexts.OfferHeader.Format(("count", waiting))));
            int installIndex = -1;
            for (int i = 0; i < _offer.Count; i++)
            {
                GUILayout.BeginHorizontal();
                DrawModule(_offer[i]);
                GameTextKey button = slots.HasFreeSlot ? FleetModuleTexts.InstallButton : FleetModuleTexts.ReplaceButton;
                if (GUILayout.Button(Resolve(button), GUILayout.Width(110f)))
                {
                    installIndex = i;
                }
                GUILayout.EndHorizontal();
            }
            bool discarded = GUILayout.Button(Resolve(FleetModuleTexts.DiscardButton));

            if (installIndex >= 0 && slots.HasFreeSlot)
            {
                _modules.Install(installIndex);
            }
            else if (installIndex >= 0)
            {
                _replacingOfferIndex = installIndex;
            }
            else if (discarded)
            {
                _modules.Discard();
            }
        }

        void DrawModule(FleetModuleKind kind)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(Resolve(FleetModuleTexts.Names.For(kind)), _nameStyle);
            GUILayout.Label(Resolve(FleetModuleTexts.Effects.For(kind)), _wrapStyle);
            GUILayout.EndVertical();
        }

        static string Resolve(GameText text)
        {
            return GameTextResolver.Resolve(text);
        }
    }
}
