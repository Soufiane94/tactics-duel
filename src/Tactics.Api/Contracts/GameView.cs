using Tactics.Core.Model;

namespace Tactics.Api.Contracts;

/// <summary>
/// The game as one player sees it: the body of <c>GET /api/games/{gameId}</c>, and the <c>view</c> after every action.
/// The client draws exactly this and decides nothing: what a unit may do is listed in <c>availableActions</c>.
/// </summary>
/// <param name="GameId">The game's id.</param>
/// <param name="Sequence">How many actions have been played. Send it back as <c>expectedSequence</c> with your next action.</param>
/// <param name="YourSide">Your side, from your player token.</param>
/// <param name="CurrentPlayer">The side whose turn it is.</param>
/// <param name="TurnNumber">Starts at 1 and goes up by 1 each time a turn ends.</param>
/// <param name="Status">Whether the game is still on.</param>
/// <param name="Winner">The winning side once the game is finished, otherwise null.</param>
/// <param name="OpponentJoined">False while seat two is free. Player One can already play turn 1.</param>
/// <param name="Board">The grid and its obstacles.</param>
/// <param name="UnitTypes">Every unit type's stats, from this game's own setup: editing <c>content/</c> never changes a running game.</param>
/// <param name="Units">The units still on the board, in a stable order.</param>
/// <param name="AvailableActions">
/// What your units can still do. Empty when it isn't your turn or the game is over. Ending your turn is always
/// allowed on your turn, so it isn't listed.
/// </param>
public sealed record GameView(
    Guid GameId,
    int Sequence,
    PlayerSide YourSide,
    PlayerSide CurrentPlayer,
    int TurnNumber,
    GameStatus Status,
    PlayerSide? Winner,
    bool OpponentJoined,
    BoardView Board,
    IReadOnlyList<UnitTypeView> UnitTypes,
    IReadOnlyList<UnitView> Units,
    IReadOnlyList<UnitActionsView> AvailableActions);

/// <summary>A tile. The origin is the top-left corner: x grows to the right, y grows down.</summary>
/// <param name="X">Column, from 0 at the left edge.</param>
/// <param name="Y">Row, from 0 at the top edge.</param>
public sealed record PositionDto(int X, int Y);

/// <summary>The grid. It never changes during a game.</summary>
/// <param name="Width">Number of columns.</param>
/// <param name="Height">Number of rows.</param>
/// <param name="Obstacles">Tiles no unit can enter, in reading order (row by row, left to right).</param>
public sealed record BoardView(int Width, int Height, IReadOnlyList<PositionDto> Obstacles);

/// <summary>The stats shared by every unit of one type.</summary>
/// <param name="Id">The type's key, e.g. "archer". Units refer to it with <c>typeId</c>.</param>
/// <param name="Name">The display name, e.g. "Archer".</param>
/// <param name="MaxHp">Hit points at full health.</param>
/// <param name="Power">Damage dealt, or HP restored for a healer, before defense and caps.</param>
/// <param name="Defense">Subtracted from incoming damage.</param>
/// <param name="Move">The most tiles the unit can walk in one move.</param>
/// <param name="MinRange">The closest target distance, in tiles (Manhattan).</param>
/// <param name="MaxRange">The farthest target distance, in tiles (Manhattan).</param>
/// <param name="Ability">What acting does: damage an enemy, or heal an injured ally.</param>
public sealed record UnitTypeView(
    string Id,
    string Name,
    int MaxHp,
    int Power,
    int Defense,
    int Move,
    int MinRange,
    int MaxRange,
    Ability Ability);

/// <summary>One unit on the board.</summary>
/// <param name="Id">Unique in the game, e.g. "one-archer".</param>
/// <param name="TypeId">The id of its unit type.</param>
/// <param name="Owner">The side that controls it.</param>
/// <param name="X">Its column.</param>
/// <param name="Y">Its row.</param>
/// <param name="Hp">Its hit points now.</param>
/// <param name="HasMoved">True once it has moved this turn.</param>
/// <param name="HasActed">True once it has acted this turn. Acting also ends its movement.</param>
public sealed record UnitView(
    string Id,
    string TypeId,
    PlayerSide Owner,
    int X,
    int Y,
    int Hp,
    bool HasMoved,
    bool HasActed);

/// <summary>What one of your units can still do this turn. The server computed it, so the client highlights it as is.</summary>
/// <param name="UnitId">The unit.</param>
/// <param name="MoveTargets">Every tile it can move to, nearest first. Empty once it has moved or acted.</param>
/// <param name="ActTargets">The ids of the units it can act on from where it stands. Empty once it has acted.</param>
public sealed record UnitActionsView(string UnitId, IReadOnlyList<PositionDto> MoveTargets, IReadOnlyList<string> ActTargets);
