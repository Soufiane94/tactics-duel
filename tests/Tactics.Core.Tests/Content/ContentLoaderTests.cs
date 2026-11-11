using System.Text.Json.Nodes;
using Tactics.Core.Content;
using Tactics.Core.Model;

namespace Tactics.Core.Tests.Content;

/// <summary>
/// Each test starts from small valid files, breaks exactly one thing, and checks the error code
/// plus the words that point you to the culprit.
/// </summary>
public sealed class ContentLoaderTests
{
    private const string ValidUnits = """
        {
          "unitTypes": [
            { "id": "warrior", "name": "Warrior", "maxHp": 28, "power": 10, "defense": 3, "move": 3, "minRange": 1, "maxRange": 1, "ability": "damage" },
            { "id": "healer",  "name": "Healer",  "maxHp": 16, "power": 5,  "defense": 0, "move": 3, "minRange": 1, "maxRange": 2, "ability": "heal" }
          ]
        }
        """;

    // A 4×4 board with one obstacle at (1, 1).
    private const string ValidScenario = """
        {
          "id": "test",
          "name": "Test",
          "board": { "width": 4, "height": 4, "obstacles": [ { "x": 1, "y": 1 } ] },
          "placements": [
            { "id": "one-warrior", "typeId": "warrior", "owner": "one", "x": 0, "y": 3 },
            { "id": "one-healer",  "typeId": "healer",  "owner": "one", "x": 1, "y": 3 },
            { "id": "two-warrior", "typeId": "warrior", "owner": "two", "x": 3, "y": 0 }
          ]
        }
        """;

    // ---- The happy path -----------------------------------------------------------------------

    [Fact]
    public void Valid_files_become_domain_records()
    {
        var setup = ContentLoader.Load(ValidUnits, ValidScenario);

        Assert.Equal("test", setup.ScenarioId);
        Assert.Equal(new UnitType("healer", "Healer", 16, 5, 0, 3, 1, 2, Ability.Heal), setup.UnitTypes["healer"]);
        Assert.Equal(4, setup.Board.Width);
        Assert.Equal(4, setup.Board.Height);
        Assert.True(setup.Board.IsObstacle(new Position(1, 1)));
        Assert.Equal(
            new UnitPlacement("one-warrior", "warrior", PlayerSide.One, new Position(0, 3)),
            setup.Placements[0]);
        Assert.Equal(3, setup.Placements.Count);
    }

    [Fact]
    public void Zero_defense_is_valid()
    {
        // The placeholder Mage has defense 0, so "every stat must be positive" can't apply to defense.
        var setup = ContentLoader.Load(ValidUnits, ValidScenario);

        Assert.Equal(0, setup.UnitTypes["healer"].Defense);
    }

    [Fact]
    public void Enum_names_ignore_case()
    {
        var scenario = Scenario();
        scenario["placements"]![0]!["owner"] = "One";

        var setup = ContentLoader.Load(ValidUnits, scenario.ToJsonString());

        Assert.Equal(PlayerSide.One, setup.Placements[0].Owner);
    }

    // ---- Broken JSON --------------------------------------------------------------------------

    [Fact]
    public void Malformed_json_reports_the_line()
    {
        const string units = """
            {
              "unitTypes": [
                { "id": "warrior" "name": "Warrior" }
              ]
            }
            """; // a comma is missing on line 3

        var error = SingleError(units, ValidScenario);

        Assert.Equal(ContentErrorCode.InvalidJson, error.Code);
        Assert.Contains("Units file", error.Message);
        Assert.Contains("line 3", error.Message);
    }

    [Fact]
    public void An_empty_file_is_invalid_json()
    {
        var error = SingleError(ValidUnits, "");

        Assert.Equal(ContentErrorCode.InvalidJson, error.Code);
        Assert.Contains("Scenario file", error.Message);
    }

    [Fact]
    public void A_value_of_the_wrong_type_is_reported_with_its_path()
    {
        var units = Units();
        units["unitTypes"]![0]!["maxHp"] = "lots";

        var error = SingleError(units, Scenario());

        Assert.Equal(ContentErrorCode.InvalidJson, error.Code);
        Assert.Contains("maxHp", error.Message);
        Assert.Contains("wrong type", error.Message);
    }

