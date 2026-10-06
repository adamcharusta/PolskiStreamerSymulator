using PolskiStreamerSymulatorApp.Application;
using PolskiStreamerSymulatorApp.Infrastructure;
using PolskiStreamerSymulatorApp.Server.Health;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.MapHealthEndpoints();
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();
