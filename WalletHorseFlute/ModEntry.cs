using ContentPatcher;
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Triggers;
using WalletHorseFlute.Helpers;

namespace WalletHorseFlute
{
    internal sealed class ModEntry : Mod
    {
        internal static IMonitor ModMonitor { get; private set; } = null!;
        internal static IModHelper ModHelper { get; private set; } = null!;
        internal static ModConfig Config { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            ModHelper = helper;

            Config = helper.ReadConfig<ModConfig>();
            
            Utils.ModHelper = helper;

            I18n.Init(helper.Translation);

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.SaveCreated += this.OnSaveLoaded;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Player.InventoryChanged += this.OnInventoryChanged;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var ContentPatcher = ModHelper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
            if (ContentPatcher == null)
            {
                Log.Error("ContentPatcher not found. Button's Extra Books requires ContentPatcher to function.");
                return;
            }

            ContentPatcher.RegisterToken(ModManifest, "ModEnabled", () =>
            {
                return new[] { Config.Enabled ? "true" : "false" };
            });

            TriggerActionManager.RegisterAction("HeavyStarRuler.WalletHorseFluteCore_UnlockHorseFlutePower", HandleUnlockAction);

            var configMenu = ModHelper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null) return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => 
                {
                    ModHelper.WriteConfig(Config);
                    if (Context.IsWorldReady)
                    {
                        if (!Config.Enabled)
                        {
                            // If mod was just turned "OFF"
                            Utils.RevertModChanges(Game1.player);
                        }
                        else
                        {
                            // If mod was just turned "ON"
                            Utils.DumpUnnecessaryFlutes(Game1.player);
                        }
                    }
                    // Force a full refresh so Content Patcher updates the UI immediately
                    Helper.GameContent.InvalidateCache("Data/Powers");
                    Helper.GameContent.InvalidateCache("Data/Shops");
                }
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Config_EnableMod_Label(),
                tooltip: () => I18n.Config_EnableMod_Tooltip(),
                getValue: () => Config.Enabled,
                setValue: value => Config.Enabled = value
            );

            configMenu.AddKeybindList(
                mod: ModManifest,
                name: () => I18n.Config_Hotkey_Label(),
                tooltip: () => I18n.Config_Hotkey_Tooltip(),
                getValue: () => Config.Hotkey,
                setValue: value => Config.Hotkey = value
            );
        }

        private void OnSaveLoaded(object? sender, EventArgs e)
        {
            if (!Context.IsWorldReady) return;
            // Let's check to see if the modData needs to initialize
            Utils.InitializeModData(Game1.player);
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            // Dump any flutes that might be hanging around
            Farmer who = Game1.player;
            Utils.DumpUnnecessaryFlutes(who);
        }

        private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            // Dump any flutes that might be hanging around
            Farmer who = Game1.player;
            Utils.DumpUnnecessaryFlutes(who);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // If the mod's disabled, no need to run checks on this event
            if (!Config.Enabled) return;

            // If the hotkey is pressed... 
            Farmer who = Game1.player;
            if (Config.Hotkey.JustPressed())
            {
                // ...and the player has the power...
                if (Utils.IsPowerUnlocked(who))
                {
                    // ...summon the horse
                    Utils.SummonHorse(who);
                }
            }
        }

        /// <inheritdoc cref="TriggerActionDelegate" />
        public static bool HandleUnlockAction(string[] args, TriggerActionContext context, out string error)
        {
            error = string.Empty;
            // Unlock the power for the player
            Farmer who = Game1.player;
            Utils.DoPowerUnlock(who);
            return true;
        }
    }
}