    // ---- Units file ---------------------------------------------------------------------------

    [Fact]
    public void A_missing_stat_is_named()
    {
        var units = Units();
        units["unitTypes"]![0]!.AsObject().Remove("maxHp");

        var error = SingleError(units, Scenario());

        Assert.Equal(ContentErrorCode.MissingField, error.Code);
        Assert.Contains("unit type \"warrior\" is missing \"maxHp\"", error.Message);
    }

    [Theory]
    [InlineData("maxHp", 0)]
    [InlineData("power", 0)]
    [InlineData("move", 0)]
    [InlineData("minRange", 0)]
    [InlineData("maxRange", 0)]
    [InlineData("defense", -1)]
    public void A_stat_below_its_minimum_is_rejected(string stat, int value)
    {
        var units = Units();
        units["unitTypes"]![0]![stat] = value;

        // Single error: the warrior's placements don't add an "unknown unit type" error on top.
        var error = SingleError(units, Scenario());

        Assert.Equal(ContentErrorCode.InvalidStat, error.Code);
        Assert.Contains($"\"{stat}\" must be at least", error.Message);
    }

    [Fact]
    public void Min_range_above_max_range_is_rejected()
    {
        var units = Units();
        units["unitTypes"]![1]!["minRange"] = 3;
        units["unitTypes"]![1]!["maxRange"] = 2;

        var error = SingleError(units, Scenario());

        Assert.Equal(ContentErrorCode.RangeOrder, error.Code);
        Assert.Contains("\"minRange\" (3) is greater than \"maxRange\" (2)", error.Message);
    }

    [Fact]
    public void An_unknown_ability_lists_the_allowed_ones()
    {
        var units = Units();
        units["unitTypes"]![1]!["ability"] = "fly";

        var error = SingleError(units, Scenario());

        Assert.Equal(ContentErrorCode.InvalidValue, error.Code);
        Assert.Contains("\"damage\" or \"heal\" (it is \"fly\")", error.Message);
    }

    [Fact]
    public void Two_unit_types_with_the_same_id_are_rejected()
    {
        var units = Units();
        var types = units["unitTypes"]!.AsArray();
        types.Add(types[0]!.DeepClone()); // a second "warrior"

        var error = SingleError(units, Scenario());

        Assert.Equal(ContentErrorCode.DuplicateId, error.Code);
        Assert.Contains("\"warrior\"", error.Message);
    }

    // ---- Scenario file ------------------------------------------------------------------------

    [Fact]
    public void A_missing_board_is_named()
    {
        var scenario = Scenario();
        scenario.AsObject().Remove("board");

        var error = SingleError(Units(), scenario);

        Assert.Equal(ContentErrorCode.MissingField, error.Code);
        Assert.Contains("is missing \"board\"", error.Message);
    }

    [Fact]
    public void A_board_size_below_1_is_rejected()
    {
        var scenario = Scenario();
        scenario["board"]!["width"] = 0;

        var error = SingleError(Units(), scenario);

        Assert.Equal(ContentErrorCode.InvalidValue, error.Code);
        Assert.Contains("\"width\" must be at least 1", error.Message);
    }

    [Fact]
    public void A_missing_placements_list_is_named()
    {
        var scenario = Scenario();
        scenario.AsObject().Remove("placements");

        var error = SingleError(Units(), scenario);

        Assert.Equal(ContentErrorCode.MissingField, error.Code);
        Assert.Contains("is missing \"placements\"", error.Message);
    }

    [Fact]
    public void An_unknown_owner_lists_the_allowed_ones()
    {
        var scenario = Scenario();
        scenario["placements"]![0]!["owner"] = "three";

        var error = SingleError(Units(), scenario);

        Assert.Equal(ContentErrorCode.InvalidValue, error.Code);
        Assert.Contains("\"one\" or \"two\" (it is \"three\")", error.Message);
    }

