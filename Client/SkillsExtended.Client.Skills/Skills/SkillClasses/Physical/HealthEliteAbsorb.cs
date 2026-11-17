using System;
using System.Globalization;
using EFT;

namespace SkillsExtended.Skills.SkillClasses.Physical;

/// <summary>
///     EFT 4.1 defines the Health elite "Damage absorption" buff but never reads it, so the
///     configured percentage is applied by the client patch instead.
/// </summary>
internal static class HealthEliteAbsorb
{
    public const float MaxPercent = 100f;

    /// <summary>
    ///     Configured absorption in percentage points. Returns <see langword="false" /> when
    ///     the skill is disabled or the configured value is zero.
    /// </summary>
    public static bool TryGetPercent(out float percent)
    {
        var health = SkillsExtendedPlugin.SkillData?.Health;
        percent = health == null || !health.Enabled
            ? 0f
            : Math.Clamp(health.EliteAbsorbDamage, 0f, MaxPercent);
        return percent > 0f;
    }

    public static bool IsElite(SkillManager skills) =>
        skills?.Health != null && skills.Health.IsEliteLevel;

    public static string Format(float percent) =>
        percent.ToString("0.##", CultureInfo.InvariantCulture);
}
