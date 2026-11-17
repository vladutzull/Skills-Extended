using EFT;
using EFT.HealthSystem;
using SkillsExtended.Config;
using SkillsExtended.Skills.SkillClasses.Physical.Patches;

public static class HealthEliteMaxHealthChecks
{
    public static void Run(SkillsConfig config, Action<bool, string> check)
    {
        var health = config.Health;
        var originalEnabled = health.Enabled;
        var originalPercent = health.EliteMaxHealthPercent;
        try
        {
            health.Enabled = true;
            health.EliteMaxHealthPercent = 30f;

            var idle = Controller(elite: false);
            HealthEliteMaxHealthPatch.Postfix(idle);
            check(
                Maximum(idle, EBodyPart.Head) == 35f
                    && Current(idle, EBodyPart.Head) == 35f,
                "Non-elite Health keeps vanilla body part health"
            );

            var elite = Controller(elite: true);
            HealthEliteMaxHealthPatch.Postfix(elite);
            check(
                Maximum(elite, EBodyPart.Head) == 46f,
                "Elite Health rounds a 35 HP head to 46"
            );
            check(
                Maximum(elite, EBodyPart.Thorax) == 111f,
                "Elite Health rounds an 85 HP thorax to 111"
            );
            check(
                Maximum(elite, EBodyPart.Stomach) == 91f
                    && Maximum(elite, EBodyPart.LeftArm) == 78f
                    && Maximum(elite, EBodyPart.LeftLeg) == 85f,
                "Elite Health scales every hitbox and rounds halves up"
            );
            check(
                Current(elite, EBodyPart.Head) == 46f
                    && Current(elite, EBodyPart.LeftLeg) == 85f,
                "Elite Health raises current health with the new maximum"
            );
            check(
                AllWhole(elite),
                "Elite Health never leaves fractional hitbox health"
            );

            var wounded = Controller(elite: true);
            wounded.BodyState[EBodyPart.Head].Health.Current = 20f;
            HealthEliteMaxHealthPatch.Postfix(wounded);
            check(
                Maximum(wounded, EBodyPart.Head) == 46f && Current(wounded, EBodyPart.Head) == 31f,
                "Elite Health keeps the damage a hitbox already carries"
            );

            var destroyed = Controller(elite: true);
            destroyed.BodyState[EBodyPart.Thorax].Health.Current = 0f;
            HealthEliteMaxHealthPatch.Postfix(destroyed);
            check(
                Maximum(destroyed, EBodyPart.Thorax) == 111f
                    && Current(destroyed, EBodyPart.Thorax) == 0f,
                "Elite Health never revives a destroyed hitbox"
            );

            health.EliteMaxHealthPercent = 100f;
            var doubled = Controller(elite: true);
            HealthEliteMaxHealthPatch.Postfix(doubled);
            check(
                Maximum(doubled, EBodyPart.Head) == 70f,
                "Full elite health bonus doubles hitbox health"
            );

            health.EliteMaxHealthPercent = 250f;
            var capped = Controller(elite: true);
            HealthEliteMaxHealthPatch.Postfix(capped);
            check(
                Maximum(capped, EBodyPart.Head) == 105f,
                "Elite health bonus caps at 200 percent"
            );

            health.EliteMaxHealthPercent = 0f;
            var zero = Controller(elite: true);
            HealthEliteMaxHealthPatch.Postfix(zero);
            check(
                Maximum(zero, EBodyPart.Head) == 35f,
                "Zero elite health bonus keeps vanilla hitbox health"
            );

            health.EliteMaxHealthPercent = -5f;
            var negative = Controller(elite: true);
            HealthEliteMaxHealthPatch.Postfix(negative);
            check(
                Maximum(negative, EBodyPart.Head) == 35f,
                "Negative elite health bonus is ignored"
            );

            health.Enabled = false;
            health.EliteMaxHealthPercent = 30f;
            var disabled = Controller(elite: true);
            HealthEliteMaxHealthPatch.Postfix(disabled);
            check(
                Maximum(disabled, EBodyPart.Head) == 35f,
                "Disabled Health skill keeps vanilla hitbox health"
            );

            health.Enabled = true;
            var stored = Controller(elite: true);
            HealthEliteMaxHealthPatch.Postfix(stored);
            var profile = StoredProfile(stored);
            HealthEliteMaxHealthStorePatch.Postfix(stored, profile);
            check(
                profile.BodyParts[EBodyPart.Head].Health.Maximum == 35f
                    && profile.BodyParts[EBodyPart.Head].Health.Current == 35f,
                "Storing raid health restores the unmodified profile maximums"
            );
            check(
                profile.BodyParts[EBodyPart.Thorax].Health.Maximum == 85f
                    && profile.BodyParts[EBodyPart.Head].Health.Maximum == 35f,
                "Storing raid health cannot persist the elite bonus"
            );

            var damaged = Controller(elite: true);
            damaged.BodyState[EBodyPart.Head].Health.Current = 20f;
            HealthEliteMaxHealthPatch.Postfix(damaged);
            var damagedProfile = StoredProfile(damaged);
            HealthEliteMaxHealthStorePatch.Postfix(damaged, damagedProfile);
            check(
                damagedProfile.BodyParts[EBodyPart.Head].Health.Maximum == 35f
                    && damagedProfile.BodyParts[EBodyPart.Head].Health.Current == 31f,
                "Storing raid health preserves damage taken inside the base maximum"
            );
        }
        finally
        {
            health.Enabled = originalEnabled;
            health.EliteMaxHealthPercent = originalPercent;
        }
    }

    private static ActiveHealthController Controller(bool elite)
    {
        var controller = new ActiveHealthController();
        controller._skills.Health.IsEliteLevel = elite;
        Add(controller, EBodyPart.Head, 35f);
        Add(controller, EBodyPart.Thorax, 85f);
        Add(controller, EBodyPart.Stomach, 70f);
        Add(controller, EBodyPart.LeftArm, 60f);
        Add(controller, EBodyPart.RightArm, 60f);
        Add(controller, EBodyPart.LeftLeg, 65f);
        Add(controller, EBodyPart.RightLeg, 65f);
        return controller;
    }

    private static void Add(ActiveHealthController controller, EBodyPart part, float maximum) =>
        controller.BodyState[part] = new BodyPartState
        {
            Health = new HealthValue(
                new Profile.HealthInfo.ValueInfo { Current = maximum, Maximum = maximum }
            ),
        };

    private static Profile.HealthInfo StoredProfile(ActiveHealthController controller)
    {
        var profile = new Profile.HealthInfo();
        foreach (var pair in controller.BodyState)
        {
            profile.BodyParts[pair.Key] = new Profile.HealthInfo.BodyPartInfo
            {
                Health = new Profile.HealthInfo.ValueInfo
                {
                    Current = pair.Value.Health.Current,
                    Maximum = pair.Value.Health.Maximum,
                },
            };
        }

        return profile;
    }
    private static float Maximum(ActiveHealthController controller, EBodyPart part) =>
        controller.BodyState[part].Health.Maximum;

    private static float Current(ActiveHealthController controller, EBodyPart part) =>
        controller.BodyState[part].Health.Current;

    private static bool AllWhole(ActiveHealthController controller) =>
        controller.BodyState.Values.All(state => state.Health.Maximum % 1f == 0f);
}
