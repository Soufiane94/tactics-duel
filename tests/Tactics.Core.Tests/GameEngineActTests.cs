using Tactics.Core.Actions;
using Tactics.Core.Events;
using Tactics.Core.Model;
using Tactics.Core.Tests.Helpers;

namespace Tactics.Core.Tests;

/// <summary>
/// <see cref="GameEngine.Apply"/> with an <see cref="ActAction"/>: hit an enemy or heal an ally in range.
/// Fixed test stats (power / defense / range): warrior 10 / 3 / 1, archer 8 / 1 / 2–3, mage 12 / 0 / 1–2,
/// and the healer heals 5 at range 1–2. Every board has units on both sides, and every defeat leaves the
/// loser another unit, so the victory check (<see cref="GameEngineVictoryTests"/>) adds no event to these tests.
/// </summary>
public sealed class GameEngineActTests
{
    // A crowded board: every unit has at least one unit it can act on, and at least one it can't.
    private const string Brawl = """
        . a . h
        W m A w
        . H . M
        """;

    public static TheoryData<string> BrawlUnitIds => new(AsciiBoard.Parse(Brawl).Units.Select(u => u.Id));

    [Fact]
    public void An_attack_takes_HP_off_the_target_and_emits_UnitDamaged()
    {
        var state = AsciiBoard.Parse("A . . w");

        var outcome = GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-archer", "two-warrior"));

        // The example from PLAN.md, section 3 (Events): 8 power − 3 defense = 5 damage, and 28 − 5 = 23.
        var accepted = Assert.IsType<ActionOutcome.Accepted>(outcome);
        Assert.Equal(new UnitDamaged("two-warrior", "one-archer", Amount: 5, HpAfter: 23), Assert.Single(accepted.Events));
        Assert.Equal(23, accepted.State.FindUnit("two-warrior")!.Hp);
    }

    [Fact]
    public void Acting_spends_the_actor_but_not_the_target()
    {
        var state = AsciiBoard.Parse("A . . w");

        var outcome = GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-archer", "two-warrior"));

        var after = Assert.IsType<ActionOutcome.Accepted>(outcome).State;
        var archer = after.FindUnit("one-archer")!;
        Assert.True(archer.HasActed);
        Assert.True(archer.HasMoved); // acting ends the unit's activity, so it can't move afterwards
        var warrior = after.FindUnit("two-warrior")!;
        Assert.False(warrior.HasActed); // being hit isn't acting: the warrior's own turn is untouched
        Assert.False(warrior.HasMoved);
        Assert.Equal(PlayerSide.One, after.CurrentPlayer); // acting doesn't end the turn
        Assert.Equal(1, after.TurnNumber);
    }

    [Fact]
    public void A_heal_gives_an_injured_ally_HP_back_and_emits_UnitHealed()
    {
        var state = AsciiBoard.Parse("W H . w").WithUnit("one-warrior", u => u with { Hp = 20 });

        var outcome = GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-healer", "one-warrior"));

        // The example from PLAN.md, section 3 (Events): the healer's 5 power, and 20 + 5 = 25.
        var accepted = Assert.IsType<ActionOutcome.Accepted>(outcome);
        Assert.Equal(new UnitHealed("one-warrior", "one-healer", Amount: 5, HpAfter: 25), Assert.Single(accepted.Events));
        Assert.Equal(25, accepted.State.FindUnit("one-warrior")!.Hp);
    }

    [Fact]
    public void A_heal_stops_at_max_HP()
    {
        var state = AsciiBoard.Parse("W H . w").WithUnit("one-warrior", u => u with { Hp = 26 });

        var outcome = GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-healer", "one-warrior"));

        // Only 2 of the warrior's 28 HP were missing, so the event says 2, not the healer's 5.
        var accepted = Assert.IsType<ActionOutcome.Accepted>(outcome);
        Assert.Equal(new UnitHealed("one-warrior", "one-healer", Amount: 2, HpAfter: 28), Assert.Single(accepted.Events));
    }

