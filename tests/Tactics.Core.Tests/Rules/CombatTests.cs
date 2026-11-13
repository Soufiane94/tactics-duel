using Tactics.Core.Actions;
using Tactics.Core.Model;
using Tactics.Core.Rules;
using Tactics.Core.Tests.Helpers;

namespace Tactics.Core.Tests.Rules;

/// <summary>
/// The combat formulas from PLAN.md, section 1, with the fixed test stats. Each part of a formula has
/// its own test, so breaking one number in Combat.cs fails the test that names that part.
/// </summary>
public sealed class CombatTests
{
    // The damage table for the test stats: attacker's power − target's defense.
    //
    //                      Warrior (def 3)   Archer (def 1)   Mage (def 0)   Healer (def 1)
    //   Warrior (power 10)        7                 9               10              9
    //   Archer  (power 8)         5                 7                8              7
    //   Mage    (power 12)        9                11               12             11
    [Theory]
    [InlineData("warrior", "warrior", 7)]
    [InlineData("warrior", "archer", 9)]
    [InlineData("warrior", "mage", 10)]
    [InlineData("warrior", "healer", 9)]
    [InlineData("archer", "warrior", 5)] // PLAN.md's example event: 8 − 3
    [InlineData("archer", "archer", 7)]
    [InlineData("archer", "mage", 8)]
    [InlineData("archer", "healer", 7)]
    [InlineData("mage", "warrior", 9)]
    [InlineData("mage", "archer", 11)]
    [InlineData("mage", "mage", 12)]
    [InlineData("mage", "healer", 11)]
    public void Damage_is_power_minus_defense(string attacker, string target, int expected)
    {
        Assert.Equal(expected, Combat.CalculateDamage(Type(attacker), Type(target)));
    }

    [Theory]
    [InlineData(3)] // power equal to the warrior's defense: 3 − 3 = 0
    [InlineData(1)] // power below it: 1 − 3 = −2
    public void Damage_is_at_least_1_even_against_a_higher_defense(int power)
    {
        var weakling = Type("archer") with { Power = power };

        Assert.Equal(1, Combat.CalculateDamage(weakling, Type("warrior")));
    }

    [Theory]
    [InlineData(20)] // 8 HP missing: the healer's full 5 (PLAN.md's example event: 20 → 25)
    [InlineData(23)] // exactly 5 missing
    public void A_heal_restores_the_healers_power(int warriorHp)
    {
        Assert.Equal(5, Combat.CalculateHeal(Type("healer"), Type("warrior"), warriorHp));
    }

    [Theory]
    [InlineData(24, 4)] // 4 HP missing out of 28: 4, not 5
    [InlineData(27, 1)]
    public void A_heal_never_restores_more_than_the_missing_HP(int warriorHp, int expected)
    {
        Assert.Equal(expected, Combat.CalculateHeal(Type("healer"), Type("warrior"), warriorHp));
    }

    [Fact]
    public void Three_units_gang_up_on_a_warrior()
    {
        var state = AsciiBoard.Parse("""
         A . w W
         . . M a
         """).WithUnit("two-warrior", u => u with { Hp = 9 });
            var s1 = Assert.IsType<ActionOutcome.Accepted>(
                GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-archer", "two-warrior")))
                .State;
            var s2 = Assert.IsType<ActionOutcome.Accepted>(
                GameEngine.Apply(s1, new ActAction(PlayerSide.One, "one-warrior", "two-warrior")))
                .State;

        //    var s3 = Assert.IsType<ActionOutcome.Accepted>(
        //        GameEngine.Apply(s2, new ActAction(PlayerSide.One, "one-mage", "two-warrior")))
        //        .State;
        //Assert.Equal(7, s3.FindUnit("two-warrior")!.Hp); //28 (warrior hp) − 5 (archer damage) − 7 (warrior damage) − 9 (mage damage) = 7

        //after adding warrior 2 hp to 9
        Assert.Equal(RuleViolation.TargetNotFound, Assert.IsType<ActionOutcome.Rejected>(GameEngine.Apply(s2, new ActAction(PlayerSide.One, "one-mage", "two-warrior"))).Code);
    }


    private static UnitType Type(string id) => AsciiBoard.DefaultUnitTypes[id];
}
