using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tactics.Core.Actions;
using Tactics.Core.Events;
using Tactics.Core.Model;
using Tactics.Core.Serialization;

namespace Tactics.Core.Tests.Serialization;

/// <summary>
/// Actions and events are saved and sent as JSON, with a "type" field that says which kind each one is.
/// Each shape is pinned here, character for character, as soon as its type exists. Every type also round-trips,
/// and a guard test fails when a new type is missing its registration or its sample.
/// </summary>
public sealed class ActionEventJsonTests
{
    // One sample of every action and event type, for the round-trip tests.
    // Add a type to Core and the guard test asks for its sample here.
    private static readonly GameAction[] ActionSamples =
    [
        new MoveAction(PlayerSide.One, "one-archer", new Position(1, 5)),
        new ActAction(PlayerSide.One, "one-archer", "two-warrior"),
        new EndTurnAction(PlayerSide.Two),
    ];

    private static readonly GameEvent[] EventSamples =
    [
        new UnitMoved("one-archer", new Position(1, 7), new Position(1, 5), [new(1, 6), new(1, 5)]),
        new UnitDamaged("two-warrior", "one-archer", Amount: 5, HpAfter: 23),
        new UnitHealed("one-warrior", "one-healer", Amount: 5, HpAfter: 25),
        new UnitDefeated("two-mage"),
        new TurnEnded(PlayerSide.One, PlayerSide.Two, TurnNumber: 2),
        new GameWon(PlayerSide.One),
    ];

    public static TheoryData<string> ActionTypes => new(ActionSamples.Select(a => a.GetType().Name));

    public static TheoryData<string> EventTypes => new(EventSamples.Select(e => e.GetType().Name));

    [Fact]
    public void A_move_action_is_written_with_its_type()
    {
        GameAction action = new MoveAction(PlayerSide.One, "one-archer", new Position(1, 5));

        var json = JsonSerializer.Serialize(action, TacticsJson.Options);

        // "player" comes last: System.Text.Json writes a subtype's own properties before the inherited ones.
        Assert.Equal("""{"type":"move","unitId":"one-archer","to":{"x":1,"y":5},"player":"one"}""", json);
        Assert.Equal(action, JsonSerializer.Deserialize<GameAction>(json, TacticsJson.Options));
    }

    [Fact]
    public void The_type_field_can_come_anywhere_in_the_object()
    {
        // PostgreSQL jsonb stores keys shortest first, so a saved move comes back with "to" before "type".
        const string json = """{"to":{"x":1,"y":5},"type":"move","player":"one","unitId":"one-archer"}""";

        var action = JsonSerializer.Deserialize<GameAction>(json, TacticsJson.Options);

        Assert.Equal(new MoveAction(PlayerSide.One, "one-archer", new Position(1, 5)), action);
    }

