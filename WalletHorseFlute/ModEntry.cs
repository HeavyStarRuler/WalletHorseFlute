using ContentPatcher;
using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using WalletHorseFlute.Helpers;

namespace WalletHorseFlute
{
    internal sealed class ModEntry : Mod
    {
        internal static IMonitor ModMonitor { get; private set; } = null!;
        internal static IModHelper ModHelper { get; private set; } = null!;
        internal static ModConfig Config { get; private set; } = null!;

        const string horseFluteID = "(O)911";
        const string powerID = "HorseFlute";

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            ModHelper = helper;
            Config = helper.ReadConfig<ModConfig>();
            
            I18n.Init(helper.Translation);
            
            var harmony = new Harmony(this.ModManifest.UniqueID);

            try
            {
                harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Log.Error(I18n.Log_HarmonyPatchesNotApplied(ex));
            }

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Player.InventoryChanged += this.OnInventoryChanged;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;

            // [DEBUG TOOL] Remove when done testing
            helper.ConsoleCommands.Add("toggleflute", "Toggles the Horse Flute power on/off for the local player.\n\nUsage: toggle_flute", (command, args) =>
            {
                if (!Context.IsWorldReady)
                {
                    Log.Error("You must load a save first!");
                    return;
                }

                Farmer who = Game1.player;
                string key = "HeavyStarRuler.WalletHorseFlute_HorseFlute_IsUnlocked";

                if (who.modData.ContainsKey(key))
                {
                    who.modData.Remove(key);
                    Log.Info("Horse Flute power REMOVED.");
                }
                else
                {
                    who.modData[key] = "true";
                    Log.Info("Horse Flute power ADDED.");
                }

                // [TODO] Might not ever need something like this
                // Refresh the UI/Data/Powers cache immediately
                // Helper.GameContent.InvalidateCache("Data/Powers");
            });
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var ContentPatcher = ModHelper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
            if (ContentPatcher == null)
            {
                Log.Error("ContentPatcher not found. Button's Extra Books requires ContentPatcher to function.");
                return;
            }

            ContentPatcher.RegisterToken(ModManifest, "HorseFlute_IsUnlocked", () =>
            {
                if (!Context.IsWorldReady) return null;
                return new[] { Utils.IsPowerUnlocked(Game1.player, powerID) ? "true" : "false" };
            });

            var configMenu = ModHelper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null) return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => 
                {
                    // Save the new settings to the config.json file
                    ModHelper.WriteConfig(Config);

                    // TRIGGER: If mod is toggled OFF but player still has the power
                    if (Context.IsWorldReady && !Config.Enabled && Utils.IsPowerUnlocked(Game1.player, powerID))
                        Utils.RevertModChanges(Game1.player);
                }
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enabled",
                tooltip: () => "If disabled, the power is removed and the physical flute is returned to your inventory.",
                getValue: () => Config.Enabled,
                setValue: value => Config.Enabled = value
            );

            configMenu.AddKeybindList(
                mod: ModManifest,
                name: () => "Summon Hotkey",
                tooltip: () => "The key used to whistle for your horse.",
                getValue: () => Config.Hotkey,
                setValue: value => Config.Hotkey = value
            );
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Dump any flutes that might be hanging around
            Farmer who = Game1.player;
            Utils.DumpUnnecessaryFlutes(who, powerID, horseFluteID);
        }

        private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // Dump any flutes that might be hanging around
            Farmer who = Game1.player;
            Utils.DumpUnnecessaryFlutes(who, powerID, horseFluteID);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            Farmer who = Game1.player;
            // [TODO] Remove when done debugging
            if (Config.Hotkey.JustPressed())
                Log.Info($"Hotkey pressed & {who.Name} {(Utils.IsPowerUnlocked(who, powerID) ? "has" : "does NOT have")} the required power");
            if (Config.Hotkey.JustPressed() && Utils.IsPowerUnlocked(who, powerID)) Utils.SummonHorse(who);
        }
    }
}