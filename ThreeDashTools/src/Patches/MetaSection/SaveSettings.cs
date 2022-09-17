using SixDash.Patches;

using UnityEngine;

namespace ThreeDashTools.Patches.MetaSection;

// ReSharper disable once UnusedType.Global
public class SaveSettings : ConfigurablePatch {
    public SaveSettings() : base(Plugin.instance!.Config, "Meta", nameof(SaveSettings), false,
        "Saves settings set in the pause menu") { }

    public override void Apply() {
        On.PauseMenuManager.Resume += (orig, self, music) => {
            Save();
            orig(self, music);
        };
        On.PauseMenuManager.QuitToHub += (orig, self) => {
            Save();
            orig(self);
        };
        On.PauseMenuManager.QuitToMenu += (orig, self) => {
            Save();
            orig(self);
        };

        On.TitleCamera.Start += (orig, self) => {
            Load();
            orig(self);
        };
    }

    private static void Save() {
        PlayerPrefs.SetInt("showPath", PauseMenuManager.pathOn ? 1 : 0);
        PlayerPrefs.SetFloat("volume", AudioListener.volume);
    }

    private static void Load() {
        PauseMenuManager.pathOn = PlayerPrefs.GetInt("showPath", 0) > 0;
        AudioListener.volume = PlayerPrefs.GetFloat("volume", 1f);
    }
}
