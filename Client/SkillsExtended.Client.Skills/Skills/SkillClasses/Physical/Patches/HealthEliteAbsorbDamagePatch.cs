using System.Reflection;
using EFT.Ballistics;
using EFT.HealthSystem;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.SkillClasses.Physical.Patches;

/// <summary>
///     Applies the Health elite "Damage absorption" perk, which the 4.1 client declares but
///     never uses. Damage is reduced after armor and before it reaches the body part.
/// </summary>
internal class HealthEliteAbsorbDamagePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.ApplyDamage));

    [PatchPrefix]
    public static void Prefix(ActiveHealthController __instance, DamageInfo damageInfo, ref float damage)
    {
        if (!HealthEliteAbsorb.TryGetPercent(out var percent)) return;
        if (!HealthEliteAbsorb.IsElite(__instance.Player?.Skills)) return;
        if (damage <= 0f) return;

        // Bleeding, dehydration, poison and other self-inflicted damage keep their full rate.
        if (DamageHelper.IsSelfInflicted(damageInfo.DamageType)) return;

        damage *= 1f - percent / HealthEliteAbsorb.MaxPercent;
    }
}
