// Aspire AppHost: describes the whole dev environment in C# and starts it with `aspire run`.
// Resource names ("api", "client") are infrastructure names: they never contain the game title.
var builder = DistributedApplication.CreateBuilder(args);

// The game server (ASP.NET Core). Aspire runs it like `dotnet run` and shows its logs,
// traces and metrics in the dashboard. The health check lets WaitFor wait for "Healthy".
// It probes the plain-http endpoint so it works even when the local HTTPS dev certificate
// isn't trusted (fresh machines, CI). Nothing leaves localhost.
var api = builder.AddProject<Projects.Tactics_Api>("api")
    .WithHttpHealthCheck("/health", endpointName: "http");

// The browser client (Vite dev server, npm install runs automatically).
// WithReference injects the API's URL (API_HTTP, API_HTTPS) so vite.config.ts can proxy /api.
// WaitFor starts the client only once the API is healthy.
builder.AddViteApp("client", "../../client")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
