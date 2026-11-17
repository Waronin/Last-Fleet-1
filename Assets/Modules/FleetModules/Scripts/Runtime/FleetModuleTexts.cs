using LastFleet.Shared.Text;

namespace LastFleet.FleetModules
{
    /// <summary>Texts of fleet modules in the string table "FleetModules".</summary>
    public static class FleetModuleTexts
    {
        const string Table = "FleetModules";

        public static readonly EnumTextKeys<FleetModuleKind> Names = new EnumTextKeys<FleetModuleKind>(Table, "module.name");

        /// <summary>Advantage and drawback in one short line.</summary>
        public static readonly EnumTextKeys<FleetModuleKind> Effects = new EnumTextKeys<FleetModuleKind>(Table, "module.effect");

        /// <summary>{count}</summary>
        public static readonly GameTextKey ModuleReady = new GameTextKey(Table, "notice.ready");

        /// <summary>{module}</summary>
        public static readonly GameTextKey Installed = new GameTextKey(Table, "notice.installed");

        /// <summary>{module}, {removed}</summary>
        public static readonly GameTextKey Replaced = new GameTextKey(Table, "notice.replaced");

        public static readonly GameTextKey Discarded = new GameTextKey(Table, "notice.discarded");

        /// <summary>{used}, {slots}</summary>
        public static readonly GameTextKey WindowTitle = new GameTextKey(Table, "window.title");

        public static readonly GameTextKey EmptySlot = new GameTextKey(Table, "window.empty-slot");

        /// <summary>{count}</summary>
        public static readonly GameTextKey OfferHeader = new GameTextKey(Table, "window.offer");

        public static readonly GameTextKey NoModuleWaiting = new GameTextKey(Table, "window.none-waiting");

        /// <summary>{module}</summary>
        public static readonly GameTextKey ChooseSlotToReplace = new GameTextKey(Table, "window.choose-slot");

        public static readonly GameTextKey InstallButton = new GameTextKey(Table, "button.install");
        public static readonly GameTextKey ReplaceButton = new GameTextKey(Table, "button.replace");
        public static readonly GameTextKey DiscardButton = new GameTextKey(Table, "button.discard");
        public static readonly GameTextKey BackButton = new GameTextKey(Table, "button.back");
        public static readonly GameTextKey CloseButton = new GameTextKey(Table, "button.close");

        /// <summary>{used}, {slots}</summary>
        public static readonly GameTextKey HudButton = new GameTextKey(Table, "hud.button");

        /// <summary>{used}, {slots}, {count}</summary>
        public static readonly GameTextKey HudButtonWaiting = new GameTextKey(Table, "hud.button-waiting");
    }
}