    [Fact]
    public void An_unknown_unit_type_lists_the_known_ones()
    {
        var scenario = Scenario();
        scenario["placements"]![0]!["typeId"] = "warior";

        var error = SingleError(Units(), scenario);

        Assert.Equal(ContentErrorCode.UnknownUnitType, error.Code);
        Assert.Contains("unknown unit type \"warior\". Known types: healer, warrior.", error.Message);
    }

    [Fact]
    public void Two_placements_with_the_same_id_are_rejected()
    {
        var scenario = Scenario();
        scenario["placements"]![1]!["id"] = "one-warrior";

        var error = SingleError(Units(), scenario);

        Assert.Equal(ContentErrorCode.DuplicateId, error.Code);
        Assert.Contains("\"one-warrior\"", error.Message);
    }

    [Fact]
    public void A_placement_off_the_board_shows_the_bounds()
    {
        var scenario = Scenario();
        scenario["placements"]![0]!["x"] = 4; // the board is 4 wide: x goes from 0 to 3

        var error = SingleError(Units(), scenario);

        Assert.Equal(ContentErrorCode.OffBoard, error.Code);
        Assert.Contains("\"one-warrior\" at (4, 3) is off the board", error.Message);
        Assert.Contains("x goes from 0 to 3", error.Message);
    }

    [Fact]
    public void An_obstacle_off_the_board_is_rejected()
    {
        var scenario = Scenario();
        scenario["board"]!["obstacles"]!.AsArray().Add(new JsonObject { ["x"] = 9, ["y"] = 9 });

        var error = SingleError(Units(), scenario);

        Assert.Equal(ContentErrorCode.OffBoard, error.Code);
        Assert.Contains("obstacle at (9, 9)", error.Message);
    }

    [Fact]
    public void A_placement_on_an_obstacle_is_rejected()
    {
        var scenario = Scenario();
        scenario["placements"]![0]!["x"] = 1;
        scenario["placements"]![0]!["y"] = 1;

        var error = SingleError(Units(), scenario);

        Assert.Equal(ContentErrorCode.OnObstacle, error.Code);
        Assert.Contains("\"one-warrior\" at (1, 1) is on an obstacle", error.Message);
    }

    [Fact]
    public void Two_placements_on_one_tile_name_both_units()
    {
        var scenario = Scenario();
        scenario["placements"]![1]!["x"] = 0; // the healer moves onto the warrior's tile (0, 3)

        var error = SingleError(Units(), scenario);

        Assert.Equal(ContentErrorCode.Overlap, error.Code);
        Assert.Contains("\"one-warrior\" and \"one-healer\" are both at (0, 3)", error.Message);
    }

    [Fact]
    public void A_side_without_units_is_rejected()
    {
        var scenario = Scenario();
        scenario["placements"]!.AsArray().RemoveAt(2); // the only unit of player two

        var error = SingleError(Units(), scenario);

        Assert.Equal(ContentErrorCode.EmptySide, error.Code);
        Assert.Contains("player \"two\" has no units", error.Message);
    }

    // ---- Several problems ---------------------------------------------------------------------

    [Fact]
    public void Every_problem_is_reported_at_once()
    {
        var units = Units();
        units["unitTypes"]![0]!["maxHp"] = 0;
        var scenario = Scenario();
        scenario["placements"]![1]!["x"] = 9;

        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(units.ToJsonString(), scenario.ToJsonString()));

        Assert.Equal([ContentErrorCode.InvalidStat, ContentErrorCode.OffBoard], ex.Errors.Select(e => e.Code).ToArray());
        Assert.StartsWith("The game content has 2 problems:", ex.Message);
        Assert.All(ex.Errors, e => Assert.Contains(e.Message, ex.Message));
    }

    // ---- Helpers ------------------------------------------------------------------------------

    private static JsonNode Units() => JsonNode.Parse(ValidUnits)!;

    private static JsonNode Scenario() => JsonNode.Parse(ValidScenario)!;

    private static ContentError SingleError(JsonNode units, JsonNode scenario) =>
        SingleError(units.ToJsonString(), scenario.ToJsonString());

    private static ContentError SingleError(string units, string scenario)
    {
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(units, scenario));
        return Assert.Single(ex.Errors);
    }
}
