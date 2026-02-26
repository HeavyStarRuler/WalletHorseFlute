using System;
using StardewValley;
using StardewModdingAPI;

namespace WalletHorseFlute.Helpers;

public class Utils
{
    static readonly string powerPrefix = "HeavyStarRuler.WalletHorseFlute_";
    static readonly string powerUnlockedSuffix = "_IsUnlocked";
    static readonly string horseFluteID = "(O)911";

    internal static IModHelper Helper { get; private set; } = null!;

    public static bool IsPowerUnlocked(Farmer who, string powerID)
    {
        // If the modData key doesn't exist, the power is definitely not unlocked
        if (!who.modData.ContainsKey(powerPrefix + powerID + powerUnlockedSuffix))
            return false;

        // If the modData key exists, then just return if the value is 'true' or not
        return who.modData[powerPrefix + powerID + powerUnlockedSuffix] == "true";

        // [TODO] Shouldn't need this, so we'll delete it when we're sure
        // If the modData key exists but the conditions aren't met, the power is also not unlocked
        // var powerData = DataLoader.Powers(Game1.content)[powerPrefix + powerID];
        // return GameStateQuery.CheckConditions(powerData.UnlockedCondition, who.currentLocation, who);
    }

    public static void DoPowerUnlock(Farmer who, string powerID, Item? flute = null)
    {
        // Check if the player already has the power
        if (IsPowerUnlocked(who, powerID))
            return;

        // Set a custom player stat to indicate that the player has the power
        who.modData[powerPrefix + powerID + powerUnlockedSuffix] = "true";

        // If there's a flute, remove it from the player's inventory
        if (flute != null) who.removeItemFromInventory(flute);

        // Close any menus so the player is visible for the animation
        Game1.exitActiveMenu();

        // Trigger the "Hold Up Item" pose
        who.holdUpItemThenMessage(flute);

        // [TODO] Revisit if necessary
        // Clear the cache so the Special Items tab updates
        // Helper.GameContent.InvalidateCache("Data/Powers");
    }

    public static void SummonHorse(Farmer who)
    {
        // If the player doesn't have the power, do nothing
        if (!IsPowerUnlocked(who, "HorseFlute"))
            return;

        Item fluteItem = ItemRegistry.Create(horseFluteID);

        if (fluteItem is StardewValley.Object fluteObject)
        {
            bool isSuccessful = fluteObject.performUseAction(Game1.currentLocation);
            if (!isSuccessful) Log.Warn(I18n.Log_SummonFailed());
        }
    }

    public static void DumpUnnecessaryFlutes(Farmer who, string powerID, string fluteID)
    {
        // If the player has any flutes in their inventory, let's get rid of them
        var flute = who.Items.FirstOrDefault(i => i?.QualifiedItemId == fluteID);
        if (flute != null)
        {
            // If the player doesn't have the power, give it to them
            if (!IsPowerUnlocked(who, powerID))
                DoPowerUnlock(who, powerID, flute);

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
        Item flute = ItemRegistry.Create("(O)911");
        who.addItemByMenuIfNecessary(flute);

        // Remove the modData flag
        who.modData.Remove(powerPrefix + "HorseFlute" + powerUnlockedSuffix);

        // [TODO] Revisit if necessary
        // Clear the cache so the Special Items tab updates
        // Helper.GameContent.InvalidateCache("Data/Powers");

        // Log an info message so the user knows what happened
        Log.Info(I18n.Log_ModDisabledRevert());
    }
}