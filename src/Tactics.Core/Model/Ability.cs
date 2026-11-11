namespace Tactics.Core.Model;

/// <summary>What a unit's action does to its target. Set per unit type in <c>content/units.json</c>.</summary>
public enum Ability
{
    /// <summary>Hurts an enemy in range (the formula lives in Combat, from Step 3).</summary>
    Damage,

    /// <summary>Restores HP to an injured ally in range (the formula lives in Combat, from Step 3).</summary>
    Heal,
}
