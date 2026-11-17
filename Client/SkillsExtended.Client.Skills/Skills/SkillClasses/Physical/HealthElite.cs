using System;
using System.Globalization;
using EFT;

namespace SkillsExtended.Skills.SkillClasses.Physical;

/// <summary>
///     Elite Health settings that the 4.1 client declares but never applies: the damage
///     absorption buff is never read, and no buff raises body part health.
/// </summary>
internal static class HealthElite
{
    public const float MaxAbsorbPercent = 100f;
    public const float MaxHealthPercent = 200f;

    /// <summary>
    ///     Configured absorption in percentage points. Returns <see langword="false" /> when
    ///     the skill is disabled or the configured value is zero.
    /// </summary>
    public static bool TryGetAbsorbPercent(out float percent)
    {
        var health = SkillsExtendedPlugin.SkillData?.Health;
        percent = health == null || !health.Enabled
            ? 0f
            : Math.Clamp(health.EliteAbsorbDamage, 0f, MaxAbsorbPercent);
        return percent > 0f;
    }

    /// <summary>
    ///     Configured body part health increase in percentage points. Returns
    ///     <see langword="false" /> when the skill is disabled or the configured value is zero.
    /// </summary>
    public static bool TryGetMaxHealthPercent(out float percent)
    {
        var health = SkillsExtendedPlugin.SkillData?.Health;
        percent = health == null || !health.Enabled
            ? 0f
            : Math.Clamp(health.EliteMaxHealthPercent, 0f, MaxHealthPercent);
        return percent > 0f;
    }

    public static bool IsElite(SkillManager skills) =>
        skills?.Health != null && skills.Health.IsEliteLevel;

    /// <summary>
    ///     Scales a body part maximum and rounds it to whole points so the health panel never
    ///     shows fractions: a 35 head with a 30% bonus becomes 46, not 45.5. Decimal math keeps
    ///     halves exact, so an 85 thorax becomes 111 rather than drifting down to 110.
    /// </summary>
    public static float ScaleHealth(float baseMaximum, float percent) =>
        (float)
            Math.Round(
                (decimal)baseMaximum * (1m + (decimal)percent / 100m),
                MidpointRounding.AwayFromZero
            );

    public static string Format(float value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
