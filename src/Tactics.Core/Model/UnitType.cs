namespace Tactics.Core.Model;

/// <summary>
/// The stats shared by every unit of one kind (warrior, archer…). They come from
/// <c>content/units.json</c>, so balancing the game means editing data, not code.
/// </summary>
/// <param name="Id">Stable key, e.g. "archer". Placements refer to it.</param>
/// <param name="Name">Display name, e.g. "Archer".</param>
/// <param name="MaxHp">Hit points at full health. A unit at 0 HP is defeated.</param>
/// <param name="Power">Damage dealt, or HP restored for a healer, before defense and caps.</param>
/// <param name="Defense">Subtracted from incoming damage. 0 means no protection.</param>
/// <param name="Move">The most tiles the unit can walk in one move, in 4 directions.</param>
/// <param name="MinRange">Closest target distance (Manhattan). 1 means adjacent.</param>
/// <param name="MaxRange">Farthest target distance (Manhattan).</param>
/// <param name="Ability">What acting does: damage an enemy or heal an ally.</param>
public sealed record UnitType(
    string Id,
    string Name,
    int MaxHp,
    int Power,
    int Defense,
    int Move,
    int MinRange,
    int MaxRange,
    Ability Ability);
