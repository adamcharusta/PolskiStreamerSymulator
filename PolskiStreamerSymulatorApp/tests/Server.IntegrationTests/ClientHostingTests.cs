using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PolskiStreamerSymulatorApp.Server.IntegrationTests;

public sealed partial class ClientHostingTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task RootServesHostPageThatLoadsBlazorBootScript()
    {
        using HttpClient client = factory.CreateClient();

        string hostPage = await GetHostPageAsync(client, "/");

        Assert.Contains("<script src=\"_framework/blazor.webassembly.js\"></script>", hostPage, StringComparison.Ordinal);
        Assert.DoesNotContain("#[.{fingerprint}]", hostPage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EveryLocalAssetReferencedByHostPageIsServed()
    {
        using HttpClient client = factory.CreateClient();
        string hostPage = await GetHostPageAsync(client, "/");
        string[] assetPaths = [.. LocalAssetReference().Matches(hostPage).Select(match => match.Groups["path"].Value)];

        Assert.NotEmpty(assetPaths);
        foreach (string assetPath in assetPaths)
        {
            using HttpResponseMessage response = await client.GetAsync(assetPath, TestContext.Current.CancellationToken);

            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{assetPath} returned {(int)response.StatusCode}.");
        }
    }

    [Theory]
    [InlineData("/counter")]
    [InlineData("/career/week/3")]
    public async Task ClientRouteFallsBackToHostPage(string path)
    {
        using HttpClient client = factory.CreateClient();

        await GetHostPageAsync(client, path);
    }

    [Fact]
    public async Task MissingStaticFileReturnsNotFound()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/_framework/blazor.webassembly.outdated.js", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<string> GetHostPageAsync(HttpClient client, string path)
    {
        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("<div id=\"app\">", body, StringComparison.Ordinal);

        return body;
    }

    [GeneratedRegex(@"(?:href|src)=""(?<path>(?!https?:|//|#|\.)[^""]+)""")]
    private static partial Regex LocalAssetReference();
}
