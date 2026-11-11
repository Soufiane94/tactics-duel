using System.Collections.Immutable;
using Tactics.Core.Actions;
using Tactics.Core.Model;

namespace Tactics.Core;

/// <summary>
/// The rules engine's entry point: pure functions that take a state (and an action) and return a new state.
/// No I/O, no clock, no randomness, so the same inputs always give the same game.
/// The rules for each action live in their own file (<c>GameEngine.Move.cs</c>…), as parts of this one class.
/// </summary>
public static partial class GameEngine
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

    /// <summary>
    /// Plays one action: first check it against the rules, then apply it. A broken rule is a normal answer,
    /// not an exception: it comes back as <see cref="ActionOutcome.Rejected"/>, and nothing changes.
    /// </summary>
    public static ActionOutcome Apply(GameState state, GameAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        // The checks every action shares come first. The order is fixed, so a mistake always gets the same code.
        if (state.Status == GameStatus.Finished)
        {
            return state.Winner is { } winner
                ? Reject(RuleViolation.GameOver, $"The game is over: {Describe(winner)} won.")
                : Reject(RuleViolation.GameOver, "The game is over.");
        }

        if (action.Player != state.CurrentPlayer)
        {
            return Reject(RuleViolation.NotYourTurn, $"It's {Describe(state.CurrentPlayer)}'s turn.");
        }

        // One case per action type. C# can't tell that this list is complete, hence the last arm.
        return action switch
        {
            MoveAction move => ApplyMove(state, move),
            _ => throw new ArgumentException($"The engine has no rules for {action.GetType().Name}.", nameof(action)),
        };
    }

    private static ActionOutcome.Rejected Reject(RuleViolation code, string message) => new(code, message);

    private static string Describe(PlayerSide side) => $"Player {side}";

    private static string Tiles(int count) => count == 1 ? "1 tile" : $"{count} tiles";
}
