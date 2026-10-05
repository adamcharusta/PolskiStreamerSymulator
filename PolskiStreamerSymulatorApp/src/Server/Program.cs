using PolskiStreamerSymulatorApp.Application;
using PolskiStreamerSymulatorApp.Infrastructure;
using PolskiStreamerSymulatorApp.Server.Health;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

app.MapHealthEndpoints();

app.Run();
