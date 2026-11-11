using System.Collections.Immutable;
using Tactics.Core.Model;

namespace Tactics.Core;

/// <summary>
/// The rules engine's entry point: pure functions that take a state (and later an action) and return a new state.
/// No I/O, no clock, no randomness, so the same inputs always give the same game.
/// </summary>
public static class GameEngine
{
    /// <summary>
    /// The state before the first action: turn 1, Player One to play,
    /// every unit at full HP and free to move and act.
    /// </summary>
    public static GameState CreateInitialState(GameSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);

        var units = setup.Placements
            .Select(p => new Unit(
                p.Id,
                p.TypeId,
                p.Owner,
                p.Position,
                Hp: setup.UnitTypes[p.TypeId].MaxHp,
                HasMoved: false,
                HasActed: false))
            .ToImmutableList();

        return new GameState(
            setup.Board,
            setup.UnitTypes,
            units,
            CurrentPlayer: PlayerSide.One,
            TurnNumber: 1,
            Status: GameStatus.InProgress,
            Winner: null);
    }
}
