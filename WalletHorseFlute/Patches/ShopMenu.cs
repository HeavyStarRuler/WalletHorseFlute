using System;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;
using WalletHorseFlute.Helpers;

namespace WalletHorseFlute.Patches
{
    public static class ShopMenuPatches
    {
        internal static IModHelper Helper { get; private set; } = null!;
        const string horseFluteID = "(O)911";
        const string powerID = "HorseFlute";
        
        // This targets the ShopMenu constructor to strip the item from stock
        [HarmonyPatch(typeof(ShopMenu), MethodType.Constructor, new Type[] { typeof(string), typeof(bool) })]
        [HarmonyPostfix]
        public static void Constructor_Postfix(ShopMenu __instance)
        {
            Farmer who = Game1.player;

            // Only act if it's Qi's Shop and the player has the power
            if (__instance.ShopId == "QiGemShop" && Utils.IsPowerUnlocked(who, powerID))
            {
                // Find the flute in the shop's forSale list
                var flute = __instance.forSale.FirstOrDefault(i => i?.QualifiedItemId == horseFluteID);
                if (flute != null)
                {
                    __instance.forSale.Remove(flute);
                    __instance.itemPriceAndStock.Remove(flute);
                }
            }
        }

        [HarmonyPatch(typeof(ShopMenu), "tryToPurchaseItem")]
        [HarmonyPrefix]
        public static bool Purchase_Prefix(ShopMenu __instance, ISalable item, Item stockItem, int numberToBuy, ref bool __result)
        {
            if (item?.QualifiedItemId == horseFluteID)
            {
                Farmer who = Game1.player;
            
                // Manually check the price from the shop's stock
                if (__instance.itemPriceAndStock.TryGetValue(item, out var stock))
                {
                    int totalPrice = stock.Price * numberToBuy;

                    if (who.QiGems >= totalPrice)
                    {
                        // Deduct the Qi Gems from the player
                        who.QiGems -= totalPrice;

                        // Unlock the power for the player
                        Utils.DoPowerUnlock(who, powerID, null);

                        __result = true; // Tell the game it was a success
                        return false; // Don't let the game add a physical item to the backpack
                    }
                    else
                    {
                        Game1.playSound("cancel"); // Play a sound to indicate the purchase failed
                        return false; // Don't let the game process the purchase
                    }
                }
            }
            return true;
        }
    }
}