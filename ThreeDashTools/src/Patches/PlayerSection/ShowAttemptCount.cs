using System.Collections.Generic;

using JetBrains.Annotations;

using SixDash;
using SixDash.API;
using SixDash.Patches;

using TMPro;

using UnityEngine;

namespace ThreeDashTools.Patches.PlayerSection;

[UsedImplicitly]
public class ShowAttemptCount : ConfigurablePatch {
    private readonly List<(TMP_Text, Transform)> _texts = new();
    private int _attemptCount;
    private float _distance;
    private float _outAnimationEnd;

    public ShowAttemptCount() : base(Plugin.instance!.Config, "Player", nameof(ShowAttemptCount), false, "") { }

    public override void Apply() {
        Player.spawn += self => {
            _attemptCount++;

            if(!enabled) {
                foreach((_, Transform trans) in _texts)
                    Object.Destroy(trans.gameObject);
                _texts.Clear();
                return;
            }

            UpdateTexts(self.transform);
        };

        World.levelUnload += () => {
            _texts.Clear();
            _attemptCount = 0;
        };

        World.levelUpdate += (renderMin, renderMax) => {
            if(!enabled)
                return;
            Vector3 scale = Chunk.ScaleOut(renderMin, _outAnimationEnd, _distance);
            foreach((_, Transform transform) in _texts)
                transform.localScale = scale;
        };
    }

    private void UpdateTexts(Transform transform) {
        // failsafe
        for(int i = _texts.Count - 1; i >= 0; i--) {
            Transform trans = _texts[i].Item2;
            if(_texts[i].Item1 && trans)
                continue;
            if(trans && trans.gameObject)
                Object.Destroy(trans.gameObject);
            _texts.RemoveAt(i);
        }

        while(_texts.Count < 2)
            _texts.Add(CreateText());

        _distance = PathFollower.distanceTravelled;
        _outAnimationEnd = World.TimeToDistance(World.DistanceToTime(_distance) + Chunk.OutAnimTime);

        Vector3 position = transform.position + new Vector3(0f, 3f);
        Quaternion rotation = transform.rotation;

        foreach((TMP_Text text, Transform transform) text in _texts) {
            text.transform.position = position;
            text.transform.rotation = rotation;
            rotation *= Quaternion.Euler(0f, 180f, 0f);
            text.text.text = $"Attempt {_attemptCount.ToString()}";
        }
    }

    private static (TMP_Text, Transform) CreateText() {
        GameObject obj = new("Attempt count");
        obj.AddComponent<MeshRenderer>();

        TextMeshPro text = obj.AddComponent<TextMeshPro>();
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 12;
        text.font = UI.fontAsset;
        text.fontSharedMaterial = UI.fontMaterial;
        text.fontStyle = UI.FontStyle;
        text.outlineColor = new Color32(0, 0, 0, 255);
        text.outlineWidth = UI.FontOutlineWidth;
        text.enableCulling = true;

        return (text, obj.transform);
    }
}
