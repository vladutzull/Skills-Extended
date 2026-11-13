using System.Reflection;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Skills.Core;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.Core.Patches;

/// <summary>
///     Makes the repair-kit wear prediction follow the configured elite chance.
///     A chance to skip wear is displayed as a 0..max range; a fully disabled
///     chance restores the normal range, and a 100% chance removes the range.
/// </summary>
internal class RepairKitWearPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(RepairKitsCollection), nameof(RepairKitsCollection.GetDegradationValues));

    [PatchPostfix]
    public static void Postfix(
        RepairKitsCollection __instance,
        RepairableComponent repairable,
        Item item,
        ref Vector2 __result
    )
    {
        var skillKey = NativeRepairWear.GetSkillKey(item);
        if (skillKey == null) return;
        var field = NativeRepairWear.GetFieldKey(skillKey);
        if (field == null) return;
        var skillManager = __instance._repairController?._skillManager;
        if (!NativeRepairWear.IsElite(skillManager, skillKey)) return;

        var chance = NativeRepairWear.GetValue(field) / 100f;
        var quality = item.IsArmorMod() ? 1f : __instance.GetRepairQuality(item);
        var degradation = repairable.RepairKitDegradation;
        var minimum = quality * repairable.TemplateDurability * degradation.x;
        var maximum = quality * repairable.TemplateDurability * degradation.y;

        __result = chance switch
        {
            <= 0f => new Vector2(minimum, maximum),
            >= 1f => Vector2.zero,
            _ => new Vector2(0f, maximum),
        };
    }
}
