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
///     numeric placeholder: the elite repair-wear chance, the Health damage absorption and
///     the Health body part health bonus.
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
            var hasAbsorb = HealthElite.TryGetAbsorbPercent(out var absorb);
            var hasHealth = HealthElite.TryGetMaxHealthPercent(out var maxHealth);
            if (!hasAbsorb && !hasHealth) return true;

            var description = buff.Id.ToString().Localized();
            if (hasAbsorb)
            {
                description = Regex.Replace(
                    description,
                    @"\[[^\]]*\]",
                    "[" + HealthElite.Format(absorb) + "%]"
                );
            }
            description = description.Replace("[", "<color=#54c1ff>").Replace("]", "</color>");

            if (hasHealth)
            {
                description += "\n<color=#54c1ff>"
                    + LocalizedText.Get(
                        "SkillsExtended.HealthEliteMaxHealth",
                        HealthElite.Format(maxHealth)
                    )
                    + "</color>";
            }

            __result = description;
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
