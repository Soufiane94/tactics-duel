using System.Text.Json;
using Tactics.Core.Actions;
using Tactics.Core.Events;
using Tactics.Core.Model;
using Tactics.Core.Serialization;

namespace Tactics.Core.Tests.Serialization;

/// <summary>
/// Actions and events are saved and sent as JSON, with a "type" field that says which kind each one is.
/// Step 4 adds a round trip for every action and event type; these tests pin the first two shapes.
/// </summary>
public sealed class ActionEventJsonTests
{
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
    public void Written_as_its_own_type_an_action_has_no_type_field()
    {
        // The "type" field only appears when the declared type is the base type (GameAction, GameEvent).
        // A trap for later steps: write lists and variables of the base type.
        var move = new MoveAction(PlayerSide.One, "one-archer", new Position(1, 5));

        var json = JsonSerializer.Serialize(move, TacticsJson.Options);

        Assert.DoesNotContain("\"type\"", json, StringComparison.Ordinal);
    }
}
