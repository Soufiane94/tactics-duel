using Tactics.Core.Model;
using Tactics.Core.Tests.Helpers;

namespace Tactics.Core.Tests;

public sealed class GameEngineTests
{
    [Fact]
    public void The_classic_game_starts_on_turn_1_with_player_one_and_8_fresh_units()
    {
        var state = AsciiBoard.Parse(Maps.Classic);

        Assert.Equal(1, state.TurnNumber);
        Assert.Equal(PlayerSide.One, state.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, state.Status);
        Assert.Null(state.Winner);
        Assert.Equal(8, state.Units.Count);
        Assert.All(state.Units, unit =>
        {
            Assert.Equal(state.TypeOf(unit).MaxHp, unit.Hp);
            Assert.False(unit.HasMoved);
            Assert.False(unit.HasActed);
        });
    }

    [Fact]
    public void CreateInitialState_turns_each_placement_into_a_unit_in_the_same_order()
    {
        var setup = new GameSetup(
            "test",
            new Board(3, 3, []),
            AsciiBoard.DefaultUnitTypes,
            [
                new UnitPlacement("one-archer", "archer", PlayerSide.One, new Position(0, 2)),
                new UnitPlacement("two-mage", "mage", PlayerSide.Two, new Position(2, 0)),
            ]);

        var state = GameEngine.CreateInitialState(setup);

        Unit[] expected =
        [
            new("one-archer", "archer", PlayerSide.One, new Position(0, 2), Hp: 18, HasMoved: false, HasActed: false),
            new("two-mage", "mage", PlayerSide.Two, new Position(2, 0), Hp: 14, HasMoved: false, HasActed: false),
        ];
        Assert.Equal(expected, state.Units);
        Assert.Same(setup.Board, state.Board); // immutable, so sharing is safe: nothing can change it
        Assert.Same(setup.UnitTypes, state.UnitTypes);
    }
}
