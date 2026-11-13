namespace Tactics.Core.Model;

/// <summary>What a unit's action does to its target. Set per unit type in <c>content/units.json</c>.</summary>
public enum Ability
{
    /// <summary>Hurts an enemy in range. The formula: <see cref="Tactics.Core.Rules.Combat.CalculateDamage"/>.</summary>
    Damage,

    /// <summary>Restores HP to an injured ally in range. The formula: <see cref="Tactics.Core.Rules.Combat.CalculateHeal"/>.</summary>
    Heal,
}
