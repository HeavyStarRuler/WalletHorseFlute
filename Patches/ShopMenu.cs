using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Linq;
using WalletHorseFlute.Utils;

namespace WalletHorseFlute.Patches
{
    internal class ShopMenuPatches
    {
        internal static IModHelper Helper { get; private set; } = null!;

        private static IMonitor Monitor;

        const string horseFluteID = "(O)911";
        const string dataKey = "HeavyStarRuler.WalletHorseFlute_HorseFlute_Unlocked";

        internal static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }

        // To avoid wasted gems, we'll wait for the original stock to load,
        // then quickly clean it up before the player sees the shop menu.
        public static void Postfix(ShopMenu __instance)
        {
            try
            {
                if (__instance.ShopId == "QiGemShop")
                {
                    // Check to see if player has the power.
                    // If they do, remove the flute from the shop stock and prevent it from appearing in the menu.
                    if (Game1.player.modData.ContainsKey(dataKey) && Game1.player.modData[dataKey] == "true")
                    {
                        var itemToRemove = __instance.forSale.FirstOrDefault(i => i?.QualifiedItemId == horseFluteID);

                        if (itemToRemove != null)
                        {
                            __instance.forSale.Remove(itemToRemove);
                            __instance.itemPriceAndStock.Remove(itemToRemove);
                            Log.Debug(I18n.Log_RemovedFromShop());
                        }
                    }
                }
            } catch (System.Exception ex)
            {
                Log.Error(I18n.Log_ShopMenuPatchError(new { ex }));
            }
        }
    }
}