using BepInEx.Configuration;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;

using MonoMod.Cil;

using SixDash.Patches;

using UnityEngine;

namespace ThreeDashTools.Patches;

[UsedImplicitly]
public class Keybinds : IPatch {
    private ConfigEntry<KeyboardShortcut> _placeCheckpoint;
    private ConfigEntry<KeyboardShortcut> _removeCheckpoint;
    private ConfigEntry<KeyboardShortcut> _respawn;

    public Keybinds() {
        ConfigFile config = Plugin.instance!.Config;
        _placeCheckpoint = config.Bind("Keybinds", "PlaceCheckpoint", new KeyboardShortcut(KeyCode.Z), "");
        _removeCheckpoint = config.Bind("Keybinds", "RemoveCheckpoint", new KeyboardShortcut(KeyCode.X), "");
        _respawn = config.Bind("Keybinds", "Respawn", new KeyboardShortcut(KeyCode.Backspace), "");
    }

    public void Apply() {
        IL.PlayerScript.ManagePracticeMode += il => {
            ILCursor cursor = new(il);
            cursor.GotoNext(code => code.MatchLdcI4((sbyte)KeyCode.Z));
            cursor.RemoveRange(2);
            EmitIsDown(cursor, nameof(_placeCheckpoint));
        };

        IL.PauseMenuManager.Update += il => {
            ILCursor cursor = new(il);
            cursor.GotoNext(code => code.MatchLdcI4((sbyte)KeyCode.X));
            cursor.RemoveRange(2);
            EmitIsDown(cursor, nameof(_removeCheckpoint));
        };

        IL.PlayerScript.Update += il => {
            ILCursor cursor = new(il);
            cursor.GotoNext(code => code.MatchLdcI4((sbyte)KeyCode.Backspace));
            cursor.RemoveRange(2);
            EmitIsDown(cursor, nameof(_respawn));
        };
    }

    private void EmitIsDown(ILCursor cursor, string entry) {
        VariableDefinition shortcut = new(cursor.Context.Import(typeof(KeyboardShortcut)));
        cursor.Body.Variables.Add(shortcut);

        cursor.EmitReference(this);
        cursor.Emit<Keybinds>(OpCodes.Ldfld, entry);
        cursor.Emit<ConfigEntry<KeyboardShortcut>>(OpCodes.Callvirt, "get_Value");
        cursor.Emit(OpCodes.Stloc, shortcut);
        cursor.Emit(OpCodes.Ldloca, shortcut);
        cursor.Emit<KeyboardShortcut>(OpCodes.Call, nameof(KeyboardShortcut.IsDown));
    }
}
