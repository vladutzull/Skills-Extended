using System;
using System.Collections.Generic;
using System.Globalization;
using EFT;
using EFT.InventoryLogic;
using SkillsExtended.Config;

namespace SkillsExtended.Skills.Core;

/// <summary>
///     Resolves the elite repair-wear reduction settings for the native repair skills.
///     EFT 4.1 no longer reads the matching global fields, so the client patches and the
///     in-game description use this helper to stay in sync with the configured value.
/// </summary>
internal static class NativeRepairWear
{
    private const string WeaponTemplate = "5422acb9af1c889c16000029";
    private const string ArmoredEquipmentTemplate = "57bef4c42459772e8d35a53b";
    private const string VestTemplate = "5448e5284bdc2dcb718b4567";

    private static readonly Dictionary<EBuffId, string> BuffFields = new()
    {
        [EBuffId.WearChanceRepairGunsReduceEliteLevel] =
            "WeaponTreatment.WearChanceRepairGunsReduceEliteLevel",
        [EBuffId.LightVestDeteriorationChanceOnRepairReduce] =
            "LightVests.WearChanceRepairLVestsReduceEliteLevel",
        [EBuffId.HeavyVestDeteriorationChanceOnRepairReduce] =
            "HeavyVests.WearChanceRepairHVestsReduceEliteLevel",
    };

    private static readonly Dictionary<string, string> SkillFields = new()
    {
        ["WeaponTreatment"] = "WeaponTreatment.WearChanceRepairGunsReduceEliteLevel",
        ["LightVests"] = "LightVests.WearChanceRepairLVestsReduceEliteLevel",
        ["HeavyVests"] = "HeavyVests.WearChanceRepairHVestsReduceEliteLevel",
        ["Intellect"] = "Intellect.WearChanceReduceEliteLevel",
    };

    private static readonly Dictionary<string, ESkillId> SkillIds = new()
    {
        ["WeaponTreatment"] = ESkillId.WeaponTreatment,
        ["LightVests"] = ESkillId.LightVests,
        ["HeavyVests"] = ESkillId.HeavyVests,
        ["Intellect"] = ESkillId.Intellect,
    };

    public static bool TryGetBuffField(EBuffId buffId, out string field) =>
        BuffFields.TryGetValue(buffId, out field);

    public static string GetSkillKey(Item item)
    {
        if (item.Template.IsChildOf(WeaponTemplate)) return "WeaponTreatment";
        if (!item.Template.IsChildOf(VestTemplate)
            && !item.Template.IsChildOf(ArmoredEquipmentTemplate)) return null;
        if (!item.TryGetItemComponent<ArmorComponent>(out var armor)) return null;
        return armor.ArmorType switch
        {
            EArmorType.Light => "LightVests",
            EArmorType.Heavy => "HeavyVests",
            _ => "Intellect",
        };
    }

    public static string GetFieldKey(string skillKey) =>
        SkillFields.GetValueOrDefault(skillKey);

    /// <summary>
    ///     Configured reduction in percentage points (0-100).
    /// </summary>
    public static float GetValue(string fieldKey)
    {
        if (fieldKey == null) return 0f;
        var separator = fieldKey.IndexOf('.');
        if (separator < 0) return 0f;
        var skillKey = fieldKey[..separator];
        var data = SkillsExtendedPlugin.SkillData?.NativeSkills;
        if (data != null
            && data.Overrides.TryGetValue(skillKey, out var values)
            && values != null
            && values.TryGetValue(fieldKey, out var value))
            return Math.Clamp(value, 0f, 100f);
        return NativeSkillCatalog.Defaults.TryGetValue(fieldKey, out var fallback)
            ? Math.Clamp(fallback, 0f, 100f)
            : 0f;
    }

    public static bool IsElite(SkillManager skillManager, string skillKey)
    {
        if (skillManager == null || !SkillIds.TryGetValue(skillKey, out var id)) return false;
        var skill = skillManager.GetSkill(id);
        return skill != null && skill.IsEliteLevel;
    }

    public static string Format(float value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
