using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace WalletHorseFlute;

public sealed class ModConfig
{
    public KeybindList Hotkey { get; set; } = KeybindList.Parse("Q");

    public ModConfig()
    {
        Init();
    }

    private void Init()
    {
        Hotkey = KeybindList.Parse("Q");
    }

    public void SetupConfig(IGenericModConfigMenuApi configMenu, IManifest ModManifest, IModHelper Helper)
    {
        configMenu.Register(
            mod: ModManifest,
            reset: Init,
            save: () => Helper.WriteConfig(this)
        );
    }
}