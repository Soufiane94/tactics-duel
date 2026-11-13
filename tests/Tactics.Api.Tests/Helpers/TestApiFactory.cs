using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tactics.Api.Games;

namespace Tactics.Api.Tests.Helpers;

/// <summary>
/// The real API, running in memory (no network, no port), with one change: new games start from
/// <see cref="TestScenario"/> instead of <c>content/</c>. Swapping one registration is how tests replace a
/// dependency, like a mock provider in an Angular TestBed.
/// </summary>
public class TestApiFactory : WebApplicationFactory<Program>
{
    /// <summary>The ASP.NET Core environment the API runs in. Development, like <c>aspire run</c>.</summary>
    protected virtual string EnvironmentName => "Development";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<SetupProvider>();
            services.AddSingleton(new SetupProvider(TestScenario.Setup));
        });
    }
}

/// <summary>The same API in the Production environment, as it will run in the Docker image (Step 13).</summary>
public sealed class ProductionApiFactory : TestApiFactory
{
    /// <inheritdoc />
    protected override string EnvironmentName => "Production";
}
