using Tactics.Core.Actions;
using Tactics.Core.Model;
using Tactics.Core.Tests.Helpers;

namespace Tactics.Core.Tests;

/// <summary>
/// <see cref="GameEngine.GetAvailableActions"/>: what the client may highlight. It must agree with
/// <see cref="GameEngine.Apply"/> exactly, so the last test tries every move and act on several boards and compares.
/// </summary>
public sealed class GameEngineAvailableActionsTests
{
    // Moments from different games: a start, a crowded fight, half a turn, the other side's turn, the end.
    private static readonly Dictionary<string, GameState> Moments = BuildMoments();

    public static TheoryData<string> MomentNames => new(Moments.Keys);

    [Fact]
    public void On_turn_1_of_the_classic_game_every_unit_can_move_and_nobody_is_in_range()
    {
        var available = GameEngine.GetAvailableActions(AsciiBoard.Parse(Maps.Classic), PlayerSide.One);

        string[] units = ["one-warrior", "one-archer", "one-mage", "one-healer"];
        Assert.Equal(units, available.Select(a => a.UnitId));
        Assert.All(available, a =>
        {
            Assert.NotEmpty(a.MoveTargets);
            Assert.Empty(a.ActTargets); // the armies start at least 6 tiles apart, and nobody is hurt yet
        });
    }

    [Fact]
    public void Each_unit_lists_the_tiles_it_can_reach_and_the_units_it_can_target()
    {
        // The archer is hurt, so the healer has someone to heal, even though the archer and the obstacle box it in.
        var state = AsciiBoard.Parse("""
            A . . m
            H # . .
            """).WithUnit("one-archer", u => u with { Hp = 10 });

        var available = GameEngine.GetAvailableActions(state, PlayerSide.One);

        string[] units = ["one-archer", "one-healer"];
        Assert.Equal(units, available.Select(a => a.UnitId));
        var archer = available[0];
        Position[] archerMoves = [new(1, 0), new(2, 0), new(2, 1)]; // nearest first: 1, 2, then 3 steps away
        Assert.Equal(archerMoves, archer.MoveTargets);
        Assert.Equal("two-mage", Assert.Single(archer.ActTargets)); // 3 tiles away: in range (2–3)
        var healer = available[1];
        Assert.Empty(healer.MoveTargets);
        Assert.Equal("one-archer", Assert.Single(healer.ActTargets));
    }

    [Fact]
    public void A_unit_with_nothing_left_to_do_is_not_listed()
    {
        // The same board at full HP: the boxed-in healer has nowhere to go and nobody to heal.
        var state = AsciiBoard.Parse("""
            A . . m
            H # . .
            """);

        var available = GameEngine.GetAvailableActions(state, PlayerSide.One);

        Assert.Equal("one-archer", Assert.Single(available).UnitId);
    }

    [Fact]
    public void A_units_options_shrink_as_it_moves_then_acts()
    {
        var start = AsciiBoard.Parse("A . . . m"); // the mage is 4 tiles away: too far for the archer (2–3)
        var moved = Assert.IsType<ActionOutcome.Accepted>(
            GameEngine.Apply(start, new MoveAction(PlayerSide.One, "one-archer", new Position(1, 0)))).State;
        var acted = Assert.IsType<ActionOutcome.Accepted>(
            GameEngine.Apply(moved, new ActAction(PlayerSide.One, "one-archer", "two-mage"))).State;

        var fresh = Assert.Single(GameEngine.GetAvailableActions(start, PlayerSide.One));
        Position[] moves = [new(1, 0), new(2, 0), new(3, 0)];
        Assert.Equal(moves, fresh.MoveTargets);
        Assert.Empty(fresh.ActTargets); // range counts from where the archer stands now, not from where it could go

        var afterMove = Assert.Single(GameEngine.GetAvailableActions(moved, PlayerSide.One));
        Assert.Empty(afterMove.MoveTargets); // it has moved
        Assert.Equal("two-mage", Assert.Single(afterMove.ActTargets)); // now 3 tiles away: in range

        Assert.Empty(GameEngine.GetAvailableActions(acted, PlayerSide.One)); // only ending the turn is left
    }

