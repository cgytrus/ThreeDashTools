using JetBrains.Annotations;

using SixDash.API;
using SixDash.Patches;

namespace ThreeDashTools.Patches.PlayerSection;

[UsedImplicitly]
public class NoDeath : ConfigurablePatch {
    private bool _defaultNoDeath;

    protected override bool enabled {
        get => base.enabled;
        set {
            base.enabled = value;
            if(!Player.scriptInstance)
                return;
            Player.scriptInstance!.noDeath = value || _defaultNoDeath;
        }
    }

    public NoDeath() : base(Plugin.instance!.Config, "Player", nameof(NoDeath), false, "") { }

    public override void Apply() {
        Player.spawn += self => {
            _defaultNoDeath = self.noDeath;
            if(!enabled)
                return;
            self.noDeath = true;
        };
    }
}
