using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace WalletHorseFlute;

public sealed class ModConfig
{
    public bool Enabled { get; set; } = true;
    public KeybindList Hotkey { get; set; } = KeybindList.Parse("Q");
}