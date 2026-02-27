using System;
using StardewValley;
using StardewModdingAPI;

namespace WalletHorseFlute.Helpers;

public class Utils
{
    public const string PowerID = "HorseFlute";
    public const string HorseFluteID = "(O)911";
    public const string PowerPrefix = "HeavyStarRuler.WalletHorseFlute_";
    public const string PowerUnlockedSuffix = "_IsUnlocked";

    internal static IModHelper ModHelper { get; set; } = null!;

    public static void InitializeModData(Farmer who)
    {
        // Only set it to false if the key is completely missing
        string key = PowerPrefix + PowerID + PowerUnlockedSuffix;
        if (!who.modData.ContainsKey(key)) who.modData[key] = "false";
    }

    public static bool IsPowerUnlocked(Farmer who)
    {
        // Return whether the modData key exists and what its value is
        string key = PowerPrefix + PowerID + PowerUnlockedSuffix;
        return who.modData.ContainsKey(key) && who.modData[key] == "true";
    }

    public static void DoPowerUnlock(Farmer who)
    {
        // Leave if the player already has the power, or the mod is disabled
        if (IsPowerUnlocked(who)) return;

        // Set a custom player stat to indicate that the player has the power
        who.modData[PowerPrefix + PowerID + PowerUnlockedSuffix] = "true";

        // Close any menus so the player is visible for the animation
        Game1.exitActiveMenu();

        // Create a temporary item instance for the pose
        Item fluteAnimationItem = ItemRegistry.Create(HorseFluteID);

        // Trigger the "Hold Up Item" pose
        who.holdUpItemThenMessage(fluteAnimationItem);
    }

    public static void SummonHorse(Farmer who)
    {
        // If the player doesn't have the power, do nothing
        if (!ModEntry.Config.Enabled || !IsPowerUnlocked(who))
            return;

        Item fluteItem = ItemRegistry.Create(HorseFluteID);

        if (fluteItem is StardewValley.Object fluteObject)
        {
            bool isSuccessful = fluteObject.performUseAction(Game1.currentLocation);
            if (!isSuccessful) Log.Warn(I18n.Log_SummonFailed());
        }
    }

    public static void DumpUnnecessaryFlutes(Farmer who)
    {
        // If mod is disabled, jump out of here
        if (!ModEntry.Config.Enabled) return;

        // If the player has any flutes in their inventory, let's get rid of them
        var flute = who.Items.FirstOrDefault(i => i?.QualifiedItemId == HorseFluteID);
        if (flute != null)
        {
            // If the player doesn't have the power, give it to them
            if (!IsPowerUnlocked(who))
                DoPowerUnlock(who);

            // Remove the flute from the player's inventory
            who.removeItemFromInventory(flute);
        }
    }

    // This is a special method that gives the player their flute back if
    // they disable the mod after unlocking the power considering
    // unlocking the power removes the flute from the player's inventory
    public static void RevertModChanges(Farmer who)
    {
        // Give the physical item back
        Item flute = ItemRegistry.Create(HorseFluteID);
        who.addItemByMenuIfNecessary(flute);

        // Falsify the modData flag
        who.modData[PowerPrefix + PowerID + PowerUnlockedSuffix] = "false";

        // Clear the cache so the Special Items tab updates
        ModHelper.GameContent.InvalidateCache("Data/Powers");
        ModHelper.GameContent.InvalidateCache("Data/Shops");

        // Log an info message so the user knows what happened
        Log.Info(I18n.Log_ModDisabledRevert());
    }
}