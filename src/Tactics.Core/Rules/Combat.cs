using Tactics.Core.Model;

namespace Tactics.Core.Rules;

/// <summary>
/// The combat formulas, and nothing else: how hard a hit is and how much a heal restores.
/// Balancing the rules means editing this one file; balancing the numbers means editing content/units.json.
/// No randomness: the same stats always give the same result.
/// </summary>
public static class Combat
{
    /// <summary>
    /// Damage of one hit: the attacker's power minus the target's defense, but always at least 1,
    /// so no defense makes a unit invulnerable. It ignores the target's HP: on a killing blow it can be
    /// more than the HP left (overkill), and the engine stops HP at 0.
    /// </summary>
    public static int CalculateDamage(UnitType attackerType, UnitType targetType)
    {
        ArgumentNullException.ThrowIfNull(attackerType);
        ArgumentNullException.ThrowIfNull(targetType);

        return Math.Max(1, attackerType.Power - targetType.Defense);
    }

    /// <summary>
    /// HP restored by one heal: the healer's power, capped at what the target is missing,
    /// so a unit never goes above its MaxHp. It takes the target's HP as a number because a formula
    /// only needs stats and numbers, never the board.
    /// </summary>
    public static int CalculateHeal(UnitType healerType, UnitType targetType, int targetHp)
    {
        ArgumentNullException.ThrowIfNull(healerType);
        ArgumentNullException.ThrowIfNull(targetType);

        return Math.Min(healerType.Power, targetType.MaxHp - targetHp);
    }
}
