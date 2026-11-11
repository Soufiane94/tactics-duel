using Tactics.Api.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Aspire defaults: OpenTelemetry (logs, traces, metrics → dashboard), health checks,
// service discovery and HTTP resilience. Defined in Tactics.ServiceDefaults.
builder.AddServiceDefaults();

var app = builder.Build();

// /health and /alive (Development only). Aspire uses them to show the API as "Healthy".
app.MapDefaultEndpoints();

// Every game endpoint lives under /api, so the Vite dev server can proxy one prefix.
var api = app.MapGroup("/api");

// Smoke endpoint: proves browser → Vite proxy → API works. The server never needs the game title.
api.MapGet("/hello", () => TypedResults.Ok(new HelloResponse("API is alive")));

app.Run();
