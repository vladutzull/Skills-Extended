using System.Reflection;
using System.Text.RegularExpressions;
using EFT;
using EFT.UI;
using HarmonyLib;
using SkillsExtended.Skills.Core;
using SkillsExtended.Skills.SkillClasses.Physical;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.UI.Patches;

/// <summary>
///     Shows the configured values in buff descriptions that the game stores without a
///     numeric placeholder: the elite repair-wear chance and the Health damage absorption.
/// </summary>
internal class BuffDescriptionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(BuffIcon), nameof(BuffIcon.GetBuffDescription));

    [PatchPrefix]
    public static bool Prefix(SkillManager.Buff buff, ref string __result)
    {
        if (buff.Id == EBuffId.HealthEliteAbsorbDamage)
        {
            if (!HealthEliteAbsorb.TryGetPercent(out var absorb)) return true;

            var absorbText = buff.Id.ToString().Localized()
                + " [" + HealthEliteAbsorb.Format(absorb) + "%]";
            __result = absorbText.Replace("[", "<color=#54c1ff>").Replace("]", "</color>");
            return false;
        }

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
