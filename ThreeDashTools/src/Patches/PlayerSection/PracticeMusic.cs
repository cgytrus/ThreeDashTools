using SixDash.Patches;

using UnityEngine;

namespace ThreeDashTools.Patches.PlayerSection;

// ReSharper disable once UnusedType.Global
public class PracticeMusic : ConfigurablePatch {
    public PracticeMusic() : base(Plugin.instance!.Config, "Player", nameof(PracticeMusic), false,
        "Plays level music in practice mode") { }

    public override void Apply() {
        On.EternalMusic.Awake += (orig, self) => {
            if(enabled)
                Object.Destroy(self.gameObject);
            else
                orig(self);
        };
        On.EternalMusic.PlayMusic += (orig, self) => { if(!enabled) orig(self); };
        On.EternalMusic.StopMusic += (orig, self) => { if(!enabled) orig(self); };
        On.PauseMenuManager.StopAllMusic += (orig, self) => { if(!enabled) orig(self); };
        On.PauseMenuManager.Resume += (orig, self, resumeMusic) => orig(self, enabled || resumeMusic);

        SixDash.API.Player.playerSpawn += _ => {
            if(!enabled)
                return;
            GameObject? recentCheckpoint = PauseMenuManager.inPracticeMode ? PlayerScript.GetRecentCheckpoint() : null;
            CheckpointScript? checkpoint = recentCheckpoint ? recentCheckpoint!.GetComponent<CheckpointScript>() : null;
            GameObject.FindGameObjectWithTag("Music").GetComponent<AudioSource>().time =
                checkpoint ? checkpoint!.savedMusicPos : 0f;
        };
    }
}
