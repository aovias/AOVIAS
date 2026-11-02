var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "AccredaOne.Api",
    phase = "DEV-03 scaffolding only"
}));

app.Run();

public partial class Program;