    [Fact]
    public void A_unit_at_0_HP_is_defeated_and_leaves_the_board()
    {
        var state = AsciiBoard.Parse("""
            A . m
            . . w
            """).WithUnit("two-mage", u => u with { Hp = 3 });

        var outcome = GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-archer", "two-mage"));

        // Cause before effect: the hit, then the defeat. Overkill: the hit keeps its full 8, and HP stops at 0.
        var accepted = Assert.IsType<ActionOutcome.Accepted>(outcome);
        GameEvent[] events = [new UnitDamaged("two-mage", "one-archer", Amount: 8, HpAfter: 0), new UnitDefeated("two-mage")];
        Assert.Equal(events, accepted.Events);
        Assert.Equal("""
            A . .
            . . w
            """, AsciiBoard.Draw(accepted.State), ignoreLineEndingDifferences: true);
        string[] stillThere = ["one-archer", "two-warrior"];
        Assert.Equal(stillThere, accepted.State.Units.Select(u => u.Id)); // the others keep their order
    }

    [Theory]
    [InlineData(8, true)] // exactly the archer's 8 damage: 0 HP left
    [InlineData(9, false)] // 1 HP left: hurt, but still on the board
    public void Only_0_HP_defeats_a_unit(int mageHp, bool defeated)
    {
        var state = AsciiBoard.Parse("A . m w").WithUnit("two-mage", u => u with { Hp = mageHp });

        var outcome = GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-archer", "two-mage"));

        var accepted = Assert.IsType<ActionOutcome.Accepted>(outcome);
        Assert.Equal(defeated, accepted.Events.OfType<UnitDefeated>().Any());
        Assert.Equal(defeated, accepted.State.FindUnit("two-mage") is null);
    }

