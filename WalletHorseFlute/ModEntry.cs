using System;
using Microsoft.Xna.Framework;
using ContentPatcher;
using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using WalletHorseFlute.Utils;

namespace WalletHorseFlute
{
    internal sealed class ModEntry : Mod
    {
        internal static IMonitor ModMonitor { get; private set; } = null!;
        internal static IModHelper ModHelper { get; private set; } = null!;
        internal static ModConfig Config { get; private set; } = null!;
        internal static IContentPatcherAPI ContentPatcher { get; private set; } = null!;

        const int HorseFluteID = 911;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            ModMonitor = Monitor;
            ModHelper = helper;

            KeybindList hotkey = Config.Hotkey;

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // ContentPatcher = Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
            // if (ContentPatcher == null)
            // {
            //     Log.Error("ContentPatcher not found. Wallet Horse Flute requires ContentPatcher to function.");
            //     return;
            // }

            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu != null) Config.SetupConfig(configMenu, ModManifest, Helper);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (Config.Hotkey.JustPressed())
            {
                Log.Info("Hotkey pressed, attempting to use Horse Flute...");
            }
        }
    }
}