using Microsoft.AspNetCore.Mvc.Testing;
using Tactics.Api.Games;
using Tactics.Api.Tests.Helpers;
using Tactics.Core.Model;

namespace Tactics.Api.Tests;

/// <summary>
/// The API exactly as it ships, with the real <c>content/</c> files: it starts, and new games use them.
/// No stat or position appears here, so balancing <c>content/</c> never breaks this test.
/// </summary>
public sealed class RealContentStartupTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task New_games_start_from_the_real_content()
    {
        var ct = TestContext.Current.CancellationToken;
        using var http = factory.CreateClient();
        var real = SetupProvider.LoadFrom(Path.Combine(AppContext.BaseDirectory, "content")).Default;

        var created = await Lobby.CreateGameAsync(http, ct);

        var view = created.View;
        Assert.Equal((real.Board.Width, real.Board.Height), (view.Board.Width, view.Board.Height));
        Assert.Equal(real.Placements.Select(p => p.Id), view.Units.Select(u => u.Id));
        Assert.Equal(real.UnitTypes.Keys.Order(StringComparer.Ordinal), view.UnitTypes.Select(t => t.Id));
        Assert.Contains(view.Units, unit => unit.Owner == PlayerSide.Two);
    }
}
