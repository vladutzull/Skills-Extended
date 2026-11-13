using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Utils;

namespace SkillsExtended.Patches;

/// <summary>
///     Applies the configured elite repair-wear chance reduction to repair-kit
///     repairs. EFT 4.1 no longer consumes the matching global fields, so SPT
///     must roll the "no wear" chance itself.
/// </summary>
[Injectable]
public class RepairWearChancePatch(
    GlobalTable globalTable,
    ProfileHelper profileHelper,
    TemplateTable templateTable,
    ItemHelper itemHelper,
    RandomUtil randomUtil
) : AbstractPatch
{
    private static GlobalTable _globalTable = null!;
    private static ProfileHelper _profileHelper = null!;
    private static TemplateTable _templateTable = null!;
    private static ItemHelper _itemHelper = null!;
    private static RandomUtil _randomUtil = null!;

    protected override MethodBase? GetTargetMethod()
    {
        _globalTable = globalTable;
        _profileHelper = profileHelper;
        _templateTable = templateTable;
        _itemHelper = itemHelper;
        _randomUtil = randomUtil;

        return AccessTools.Method(typeof(RepairService), nameof(RepairService.RepairItemByKit));
    }

    [PatchPrefix]
    public static void Prefix(PmcData pmcData, MongoId itemToRepairId, out RepairState __state)
    {
        var repairable = FindItem(pmcData, itemToRepairId)?.Upd?.Repairable;
        __state = repairable == null
            ? default
            : new RepairState(
                repairable.MaxDurability.GetValueOrDefault(),
                repairable.Durability.GetValueOrDefault()
            );
    }

    [PatchPostfix]
    public static void Postfix(
        PmcData pmcData,
        MongoId itemToRepairId,
        RepairState __state,
        RepairDetails __result
    )
    {
        if (__state.MaxDurability <= 0d) return;

        var item = FindItem(pmcData, itemToRepairId);
        var repairable = item?.Upd?.Repairable;
        if (item == null || repairable == null || __result == null) return;

        var loss = __state.MaxDurability - repairable.MaxDurability.GetValueOrDefault();
        if (loss <= 0d) return;

        var (skill, chance) = Resolve(item);
        if (skill == null) return;
        var skipChance = Math.Clamp(chance, 0d, 1d);
        if (skipChance <= 0d) return;
        if (!_profileHelper.HasEliteSkillLevel(skill.Value, pmcData)) return;
        if (!_randomUtil.GetChance100(skipChance * 100d)) return;

        // No wear this repair: restore the pre-repair maximum and the durability
        // the repair itself would have restored.
        var repaired = __state.Durability + __result.RepairAmount.GetValueOrDefault();
        repairable.MaxDurability = __state.MaxDurability;
        repairable.Durability = Math.Min(__state.MaxDurability, repaired);
    }

    private static (SkillTypes? Skill, double Chance) Resolve(Item item)
    {
        if (!_templateTable.Items.TryGetValue(item.Template, out var template))
        {
            return (null, 0d);
        }

        var skills = _globalTable.Configuration.SkillsSettings;

        if (_itemHelper.IsOfBaseclasses(
                item.Template,
                new[] { BaseClasses.VEST, BaseClasses.ARMORED_EQUIPMENT }))
        {
            return template.Properties?.ArmorType switch
            {
                "Light" => (SkillTypes.LightVests, skills.LightVests.WearChanceRepairLVestsReduceEliteLevel),
                "Heavy" => (SkillTypes.HeavyVests, skills.HeavyVests.WearChanceRepairHVestsReduceEliteLevel),
                _ => (SkillTypes.Intellect, skills.Intellect.WearChanceReduceEliteLevel),
            };
        }

        if (_itemHelper.IsOfBaseclass(item.Template, BaseClasses.WEAPON))
        {
            return (
                SkillTypes.WeaponTreatment,
                skills.WeaponTreatment.WearChanceRepairGunsReduceEliteLevel
            );
        }

        return (null, 0d);
    }

    private static Item? FindItem(PmcData pmcData, MongoId id) =>
        pmcData.Inventory?.Items?.FirstOrDefault(item => item.Id == id);

    public readonly record struct RepairState(double MaxDurability, double Durability);
}
