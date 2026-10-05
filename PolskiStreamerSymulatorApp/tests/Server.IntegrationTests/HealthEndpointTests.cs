using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PolskiStreamerSymulatorApp.Server.IntegrationTests;

public sealed class HealthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpointReportsHealthyWhenNoCheckFails(string path)
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("/health/live", HttpStatusCode.OK)]
    [InlineData("/health/ready", HttpStatusCode.ServiceUnavailable)]
    public async Task FailingReadyCheckFailsReadinessButNotLiveness(string path, HttpStatusCode expectedStatus)
    {
        await using WebApplicationFactory<Program> failingFactory = WithFailingCheck(["ready"]);
        using HttpClient client = failingFactory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task FailingUntaggedCheckFailsNeitherEndpoint(string path)
    {
        await using WebApplicationFactory<Program> failingFactory = WithFailingCheck([]);
        using HttpClient client = failingFactory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private WebApplicationFactory<Program> WithFailingCheck(string[] tags)
    {
        return factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => services
            .AddHealthChecks()
            .AddCheck("simulated-failure", () => HealthCheckResult.Unhealthy("Simulated dependency failure."), tags)));
    }
}
