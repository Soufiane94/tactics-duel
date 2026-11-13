using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Tactics.Api.Http;

/// <summary>
/// Tells the OpenAPI document, and so Scalar, which endpoints need a player token. Documentation only: it lets
/// Scalar show a token field and send the header. <c>GameService</c> does the real check.
/// </summary>
public static class PlayerTokenOpenApi
{
    /// <summary>The security scheme's name in the OpenAPI document.</summary>
    public const string SchemeName = "playerToken";

    /// <summary>Declares the player token once, as an HTTP Bearer scheme.</summary>
    public static OpenApiOptions AddPlayerTokenScheme(this OpenApiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                Description = "The playerToken you got when you created or joined the game. "
                    + "Sent as \"Authorization: Bearer <token>\".",
            };
            return Task.CompletedTask;
        });
    }

    /// <summary>Marks one endpoint as needing the token, so Scalar sends it with that endpoint's requests.</summary>
    public static TBuilder WithPlayerToken<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddOpenApiOperationTransformer((operation, context, _) =>
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeName, context.Document)] = [],
            });
            return Task.CompletedTask;
        });
}
