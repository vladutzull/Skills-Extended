using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using SkillsExtended.Config;
using SkillsExtended.Skills.SkillClasses.Physical.Patches;

public static class HealthEliteAbsorbDamageChecks
{
    public static void Run(SkillsConfig config, Action<bool, string> check)
    {
        var health = config.Health;
        var originalEnabled = health.Enabled;
        var originalPercent = health.EliteAbsorbDamage;
        var controller = new ActiveHealthController();
        var shot = new DamageInfo { DamageType = EDamageType.Bullet };
        try
        {
            health.Enabled = true;
            health.EliteAbsorbDamage = 30f;
            controller.Player.Skills.Health.IsEliteLevel = false;
            check(
                Apply(controller, shot, 100f) == 100f,
                "Non-elite Health keeps incoming damage unchanged"
            );

            controller.Player.Skills.Health.IsEliteLevel = true;
            check(
                Math.Abs(Apply(controller, shot, 100f) - 70f) < .0001f,
                "Health elite absorption removes the configured 30%"
            );

            health.EliteAbsorbDamage = 30f;
            check(
                Apply(controller, shot, -10f) == -10f,
                "Negative damage such as healing is never absorbed"
            );

            var bleeding = new DamageInfo { DamageType = EDamageType.LightBleeding };
            check(
                Apply(controller, bleeding, 100f) == 100f,
                "Health elite absorption leaves self-inflicted bleeding damage unchanged"
            );

            health.EliteAbsorbDamage = 0f;
            check(
                Apply(controller, shot, 100f) == 100f,
                "Zero absorption keeps incoming damage unchanged"
            );

            health.EliteAbsorbDamage = 100f;
            check(
                Apply(controller, shot, 100f) == 0f,
                "Full absorption removes all incoming damage"
            );

            health.EliteAbsorbDamage = 150f;
            check(
                Apply(controller, shot, 100f) == 0f,
                "Absorption clamps at 100 percent"
            );

            health.EliteAbsorbDamage = -5f;
            check(
                Apply(controller, shot, 100f) == 100f,
                "Negative absorption is ignored"
            );

            health.EliteAbsorbDamage = 12.5f;
            check(
                Math.Abs(Apply(controller, shot, 80f) - 70f) < .0001f,
                "Fractional absorption scales incoming damage"
            );

            health.Enabled = false;
            health.EliteAbsorbDamage = 30f;
            check(
                Apply(controller, shot, 100f) == 100f,
                "Disabled Health skill keeps incoming damage unchanged"
            );
        }
        finally
        {
            health.Enabled = originalEnabled;
            health.EliteAbsorbDamage = originalPercent;
            controller.Player.Skills.Health.IsEliteLevel = false;
        }
    }

    private static float Apply(ActiveHealthController controller, DamageInfo info, float damage)
    {
        HealthEliteAbsorbDamagePatch.Prefix(controller, info, ref damage);
        return damage;
    }
}
