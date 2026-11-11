namespace Tactics.Api.Contracts;

/// <summary>Body of <c>GET /api/hello</c>. Serialized as <c>{ "message": "..." }</c> (camelCase).</summary>
public sealed record HelloResponse(string Message);