    [Fact]
    public void A_healer_lists_only_injured_allies_in_range()
    {
        // Next to the healer: a hurt ally and a hurt enemy. Then an ally at full HP, and a hurt ally 4 tiles away.
        var state = AsciiBoard.Parse("W H w A . M")
            .WithUnit("one-warrior", u => u with { Hp = 20 })
            .WithUnit("two-warrior", u => u with { Hp = 20 })
            .WithUnit("one-mage", u => u with { Hp = 5 });

        var healer = Assert.Single(GameEngine.GetAvailableActions(state, PlayerSide.One), a => a.UnitId == "one-healer");

        Assert.Equal("one-warrior", Assert.Single(healer.ActTargets));
    }

    [Fact]
    public void The_waiting_side_and_a_finished_game_get_nothing()
    {
        var classic = AsciiBoard.Parse(Maps.Classic);
        var finished = classic with { Status = GameStatus.Finished, Winner = PlayerSide.One };

        Assert.Empty(GameEngine.GetAvailableActions(classic, PlayerSide.Two)); // turn 1 is Player One's
        Assert.Empty(GameEngine.GetAvailableActions(finished, PlayerSide.One));
        Assert.Empty(GameEngine.GetAvailableActions(finished, PlayerSide.Two));
    }

    [Theory]
    [MemberData(nameof(MomentNames))]
    public void Every_listed_action_is_accepted_and_every_other_one_is_rejected(string moment)
    {
        // Brute force: both sides, every unit, every tile (plus a ring outside the board), every target.
        var state = Moments[moment];
        var disagreements = new List<string>();

        foreach (var side in Enum.GetValues<PlayerSide>())
        {
            var available = GameEngine.GetAvailableActions(state, side);
            foreach (var unit in state.Units)
            {
                var options = available.FirstOrDefault(a => a.UnitId == unit.Id);

                for (var y = -1; y <= state.Board.Height; y++)
                {
                    for (var x = -1; x <= state.Board.Width; x++)
                    {
                        var tile = new Position(x, y);
                        Check(options?.MoveTargets.Contains(tile) ?? false, new MoveAction(side, unit.Id, tile));
                    }
                }

                foreach (var targetId in state.Units.Select(u => u.Id).Append("nobody"))
                {
                    Check(options?.ActTargets.Contains(targetId) ?? false, new ActAction(side, unit.Id, targetId));
                }
            }
        }

        // Every mismatch at once, so a failure shows the whole pattern, not just the first case.
        Assert.Empty(disagreements);

        // Not a test that passes because nothing is allowed: while the game is on, the player to move has options.
        if (state.Status == GameStatus.InProgress)
        {
            Assert.NotEmpty(GameEngine.GetAvailableActions(state, state.CurrentPlayer));
        }

        void Check(bool listed, GameAction action)
        {
            var accepted = GameEngine.Apply(state, action) is ActionOutcome.Accepted;
            if (listed != accepted)
            {
                disagreements.Add($"{action}: {(listed ? "listed" : "not listed")}, but Apply {(accepted ? "accepts" : "rejects")} it");
            }
        }
    }

    private static Dictionary<string, GameState> BuildMoments()
    {
        // The crowded board from GameEngineActTests. Everyone has lost 4 HP, so the healers have work too.
        var brawl = AsciiBoard.Parse("""
            . a . h
            W m A w
            . H . M
            """);
        var hurt = brawl with { Units = [.. brawl.Units.Select(u => u with { Hp = u.Hp - 4 })] };
        var halfPlayed = Play(
            hurt,
            new ActAction(PlayerSide.One, "one-mage", "two-warrior"),
            new MoveAction(PlayerSide.One, "one-archer", new Position(2, 0)));
        var lastStand = AsciiBoard.Parse("A . m").WithUnit("two-mage", u => u with { Hp = 3 });

        return new()
        {
            ["classic opening"] = AsciiBoard.Parse(Maps.Classic),
            ["a brawl, everyone hurt"] = hurt,
            ["half a turn played"] = halfPlayed,
            ["the other side's turn"] = Play(halfPlayed, new EndTurnAction(PlayerSide.One)),
            ["game over"] = Play(lastStand, new ActAction(PlayerSide.One, "one-archer", "two-mage")),
        };
    }

    private static GameState Play(GameState state, params GameAction[] actions) =>
        actions.Aggregate(state, (current, action) => Assert.IsType<ActionOutcome.Accepted>(GameEngine.Apply(current, action)).State);
}
