using Tactics.Core.Content;
using Tactics.Core.Model;
using Tactics.Core.Tests.Helpers;

namespace Tactics.Core.Tests.Content;

/// <summary>
/// Checks the real, hand-edited files in content/. These tests check validity only, never values:
/// rebalancing a unit must not break a test, but a typo must.
/// </summary>
public sealed class RealContentTests
{
    [Fact]
    public void The_real_content_files_load_and_pass_validation()
    {
        // If a hand edit broke a file, this fails with the list of problems (see ContentException.Message).
        var setup = ContentLoader.Load(RealContent.UnitsJson, RealContent.ClassicScenarioJson);

        Assert.Equal("classic", setup.ScenarioId); // the API uses this id as the default scenario (Step 5)
        Assert.NotEmpty(setup.UnitTypes);
        Assert.NotEmpty(setup.Placements);
    }

    [Fact]
    public void A_game_can_start_from_the_real_content()
    {
        var setup = ContentLoader.Load(RealContent.UnitsJson, RealContent.ClassicScenarioJson);

        var state = GameEngine.CreateInitialState(setup);

        Assert.Equal(setup.Placements.Count, state.Units.Count);
        Assert.NotEmpty(state.UnitsOf(PlayerSide.One));
        Assert.NotEmpty(state.UnitsOf(PlayerSide.Two));
    }
}
