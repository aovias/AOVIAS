using Aovias.CommandCenter.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Sprint M2.7: deliberately a standalone, minimal ASP.NET Core host — not wired into the Runtime
// Agent's/Release Manager's multi-profile bootstrap pipeline (AoviasPlatformBootstrapResolver).
// The Command Center is a new, independent product area with no profile-specific storage layout
// or feature-flag surface of its own; its only configuration is the Fleet Registry/polling knobs
// in appsettings.json, bound the standard ASP.NET Core way. This keeps Sprint M2.7 additive and
// avoids entangling a brand-new service with the Runtime/Release Management bootstrap system
// ("Do NOT redesign Runtime Agent").
builder.Services.AddControllers();
builder.Services.AddCommandCenterInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
