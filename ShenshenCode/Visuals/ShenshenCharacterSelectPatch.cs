using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace Shenshen.ShenshenCode.Visuals;

/// <summary>The native selection panel assumes a starter relic at index zero.</summary>
[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
internal static class ShenshenCharacterSelectPatch
{
    private static bool HasStarterToDisplay(CharacterModel character) =>
        character is not Character.Shenshen || character.StartingRelics.Count != 0;

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var code = instructions.ToList();
        var getter = AccessTools.PropertyGetter(typeof(CharacterModel), nameof(CharacterModel.StartingRelics));
        var setTint = AccessTools.PropertySetter(typeof(CanvasItem), nameof(CanvasItem.SelfModulate));
        var guard = AccessTools.Method(typeof(ShenshenCharacterSelectPatch), nameof(HasStarterToDisplay));

        for (var i = code.Count - 1; i >= 0; i--)
        {
            if (!code[i].Calls(getter)) continue;

            // Both unlocked and locked displays end by tinting the two relic icons.
            var end = i + 1;
            var tintWrites = 0;
            while (end < code.Count && tintWrites != 2)
            {
                if (code[end].Calls(setTint)) tintWrites++;
                end++;
            }
            if (tintWrites != 2 || end == code.Count)
                throw new InvalidOperationException("Native starting relic display layout changed.");

            var skip = generator.DefineLabel();
            code[end].labels.Add(skip);
            var show = generator.DefineLabel();
            var entry = new CodeInstruction(OpCodes.Dup);
            entry.labels.AddRange(code[i].labels);
            code[i].labels.Clear();
            code[i].labels.Add(show);
            code.InsertRange(i,
            [
                entry,
                new CodeInstruction(OpCodes.Call, guard),
                new CodeInstruction(OpCodes.Brtrue, show),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Br, skip)
            ]);
        }
        return code;
    }

    [HarmonyPriority(Priority.Last)]
    private static void Postfix(CharacterModel characterModel, RichTextLabel ____relicTitle,
        RichTextLabel ____relicDescription, TextureRect ____relicIcon, TextureRect ____relicIconOutline)
    {
        if (HasStarterToDisplay(characterModel)) return;
        ____relicTitle.Text = string.Empty;
        ____relicDescription.Text = string.Empty;
        ____relicIcon.Texture = null;
        ____relicIconOutline.Texture = null;
        ____relicIcon.SelfModulate = new Color(0, 0, 0, 0);
        ____relicIconOutline.SelfModulate = new Color(0, 0, 0, 0);
    }
}
