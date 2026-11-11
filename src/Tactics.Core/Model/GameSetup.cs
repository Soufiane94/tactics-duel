using System.Collections.Immutable;

namespace Tactics.Core.Model;

/// <summary>
/// Everything needed to start a game: scenario, board, unit stats and starting positions.
/// The server keeps one snapshot per game (Step 5), so editing <c>content/</c> never changes running games.
/// </summary>
/// <param name="ScenarioId">The scenario it came from, e.g. "classic".</param>
/// <param name="Board">The grid and its obstacles.</param>
/// <param name="UnitTypes">Unit stats, by type id.</param>
/// <param name="Placements">Starting units, in file order.</param>
public sealed record GameSetup(
    string ScenarioId,
    Board Board,
    ImmutableDictionary<string, UnitType> UnitTypes,
    ImmutableList<UnitPlacement> Placements);
