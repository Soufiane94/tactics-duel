using Microsoft.Extensions.DependencyInjection.Extensions;
using Scalar.AspNetCore;
using Tactics.Api.Contracts;
using Tactics.Api.Games;
using Tactics.Api.Http;

var builder = WebApplication.CreateBuilder(args);

// Aspire defaults: OpenTelemetry (logs, traces, metrics → dashboard), health checks,
// service discovery and HTTP resilience. Defined in Tactics.ServiceDefaults.
builder.AddServiceDefaults();

// Every error is a ProblemDetails JSON body (RFC 9457), even a 404 for an unknown URL or a crash.
// Every 401 also gets its "WWW-Authenticate: Bearer" header there.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ApiProblems.AddChallenge);

// JSON on the wire: Core's conventions (camelCase, enums as strings), and strict reading of requests (ContractJson).
builder.Services.ConfigureHttpJsonOptions(options => ContractJson.Configure(options.SerializerOptions));

// The OpenAPI document (/openapi/v1.json) that Scalar reads. It declares the player token, so Scalar can send it.
builder.Services.AddOpenApi(options => options.AddPlayerTokenScheme());

// The game server. GameService is scoped (one per request) because Step 12's database repository will be.
builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.AddSingleton(_ => SetupProvider.LoadFrom(Path.Combine(AppContext.BaseDirectory, "content")));
builder.Services.AddSingleton<IGameRepository, InMemoryGameRepository>();
builder.Services.AddScoped<GameService>();

var app = builder.Build();

// Load content/ now, not on the first request: broken content stops the API right here, with every problem listed.
_ = app.Services.GetRequiredService<SetupProvider>();

// Crashes and empty error responses (404, 405…) become ProblemDetails too. In Development, the developer exception
// page (added automatically) answers instead, with the details: which JSON field was missing, or a stack trace.
// It also keeps a malformed request at 400, where UseExceptionHandler would report a 500.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.UseStatusCodePages();

// /health and /alive (Development only). Aspire uses them to show the API as "Healthy".
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    // /openapi/v1.json, and the Scalar UI at /scalar to try the API from a browser.
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .AddPreferredSecuritySchemes([PlayerTokenOpenApi.SchemeName])
        // A local dev tool: no telemetry, no AI chat, no MCP server.
        .DisableTelemetry()
        .DisableAgent()
        .DisableMcp());
}

// Every game endpoint lives under /api, so the Vite dev server can proxy one prefix.
var api = app.MapGroup("/api");

// Smoke endpoint: proves browser → Vite proxy → API works. The server never needs the game title.
api.MapGet("/hello", () => TypedResults.Ok(new HelloResponse("API is alive")));

// POST /games · POST /games/{gameId}/join · GET /games/{gameId} · POST /games/{gameId}/actions
api.MapGames();

app.Run();
