using BepInEx.Configuration;

using JetBrains.Annotations;

using SixDash.API;
using SixDash.Patches;

namespace ThreeDashTools.Patches.PlayerSection;

[UsedImplicitly]
public class RespawnTime : IPatch {
    private float _normal;
    private float _practice;

    public RespawnTime() {
        ConfigFile config = Plugin.instance!.Config;

        const float def = Player.VanillaRespawnTime;
        ConfigDescription description = new("", new AcceptableValueRange<float>(0f, float.PositiveInfinity),
            new ConfigurationManagerAttributes { CustomDrawer = GuiUtil.SliderDrawer(0f, 5f, "F1") });

        ConfigEntry<float> normal = config.Bind("Player", $"Normal{nameof(RespawnTime)}", def, description);
        _normal = normal.Value;
        normal.SettingChanged += (_, _) => { _normal = normal.Value; };

        ConfigEntry<float> practice = config.Bind("Player", $"Practice{nameof(RespawnTime)}", def, description);
        _practice = practice.Value;
        practice.SettingChanged += (_, _) => { _practice = practice.Value; };
    }

    public void Apply() => Player.respawnTime += orig => orig * (PauseMenuManager.inPracticeMode ? _practice : _normal);
}
