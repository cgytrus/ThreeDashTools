using SixDash.Patches;

namespace ThreeDashTools.Patches.PlayerSection;

// ReSharper disable once UnusedType.Global
public class NoDeath : ConfigurablePatch {
    private PlayerScript? _lastPlayer;
    private bool _defaultNoDeath;

    protected override bool enabled {
        get => base.enabled;
        set {
            base.enabled = value;
            if(!_lastPlayer)
                return;
            _lastPlayer!.noDeath = value || _defaultNoDeath;
        }
    }

    public NoDeath() : base(Plugin.instance!.Config, nameof(NoDeath), null, false, "") { }

    public override void Apply() {
        SixDash.API.Player.playerSpawn += self => {
            _lastPlayer = self;
            _defaultNoDeath = self.noDeath;
            if(!enabled)
                return;
            self.noDeath = true;
        };
    }
}
