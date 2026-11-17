using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.SkillClasses.Physical.Patches;

/// <summary>
///     Applies the Health elite body part health bonus by scaling the in-raid maximums. The
///     profile keeps its unmodified values, so the bonus is re-applied on every raid start and
///     never stacks across raids.
/// </summary>
internal class HealthEliteMaxHealthPatch : ModulePatch
{
    /// <summary>Unmodified maximum per body part, kept per controller for the profile restore.</summary>
    private static readonly ConditionalWeakTable<ActiveHealthController, Dictionary<EBodyPart, float>>
        Baselines = new();

    protected override MethodBase GetTargetMethod() =>
        AccessTools.Constructor(
            typeof(ActiveHealthController),
            [typeof(Player), typeof(Profile.HealthInfo), typeof(InventoryController),
                typeof(SkillManager)]
        );

    [PatchPostfix]
    public static void Postfix(ActiveHealthController __instance)
    {
        if (!HealthElite.TryGetMaxHealthPercent(out var percent)) return;
        if (!HealthElite.IsElite(__instance._skills)) return;

        var baselines = Baselines.GetOrCreateValue(__instance);
        foreach (var pair in __instance.BodyState)
        {
            var health = pair.Value.Health;
            var baseMaximum = health.Maximum;
            if (baseMaximum <= 0f) continue;

            var maximum = HealthElite.ScaleHealth(baseMaximum, percent);
            if (maximum <= baseMaximum) continue;

            baselines[pair.Key] = baseMaximum;
            health.Value.Maximum = maximum;

            // Keep the damage a part already carries instead of healing it for free.
            if (health.Current > 0f)
                health.Current = Math.Min(health.Current + (maximum - baseMaximum), maximum);
        }
    }

    /// <summary>Writes unmodified maximums back before the game save stores the profile.</summary>
    public static void RestoreProfileHealth(
        ActiveHealthController controller,
        Profile.HealthInfo health
    )
    {
        if (health?.BodyParts == null) return;
        if (!Baselines.TryGetValue(controller, out var baselines)) return;

        foreach (var pair in health.BodyParts)
        {
            if (!baselines.TryGetValue(pair.Key, out var baseMaximum)) continue;

            var value = pair.Value.Health;
            value.Maximum = baseMaximum;
            if (value.Current > baseMaximum) value.Current = baseMaximum;
        }
    }
}

/// <summary>
///     Restores the profile maximums when raid health is stored, so the elite bonus stays an
///     in-raid effect and cannot inflate the saved profile from raid to raid.
/// </summary>
internal class HealthEliteMaxHealthStorePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(ActiveHealthController), "Store", [typeof(Profile.HealthInfo)]);

    [PatchPostfix]
    public static void Postfix(ActiveHealthController __instance, Profile.HealthInfo __result) =>
        HealthEliteMaxHealthPatch.RestoreProfileHealth(__instance, __result);
}
