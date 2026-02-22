using ContentPatcher;
using GenericModConfigMenu;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using WalletHorseFlute.Utils;
using WalletHorseFlute.Patches;

namespace WalletHorseFlute
{
    internal sealed class ModEntry : Mod
    {
        internal static IMonitor ModMonitor { get; private set; } = null!;
        internal static IModHelper ModHelper { get; private set; } = null!;
        internal static ModConfig Config { get; private set; } = null!;

        const string horseFluteID = "(O)911";
        const string dataKey = "HeavyStarRuler.WalletHorseFlute_HorseFlute_Unlocked";

        private static bool playerHasPower = false;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            ModHelper = helper;
            Config = helper.ReadConfig<ModConfig>();
            
            I18n.Init(helper.Translation);

            var harmony = new Harmony(this.ModManifest.UniqueID);
            ShopMenuPatches.Initialize(this.Monitor);

            try
            {
                var constructors = typeof(ShopMenu).GetConstructors();

                foreach (var ctor in constructors)
                {
                    harmony.Patch(ctor, postfix: new HarmonyMethod(typeof(ShopMenuPatches), nameof(ShopMenuPatches.Postfix)));
                }

                Log.Info(I18n.Log_ShopMenuConstructorsPatched(constructors.Length));
            }
            catch (Exception ex)
            {
                Log.Error(I18n.Log_ShopMenuConstructorsNotPatched(new { ex }));
            }

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Player.InventoryChanged += this.OnInventoryChanged;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            if (Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher") == null)
            {
                Log.Error(I18n.Log_ContentPatcherRequired());
                return;
            }

            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu != null) Config.SetupConfig(configMenu, ModManifest, Helper);
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // Check if the player has the Horse Flute power and store it in a static variable for quick access
            playerHasPower = Game1.player.modData.ContainsKey(dataKey) && Game1.player.modData[dataKey] == "true";
            Log.Info($"Player has power: {playerHasPower}");
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Check if the player has the Horse Flute in their inventory and give them the power if they do
            var flute = Game1.player.Items.FirstOrDefault(i => i?.QualifiedItemId == horseFluteID);
            if (flute != null)
            {
                // Set a custom player stat to indicate that the player has the power
                Game1.player.modData[dataKey] = "true";

                // Remove the flute from the player's inventory
                Game1.player.removeItemFromInventory(flute);

                // Update the static variable to indicate that the player now has the power
                playerHasPower = true;
            }
        }

        private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // Check if the player has the Horse Flute in their inventory and give them the power if they do
            var flute = Game1.player.Items.FirstOrDefault(i => i?.QualifiedItemId == horseFluteID);
            if (flute != null && !playerHasPower)
            {
                // Set a custom player stat to indicate that the player has the power
                Game1.player.modData[dataKey] = "true";

                // Remove the flute from the player's inventory
                Game1.player.removeItemFromInventory(flute);

                // Show a message and play a sound to indicate the power has been acquired
                Game1.addHUDMessage(new HUDMessage(I18n.HUD_HorseFluteAquired(), HUDMessage.newQuest_type));
                Game1.playSound("discoverMineral");
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (Config.Hotkey.JustPressed() && playerHasPower) SummonHorse();
        }

        private void SummonHorse()
        {
            Item fluteItem = ItemRegistry.Create(horseFluteID);

            if (fluteItem is StardewValley.Object fluteObject)
            {
                bool isSuccessful = fluteObject.performUseAction(Game1.currentLocation);
                if (!isSuccessful) Log.Warn(I18n.Log_SummonFailed());
            }
        }
    }
}