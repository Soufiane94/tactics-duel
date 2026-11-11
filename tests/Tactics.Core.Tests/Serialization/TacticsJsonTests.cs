using System.Text.Json;
using Tactics.Core.Model;
using Tactics.Core.Serialization;

namespace Tactics.Core.Tests.Serialization;

public sealed class TacticsJsonTests
{
    [Theory]
    [InlineData(PlayerSide.One, "\"one\"")]
    [InlineData(PlayerSide.Two, "\"two\"")]
    public void Player_sides_are_camelCase_strings(PlayerSide side, string expectedJson)
    {
        Assert.Equal(expectedJson, JsonSerializer.Serialize(side, TacticsJson.Options));
    }

    [Fact]
    public void Multi_word_enum_values_are_camelCase()
    {
        Assert.Equal("\"inProgress\"", JsonSerializer.Serialize(GameStatus.InProgress, TacticsJson.Options));
    }

    [Fact]
    public void Enum_names_read_back_into_values()
    {
        Assert.Equal(Ability.Heal, JsonSerializer.Deserialize<Ability>("\"heal\"", TacticsJson.Options));
    }

    [Fact]
    public void Enum_values_written_as_numbers_are_rejected()
    {
        // "owner": 0 would be unreadable in a hand-edited file.
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PlayerSide>("0", TacticsJson.Options));
    }

    [Fact]
    public void A_unit_round_trips_with_the_contract_shape()
    {
        var unit = new Unit("one-archer", "archer", PlayerSide.One, new Position(1, 7), Hp: 18, HasMoved: false, HasActed: false);

        var json = JsonSerializer.Serialize(unit, TacticsJson.Options);

        Assert.Equal(
            """{"id":"one-archer","typeId":"archer","owner":"one","position":{"x":1,"y":7},"hp":18,"hasMoved":false,"hasActed":false}""",
            json);
        Assert.Equal(unit, JsonSerializer.Deserialize<Unit>(json, TacticsJson.Options));
    }

    [Fact]
    public void The_shared_options_are_read_only()
    {
        Assert.True(TacticsJson.Options.IsReadOnly);
    }
}
