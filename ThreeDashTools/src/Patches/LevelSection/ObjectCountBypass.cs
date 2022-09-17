using JetBrains.Annotations;

using SixDash.Patches;

namespace ThreeDashTools.Patches.LevelSection;

[UsedImplicitly]
public class ObjectCountBypass : ConfigurablePatch {
    private bool _tempEnabled;

    public ObjectCountBypass() : base(Plugin.instance!.Config, "Level", nameof(ObjectCountBypass), false, "") { }

    public override void Apply() {
        On.FlatEditor.GetTotalItems += (orig, self) => enabled && _tempEnabled ? 0 : orig(self);
        On.FlatEditor.UpdateUI += (orig, self) => {
            orig(self);
            _tempEnabled = true;
        };
        On.FlatEditor.Update += (orig, self) => {
            orig(self);
            _tempEnabled = false;
        };
    }
}