    [Fact]
    public void An_unknown_type_is_rejected()
    {
        const string json = """{"type":"teleport","player":"one","unitId":"one-archer"}""";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<GameAction>(json, TacticsJson.Options));
    }

    [Fact]
    public void A_unit_moved_event_has_the_contract_shape()
    {
        GameEvent moved = new UnitMoved("one-archer", new Position(1, 7), new Position(1, 5), [new(1, 6), new(1, 5)]);

        var json = JsonSerializer.Serialize(moved, TacticsJson.Options);

        // Character for character the example in PLAN.md, section 3 (Events).
        Assert.Equal(
            """{"type":"unitMoved","unitId":"one-archer","from":{"x":1,"y":7},"to":{"x":1,"y":5},"path":[{"x":1,"y":6},{"x":1,"y":5}]}""",
            json);

        // Records compare lists by reference, so two equal-looking events are not Equal. Compare the JSON instead.
        var back = JsonSerializer.Deserialize<GameEvent>(json, TacticsJson.Options);
        Assert.IsType<UnitMoved>(back);
        Assert.Equal(json, JsonSerializer.Serialize(back, TacticsJson.Options));
    }

    [Fact]
    public void An_act_action_is_written_with_its_type()
    {
        GameAction action = new ActAction(PlayerSide.One, "one-archer", "two-warrior");

        var json = JsonSerializer.Serialize(action, TacticsJson.Options);

        // PLAN.md's request body (section 3), plus the player that the server fills in from the token.
        Assert.Equal("""{"type":"act","actorId":"one-archer","targetId":"two-warrior","player":"one"}""", json);
        Assert.Equal(action, JsonSerializer.Deserialize<GameAction>(json, TacticsJson.Options));
    }

    [Fact]
    public void The_combat_events_have_the_contract_shapes()
    {
        // Character for character the examples in PLAN.md, section 3 (Events).
        (GameEvent Event, string Json)[] cases =
        [
            (new UnitDamaged("two-warrior", "one-archer", Amount: 5, HpAfter: 23),
                """{"type":"unitDamaged","unitId":"two-warrior","byUnitId":"one-archer","amount":5,"hpAfter":23}"""),
            (new UnitHealed("one-warrior", "one-healer", Amount: 5, HpAfter: 25),
                """{"type":"unitHealed","unitId":"one-warrior","byUnitId":"one-healer","amount":5,"hpAfter":25}"""),
            (new UnitDefeated("two-mage"),
                """{"type":"unitDefeated","unitId":"two-mage"}"""),
        ];

        Assert.All(cases, c =>
        {
            Assert.Equal(c.Json, JsonSerializer.Serialize(c.Event, TacticsJson.Options));

            // These events hold no list, so plain record equality works on the way back (unlike UnitMoved).
            Assert.Equal(c.Event, JsonSerializer.Deserialize<GameEvent>(c.Json, TacticsJson.Options));
        });
    }

    [Fact]
    public void An_end_turn_action_is_written_with_its_type()
    {
        GameAction action = new EndTurnAction(PlayerSide.One);

        var json = JsonSerializer.Serialize(action, TacticsJson.Options);

        // PLAN.md's request body is { "type": "endTurn" }, plus the player that the server fills in from the token.
        Assert.Equal("""{"type":"endTurn","player":"one"}""", json);
        Assert.Equal(action, JsonSerializer.Deserialize<GameAction>(json, TacticsJson.Options));
    }

    [Fact]
    public void The_turn_and_victory_events_have_the_contract_shapes()
    {
        // Character for character the examples in PLAN.md, section 3 (Events).
        (GameEvent Event, string Json)[] cases =
        [
            (new TurnEnded(PlayerSide.One, PlayerSide.Two, TurnNumber: 2),
                """{"type":"turnEnded","previousPlayer":"one","nextPlayer":"two","turnNumber":2}"""),
            (new GameWon(PlayerSide.One),
                """{"type":"gameWon","winner":"one"}"""),
        ];

        Assert.All(cases, c =>
        {
            Assert.Equal(c.Json, JsonSerializer.Serialize(c.Event, TacticsJson.Options));
            Assert.Equal(c.Event, JsonSerializer.Deserialize<GameEvent>(c.Json, TacticsJson.Options));
        });
    }

    [Theory]
    [MemberData(nameof(ActionTypes))]
    public void Every_action_type_round_trips(string typeName)
    {
        AssertRoundTrip(ActionSamples.Single(a => a.GetType().Name == typeName));
    }

    [Theory]
    [MemberData(nameof(EventTypes))]
    public void Every_event_type_round_trips(string typeName)
    {
        AssertRoundTrip(EventSamples.Single(e => e.GetType().Name == typeName));
    }

    [Fact]
    public void Every_action_and_event_type_is_registered_and_has_a_sample()
    {
        // A type without [JsonDerivedType] can't be written as its base type: System.Text.Json throws at run time.
        // A type without a sample is never round-tripped. Both mistakes fail here, as soon as the type exists.
        AssertEveryTypeIsCovered(ActionSamples);
        AssertEveryTypeIsCovered(EventSamples);
    }

    [Fact]
    public void Written_as_its_own_type_an_action_has_no_type_field()
    {
        // The "type" field only appears when the declared type is the base type (GameAction, GameEvent).
        // A trap for later steps: write lists and variables of the base type.
        var move = new MoveAction(PlayerSide.One, "one-archer", new Position(1, 5));

        var json = JsonSerializer.Serialize(move, TacticsJson.Options);

        Assert.DoesNotContain("\"type\"", json, StringComparison.Ordinal);
    }

    // Write the sample as its base type, read it back, and write it again: same type, same JSON. Comparing the
    // JSON instead of the records also works for UnitMoved, whose path is a list.
    private static void AssertRoundTrip<TBase>(TBase sample)
        where TBase : class
    {
        var json = JsonSerializer.Serialize(sample, TacticsJson.Options);
        var back = JsonSerializer.Deserialize<TBase>(json, TacticsJson.Options);

        Assert.StartsWith("{\"type\":\"", json, StringComparison.Ordinal); // the discriminator comes first
        Assert.IsType(sample.GetType(), back);
        Assert.Equal(json, JsonSerializer.Serialize(back, TacticsJson.Options));
    }

    // Three lists of type names must match: the subtypes Core defines, the ones registered on the base type
    // with [JsonDerivedType], and the ones with a sample above.
    private static void AssertEveryTypeIsCovered<TBase>(IEnumerable<TBase> samples)
        where TBase : class
    {
        var defined = typeof(TBase).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(TBase)) && !t.IsAbstract)
            .Select(t => t.Name)
            .Order();
        var registered = typeof(TBase).GetCustomAttributes<JsonDerivedTypeAttribute>()
            .Select(a => a.DerivedType.Name)
            .Order();
        var sampled = samples.Select(s => s.GetType().Name).Order();

        Assert.Equal(defined, registered);
        Assert.Equal(defined, sampled);
    }
}
