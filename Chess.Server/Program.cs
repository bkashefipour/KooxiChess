using Chess.Server.BackgroundServices;
using Chess.Server.Data;
using Chess.Server.Hubs;
using Chess.Server.Services;
using Chess.Shared.Constants;
using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Web;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

// Azure AD auth — only active when TenantId is configured
var azureAd = builder.Configuration.GetSection("AzureAd");
if (!string.IsNullOrWhiteSpace(azureAd["TenantId"]))
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
else
{
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();
}

// Cosmos DB — bypass SSL validation for the local emulator's self-signed cert
builder.Services.AddSingleton(_ =>
{
    var connectionString = builder.Configuration["Cosmos:ConnectionString"]!;
    var options = new CosmosClientOptions
    {
        HttpClientFactory = () => new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        }),
        ConnectionMode = ConnectionMode.Gateway
    };
    return new CosmosClient(connectionString, options);
});

builder.Services.AddSingleton<GameRepository>();

// Chess services
builder.Services.AddSingleton<MatchmakerService>();
builder.Services.AddSingleton<GameEngineService>();
builder.Services.AddSingleton<MoveValidatorService>();

// Clock background service
builder.Services.AddHostedService<ClockWorker>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration["AllowedOrigins"] ?? "https://localhost:7001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// Initialize Cosmos DB container on startup
var repo = app.Services.GetRequiredService<GameRepository>();
await repo.InitializeAsync();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameHub>(ApiRoutes.HubPath);

app.Run();