    [Fact]
    public void A_defeated_unit_can_no_longer_act_or_be_targeted()
    {
        var state = AsciiBoard.Parse("""
            A . m w
            M . . .
            """).WithUnit("two-mage", u => u with { Hp = 3 });
        var after = Assert.IsType<ActionOutcome.Accepted>(
            GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-archer", "two-mage"))).State;

        var targetIt = GameEngine.Apply(after, new ActAction(PlayerSide.One, "one-mage", "two-mage"));
        var twosTurn = Assert.IsType<ActionOutcome.Accepted>(GameEngine.Apply(after, new EndTurnAction(PlayerSide.One))).State;
        var actWithIt = GameEngine.Apply(twosTurn, new ActAction(PlayerSide.Two, "two-mage", "one-mage"));

        Assert.Equal(RuleViolation.TargetNotFound, Assert.IsType<ActionOutcome.Rejected>(targetIt).Code);
        Assert.Equal(RuleViolation.UnitNotFound, Assert.IsType<ActionOutcome.Rejected>(actWithIt).Code);
    }

    [Fact]
    public void A_unit_can_move_into_range_then_act()
    {
        var state = AsciiBoard.Parse("A . . . . w"); // 5 tiles apart: too far for the archer's 2–3

        var tooFar = GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-archer", "two-warrior"));
        var moved = Assert.IsType<ActionOutcome.Accepted>(
            GameEngine.Apply(state, new MoveAction(PlayerSide.One, "one-archer", new Position(2, 0)))).State;
        var shot = GameEngine.Apply(moved, new ActAction(PlayerSide.One, "one-archer", "two-warrior"));

        Assert.Equal(RuleViolation.TargetOutOfRange, Assert.IsType<ActionOutcome.Rejected>(tooFar).Code);
        Assert.IsType<ActionOutcome.Accepted>(shot); // range counts from where the archer stands now: 3 tiles
    }

    [Fact]
    public void A_unit_that_acted_cannot_move_or_act_again()
    {
        var state = AsciiBoard.Parse("""
            A . . w
            M . a .
            """);
        var after = Assert.IsType<ActionOutcome.Accepted>(
            GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-archer", "two-warrior"))).State;

        var move = GameEngine.Apply(after, new MoveAction(PlayerSide.One, "one-archer", new Position(1, 0)));
        var actAgain = GameEngine.Apply(after, new ActAction(PlayerSide.One, "one-archer", "two-archer"));
        var otherUnit = GameEngine.Apply(after, new ActAction(PlayerSide.One, "one-mage", "two-archer"));

        Assert.Equal(RuleViolation.AlreadyActed, Assert.IsType<ActionOutcome.Rejected>(move).Code);
        Assert.Equal(RuleViolation.AlreadyActed, Assert.IsType<ActionOutcome.Rejected>(actAgain).Code);
        Assert.IsType<ActionOutcome.Accepted>(otherUnit); // the other units still have their action
    }

    [Fact]
    public void Obstacles_and_units_in_between_do_not_block_an_attack()
    {
        // No line of sight in v1 (PLAN.md, section 1): range is only a distance.
        var state = AsciiBoard.Parse("A # m w");

        var outcome = GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-archer", "two-warrior"));

        Assert.IsType<ActionOutcome.Accepted>(outcome);
    }

    [Fact]
    public void A_finished_game_rejects_every_act()
    {
        var finished = AsciiBoard.Parse("A . . w") with { Status = GameStatus.Finished, Winner = PlayerSide.Two };

        var outcome = GameEngine.Apply(finished, new ActAction(PlayerSide.One, "one-archer", "two-warrior"));

        Assert.Equal(RuleViolation.GameOver, Assert.IsType<ActionOutcome.Rejected>(outcome).Code);
    }

    [Fact]
    public void A_player_cannot_act_on_the_other_players_turn()
    {
        // Turn 1 belongs to Player One. Otherwise it would be a fine hit: the two warriors stand side by side.
        var outcome = GameEngine.Apply(AsciiBoard.Parse("W w"), new ActAction(PlayerSide.Two, "two-warrior", "one-warrior"));

        Assert.Equal(RuleViolation.NotYourTurn, Assert.IsType<ActionOutcome.Rejected>(outcome).Code);
    }

    [Fact]
    public void Acting_with_a_unit_that_is_not_on_the_board_is_rejected()
    {
        var outcome = GameEngine.Apply(AsciiBoard.Parse("A . . w"), new ActAction(PlayerSide.One, "one-knight", "two-warrior"));

        Assert.Equal(RuleViolation.UnitNotFound, Assert.IsType<ActionOutcome.Rejected>(outcome).Code);
    }

    [Fact]
    public void A_player_cannot_act_with_an_enemy_unit()
    {
        var outcome = GameEngine.Apply(AsciiBoard.Parse("W w"), new ActAction(PlayerSide.One, "two-warrior", "one-warrior"));

        Assert.Equal(RuleViolation.NotYourUnit, Assert.IsType<ActionOutcome.Rejected>(outcome).Code);
    }

    [Fact]
    public void A_target_that_is_not_on_the_board_is_rejected()
    {
        var outcome = GameEngine.Apply(AsciiBoard.Parse("A . . w"), new ActAction(PlayerSide.One, "one-archer", "two-knight"));

        Assert.Equal(RuleViolation.TargetNotFound, Assert.IsType<ActionOutcome.Rejected>(outcome).Code);
    }

    [Theory]
    [InlineData("A . W . w", "one-archer", "one-warrior")] // an attacker targets an ally...
    [InlineData("A . . . w", "one-archer", "one-archer")] // ...or itself
    [InlineData("H w . . .", "one-healer", "two-warrior")] // a healer targets an enemy...
    [InlineData("H W . . w", "one-healer", "one-warrior")] // ...or an ally at full HP...
    [InlineData("H . . . w", "one-healer", "one-healer")] // ...or itself at full HP
    public void The_wrong_kind_of_target_is_rejected(string map, string actorId, string targetId)
    {
        var outcome = GameEngine.Apply(AsciiBoard.Parse(map), new ActAction(PlayerSide.One, actorId, targetId));

        Assert.Equal(RuleViolation.InvalidTarget, Assert.IsType<ActionOutcome.Rejected>(outcome).Code);
    }

    [Theory]
    [InlineData("A w . . a", "one-archer", "two-warrior")] // 1 tile: inside the archer's blind spot (range 2–3)
    [InlineData("A . . . w", "one-archer", "two-warrior")] // 4 tiles: too far for the archer
    [InlineData("W . w", "one-warrior", "two-warrior")] // 2 tiles: the warrior only hits adjacent units
    [InlineData("H . . W w", "one-healer", "one-warrior")] // 3 tiles: too far to heal (range 1–2)
    [InlineData("H . . . w", "one-healer", "one-healer")] // 0 tiles: a healer can't heal itself
    public void A_target_at_the_wrong_distance_is_rejected(string map, string actorId, string targetId)
    {
        // Every target has lost 1 HP, so a healer's target is the right kind and only the distance is wrong.
        var state = AsciiBoard.Parse(map).WithUnit(targetId, u => u with { Hp = u.Hp - 1 });

        var outcome = GameEngine.Apply(state, new ActAction(PlayerSide.One, actorId, targetId));

        Assert.Equal(RuleViolation.TargetOutOfRange, Assert.IsType<ActionOutcome.Rejected>(outcome).Code);
    }

    [Fact]
    public void A_unit_with_MinRange_0_can_heal_itself()
    {
        // Content forbids MinRange 0 today, which is why a healer can't heal itself. If you ever allow it,
        // the engine already copes with a unit that is both the actor and the target.
        var parsed = AsciiBoard.Parse("H . w");
        var state = (parsed with
        {
            UnitTypes = parsed.UnitTypes.SetItem("healer", parsed.UnitTypes["healer"] with { MinRange = 0 }),
        }).WithUnit("one-healer", u => u with { Hp = 10 });

        var outcome = GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-healer", "one-healer"));

        var accepted = Assert.IsType<ActionOutcome.Accepted>(outcome);
        Assert.Equal(new UnitHealed("one-healer", "one-healer", Amount: 5, HpAfter: 15), Assert.Single(accepted.Events));
        var healer = accepted.State.FindUnit("one-healer")!;
        Assert.Equal(15, healer.Hp);
        Assert.True(healer.HasActed); // both changes landed on the same unit
    }

    [Fact]
    public void When_several_rules_are_broken_the_first_check_decides_the_code()
    {
        // Who, then what, then where: the actor's own checks first, and the kind of target before its distance.
        var state = AsciiBoard.Parse("A W . . . w");
        var enemyActed = state.WithUnit("two-warrior", u => u with { HasActed = true });
        var archerActed = state.WithUnit("one-archer", u => u with { HasActed = true });

        var notYourUnit = GameEngine.Apply(enemyActed, new ActAction(PlayerSide.One, "two-warrior", "nobody"));
        var alreadyActed = GameEngine.Apply(archerActed, new ActAction(PlayerSide.One, "one-archer", "nobody"));
        var allyTooClose = GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-archer", "one-warrior"));

        Assert.Equal(RuleViolation.NotYourUnit, Assert.IsType<ActionOutcome.Rejected>(notYourUnit).Code);
        Assert.Equal(RuleViolation.AlreadyActed, Assert.IsType<ActionOutcome.Rejected>(alreadyActed).Code);
        Assert.Equal(RuleViolation.InvalidTarget, Assert.IsType<ActionOutcome.Rejected>(allyTooClose).Code); // an ally, 1 tile away
    }

    [Theory]
    [MemberData(nameof(BrawlUnitIds))]
    public void Apply_never_throws_and_every_acceptance_spends_the_actor(string actorId)
    {
        // Everyone has lost 4 HP: allies can be healed, and a warrior's 10 damage defeats a 10-HP mage.
        var start = AsciiBoard.Parse(Brawl);
        var injured = start with { Units = [.. start.Units.Select(u => u with { Hp = u.Hp - 4 })] };
        var actor = injured.FindUnit(actorId)!;
        var state = injured with { CurrentPlayer = actor.Owner }; // give the turn to the actor's side

        var outcomes = state.Units.Select(u => u.Id).Append("nobody")
            .Select(targetId => GameEngine.Apply(state, new ActAction(actor.Owner, actor.Id, targetId)))
            .ToList();

        // The actor itself is fine, so a refusal can only be about the target.
        RuleViolation[] targetCodes = [RuleViolation.TargetNotFound, RuleViolation.InvalidTarget, RuleViolation.TargetOutOfRange];
        Assert.All(outcomes.OfType<ActionOutcome.Rejected>(), rejected => Assert.Contains(rejected.Code, targetCodes));
        Assert.All(outcomes.OfType<ActionOutcome.Accepted>(), accepted => Assert.True(accepted.State.FindUnit(actor.Id)!.HasActed));
        Assert.Contains(outcomes, o => o is ActionOutcome.Accepted); // the board gives every unit both kinds of target
        Assert.Contains(outcomes, o => o is ActionOutcome.Rejected);
    }
}
