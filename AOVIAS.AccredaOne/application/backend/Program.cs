using System.Text.Json;
using AccredaOne.Api.Configuration;
using AccredaOne.Api.Foundation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.Configure<AccredaOneApiOptions>(
    builder.Configuration.GetSection(AccredaOneApiOptions.SectionName));
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    if (response.HasStarted)
    {
        return;
    }

    await response.WriteAsJsonAsync(
        new
        {
            type = "about:blank",
            title = "Request failed",
            status = response.StatusCode
        },
        new JsonSerializerOptions(JsonSerializerDefaults.Web),
        contentType: "application/problem+json");
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapFoundationEndpoints();

app.Run();

public partial class Program;
