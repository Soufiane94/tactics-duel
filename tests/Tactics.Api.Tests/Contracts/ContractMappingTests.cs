using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tactics.Api.Contracts;
using Tactics.Core.Actions;
using Tactics.Core.Events;
using Tactics.Core.Model;
using Tactics.Core.Serialization;

namespace Tactics.Api.Tests.Contracts;

/// <summary>
/// Guards between the two worlds. Core and the wire contract list their types separately, on purpose, so these
/// tests fail when one side gets a new action or event and the other doesn't.
/// </summary>
public sealed class ContractMappingTests
{
    // One of each Core event, with every field set to a value no other field has.
    private static readonly GameEvent[] EventSamples =
    [
        new UnitMoved("one-warrior", new Position(1, 2), new Position(2, 3), [new Position(2, 2), new Position(2, 3)]),
        new UnitDamaged("two-mage", "one-warrior", Amount: 10, HpAfter: 4),
        new UnitHealed("one-warrior", "one-healer", Amount: 5, HpAfter: 25),
        new UnitDefeated("two-mage"),
        new TurnEnded(PlayerSide.One, PlayerSide.Two, TurnNumber: 2),
        new GameWon(PlayerSide.One),
    ];

    // One of each action DTO, with the Core action it must become when Player Two sends it.
    private static readonly (ActionDto Dto, GameAction Core)[] ActionSamples =
    [
        (new MoveActionDto("two-mage", new PositionDto(0, 1)), new MoveAction(PlayerSide.Two, "two-mage", new Position(0, 1))),
        (new ActActionDto("two-mage", "one-warrior"), new ActAction(PlayerSide.Two, "two-mage", "one-warrior")),
        (new EndTurnActionDto(), new EndTurnAction(PlayerSide.Two)),
    ];

    public static TheoryData<GameEvent> Events => new(EventSamples);

    public static TheoryData<int> ActionIndexes => new(Enumerable.Range(0, ActionSamples.Length));

    [Fact]
    public void The_action_DTOs_have_the_same_type_names_as_the_Core_actions()
    {
        Assert.Equal(Discriminators<GameAction>(), Discriminators<ActionDto>());
    }

    [Fact]
    public void The_event_DTOs_have_the_same_type_names_as_the_Core_events()
    {
        Assert.Equal(Discriminators<GameEvent>(), Discriminators<EventDto>());
    }

    [Theory]
    [MemberData(nameof(Events))]
    public void An_event_keeps_its_exact_JSON_when_it_becomes_a_DTO(GameEvent coreEvent)
    {
        // Core's event JSON is pinned by the Core tests (Step 4). Equal JSON proves no field was dropped or swapped.
        var coreJson = JsonSerializer.Serialize(coreEvent, TacticsJson.Options);
        var dtoJson = JsonSerializer.Serialize(coreEvent.ToDto(), TacticsJson.Options);

        Assert.Equal(coreJson, dtoJson);
    }

    [Theory]
    [MemberData(nameof(ActionIndexes))]
    public void An_action_DTO_becomes_the_Core_action_of_the_player_from_the_token(int index)
    {
        var (dto, expected) = ActionSamples[index];

        Assert.Equal(expected, dto.ToCore(PlayerSide.Two));
    }

    [Fact]
    public void The_samples_cover_every_event_and_every_action_DTO()
    {
        Assert.Equal(ConcreteTypesOf<GameEvent>(), EventSamples.Select(e => e.GetType().Name).Order(StringComparer.Ordinal));
        Assert.Equal(ConcreteTypesOf<ActionDto>(), ActionSamples.Select(s => s.Dto.GetType().Name).Order(StringComparer.Ordinal));
    }

    // The "type" names a polymorphic base type declares with [JsonDerivedType], sorted.
    private static IEnumerable<string> Discriminators<TBase>() =>
        typeof(TBase).GetCustomAttributes<JsonDerivedTypeAttribute>()
            .Select(attribute => attribute.TypeDiscriminator?.ToString() ?? "")
            .Order(StringComparer.Ordinal);

    // Every non-abstract type that derives from TBase in TBase's assembly, by name, sorted.
    private static IEnumerable<string> ConcreteTypesOf<TBase>() =>
        typeof(TBase).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(TBase)) && !type.IsAbstract)
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal);
}
