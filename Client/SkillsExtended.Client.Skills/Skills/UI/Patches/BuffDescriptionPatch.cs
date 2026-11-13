using System.Reflection;
using System.Text.RegularExpressions;
using EFT;
using EFT.UI;
using HarmonyLib;
using SkillsExtended.Skills.Core;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.UI.Patches;

/// <summary>
///     Replaces the hard-coded elite repair-wear value in the buff description with
///     the configured percentage.
/// </summary>
internal class BuffDescriptionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(BuffIcon), nameof(BuffIcon.GetBuffDescription));

    [PatchPrefix]
    public static bool Prefix(SkillManager.Buff buff, ref string __result)
    {
        if (!NativeRepairWear.TryGetBuffField(buff.Id, out var field)) return true;

        var value = NativeRepairWear.GetValue(field);
        var text = buff.Id.ToString().Localized();
        text = Regex.Replace(
            text,
            @"\[[^\]]*\]",
            "[" + NativeRepairWear.Format(value) + "%]"
        );
        text = text.Replace("[", "<color=#54c1ff>").Replace("]", "</color>");
        __result = text.Contains("{0") ? string.Format(text, buff.ValueObj) : text;
        return false;
    }
}
