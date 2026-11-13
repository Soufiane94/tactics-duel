using System.Collections.Immutable;
using Tactics.Core.Events;
using Tactics.Core.Model;

namespace Tactics.Core;

public static partial class GameEngine
{
    // Ending a turn has nothing left to validate: Apply already checked that the game is in progress and that
    // it's this player's turn, and you may end your turn at any time. So only the apply step remains.
    private static ActionOutcome.Accepted ApplyEndTurn(GameState state)
    {
        var next = state.CurrentPlayer.Opponent();

        // The next side's units get their move and act back. The side that just played keeps its flags until
        // its own next turn: its units can't do anything meanwhile, and the screen can still show who acted.
        var units = state.Units
            .Select(u => u.Owner == next ? u with { HasMoved = false, HasActed = false } : u)
            .ToImmutableList();

        var after = state with { Units = units, CurrentPlayer = next, TurnNumber = state.TurnNumber + 1 };
        return new ActionOutcome.Accepted(after, [new TurnEnded(state.CurrentPlayer, next, after.TurnNumber)]);
    }
}
