using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace PolskiStreamerSymulatorApp.Server.Health;

/// <summary>
/// Maps the liveness and readiness probes that Kubernetes calls.
/// </summary>
internal static class HealthEndpoints
{
    /// <summary>
    /// Tag for health checks that must pass before the server receives player traffic.
    /// </summary>
    public const string ReadyTag = "ready";

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = static _ => false,
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = static registration => registration.Tags.Contains(ReadyTag),
        });

        return endpoints;
    }
}
