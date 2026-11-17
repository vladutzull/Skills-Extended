namespace SkillsExtended.Config.Skills;

public class HealthData
{
    public bool Enabled { get; set; }
    public float BreakChanceRedPerLevel { get; set; }
    public float EnergyPerLevel { get; set; }
    public float HydrationPerLevel { get; set; }

    /// <summary>
    ///     EFT 4.1 declares the Health elite "Damage absorption" buff but never applies it.
    ///     Percentage of non-self-inflicted damage removed at elite level.
    /// </summary>
    public float EliteAbsorbDamage { get; set; } = 30f;
}