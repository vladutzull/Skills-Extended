namespace SkillsExtended.Config;

/// <summary>
///     Upgrades persisted native skill overrides to the current stored-unit conventions.
/// </summary>
public static class NativeSkillMigrations
{
    public const int CurrentVersion = 2;

    private static readonly string[] MagDrillsPercentFields =
        ["Buff.48.PerLevel", "Buff.49.PerLevel", "Buff.50.PerLevel"];

    /// <summary>
    ///     Before version 2 the Mag Drills buffs were stored as hundredths of a percentage
    ///     point (60) instead of percentage points (0.6). EFT formats those three buffs with
    ///     a literal percent sign, so their values are already percentages, unlike every other
    ///     skill buff whose description multiplies a fraction by 100.
    /// </summary>
    /// <returns><see langword="true" /> when a stored override was rewritten.</returns>
    public static bool Apply(SkillsConfig config)
    {
        if (config.Version >= CurrentVersion) return false;

        var changed = false;
        if (config.NativeSkills.Overrides.TryGetValue("MagDrills", out var values))
        {
            foreach (var key in MagDrillsPercentFields)
            {
                if (!values.TryGetValue(key, out var value)) continue;
                values[key] = value / 100f;
                changed = true;
            }
        }

        config.Version = CurrentVersion;
        return changed;
    }
}
