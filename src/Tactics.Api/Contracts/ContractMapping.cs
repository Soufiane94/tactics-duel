using Tactics.Api.Games;
using Tactics.Core;
using Tactics.Core.Actions;
using Tactics.Core.Events;
using Tactics.Core.Model;

namespace Tactics.Api.Contracts;

/// <summary>
/// The one place where Core records become DTOs, and DTOs become Core actions. It's explicit on purpose: the rules
/// can rename or add a field without changing the JSON that clients read, and the compiler points here when they do.
/// </summary>
public static class ContractMapping
{
    /// <summary>The game as <paramref name="side"/> sees it, for a state rebuilt from the record's action log.</summary>
    public static GameView ToView(this GameRecord game, GameState state, PlayerSide side)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(state);

        return new GameView(
            game.Id,
            game.Sequence,
            side,
            state.CurrentPlayer,
            state.TurnNumber,
            state.Status,
            state.Winner,
            OpponentJoined: side == PlayerSide.Two || game.HasPlayerTwo,
            ToDto(state.Board),
            // A dictionary has no reliable order. Sorting keeps the JSON identical from one request to the next.
            [.. state.UnitTypes.Values.OrderBy(type => type.Id, StringComparer.Ordinal).Select(ToDto)],
            [.. state.Units.Select(ToDto)],
            // Computed for the side that asks: on the other side's turn, it's empty.
            [.. GameEngine.GetAvailableActions(state, side).Select(ToDto)]);
    }

    /// <summary>The wire version of a Core event. Same fields, same order, so the JSON matches the Core event's.</summary>
    public static EventDto ToDto(this GameEvent gameEvent) => gameEvent switch
    {
        UnitMoved e => new UnitMovedDto(e.UnitId, ToDto(e.From), ToDto(e.To), [.. e.Path.Select(ToDto)]),
        UnitDamaged e => new UnitDamagedDto(e.UnitId, e.ByUnitId, e.Amount, e.HpAfter),
        UnitHealed e => new UnitHealedDto(e.UnitId, e.ByUnitId, e.Amount, e.HpAfter),
        UnitDefeated e => new UnitDefeatedDto(e.UnitId),
        TurnEnded e => new TurnEndedDto(e.PreviousPlayer, e.NextPlayer, e.TurnNumber),
        GameWon e => new GameWonDto(e.Winner),
        _ => throw new ArgumentException($"There's no DTO for the event {gameEvent?.GetType().Name}.", nameof(gameEvent)),
    };

    /// <summary>
    /// The Core action for a client's request. <paramref name="player"/> comes from the player token, never from
    /// the request: that's what stops a client from playing for the other side.
    /// </summary>
    public static GameAction ToCore(this ActionDto action, PlayerSide player) => action switch
    {
        MoveActionDto move => new MoveAction(player, move.UnitId, new Position(move.To.X, move.To.Y)),
        ActActionDto act => new ActAction(player, act.ActorId, act.TargetId),
        EndTurnActionDto => new EndTurnAction(player),
        _ => throw new ArgumentException($"There's no Core action for {action?.GetType().Name}.", nameof(action)),
    };

    private static BoardView ToDto(Board board) =>
        new(board.Width, board.Height, [.. board.Obstacles.OrderBy(p => p.Y).ThenBy(p => p.X).Select(ToDto)]);

    private static UnitTypeView ToDto(UnitType type) => new(
        type.Id, type.Name, type.MaxHp, type.Power, type.Defense, type.Move, type.MinRange, type.MaxRange, type.Ability);

    private static UnitView ToDto(Unit unit) =>
        new(unit.Id, unit.TypeId, unit.Owner, unit.Position.X, unit.Position.Y, unit.Hp, unit.HasMoved, unit.HasActed);

    private static UnitActionsView ToDto(UnitActions actions) =>
        new(actions.UnitId, [.. actions.MoveTargets.Select(ToDto)], actions.ActTargets);

    private static PositionDto ToDto(Position position) => new(position.X, position.Y);
}
