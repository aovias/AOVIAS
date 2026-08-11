using System.Collections.Concurrent;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var createdTasks = new ConcurrentDictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
var createdFindings = new ConcurrentDictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
var linkedEvidence = new ConcurrentDictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
var contextVersionByHospital = new ConcurrentDictionary<Guid, string>();

app.MapPost("/api/v1/internal/ai/context-bundle", (JsonObject payload) =>
{
    var hospitalId = payload["hospitalId"]?.GetValue<Guid>() ?? Guid.Empty;
    var contextVersion = contextVersionByHospital.GetOrAdd(hospitalId, _ => $"ctx-{hospitalId:N}-v1");

    var response = new JsonObject
    {
        ["contextVersionId"] = contextVersion,
        ["facts"] = new JsonObject
        {
            ["hospitalId"] = hospitalId.ToString(),
            ["mode"] = "stub"
        },
        ["sources"] = new JsonArray
        {
            new JsonObject
            {
                ["sourceType"] = "stub",
                ["sourceId"] = $"hospital:{hospitalId:N}",
                ["summary"] = "Stubbed context bundle for runtime transport validation.",
                ["relevanceScore"] = 1.0m
            }
        }
    };

    return Results.Ok(response);
});

app.MapPost("/api/v1/internal/ai/command-preconditions/validate", (JsonObject payload) =>
{
    var hospitalId = payload["hospitalId"]?.GetValue<Guid>() ?? Guid.Empty;
    var expected = payload["expectedContextVersionId"]?.GetValue<string>() ?? string.Empty;
    var idempotency = payload["idempotencyToken"]?.GetValue<string>() ?? string.Empty;
    var current = contextVersionByHospital.GetOrAdd(hospitalId, _ => $"ctx-{hospitalId:N}-v1");

    if (string.IsNullOrWhiteSpace(idempotency))
    {
        return Results.Ok(new JsonObject
        {
            ["isAllowed"] = false,
            ["reason"] = "idempotency-token-required",
            ["currentContextVersionId"] = current
        });
    }

    if (!string.Equals(expected, current, StringComparison.OrdinalIgnoreCase))
    {
        return Results.Ok(new JsonObject
        {
            ["isAllowed"] = false,
            ["reason"] = "context-version-mismatch",
            ["currentContextVersionId"] = current
        });
    }

    return Results.Ok(new JsonObject
    {
        ["isAllowed"] = true,
        ["reason"] = "ok",
        ["currentContextVersionId"] = current
    });
});

app.MapPost("/api/v1/compliance-tasks", (JsonObject payload) =>
{
    var key = payload["taskId"]?.GetValue<string>() ?? Guid.NewGuid().ToString("N");
    createdTasks.TryAdd(key, payload);
    return Results.Ok(new JsonObject { ["status"] = "created", ["taskId"] = key });
});

app.MapPost("/api/v1/findings", (JsonObject payload) =>
{
    var key = payload["findingId"]?.GetValue<string>() ?? Guid.NewGuid().ToString("N");
    createdFindings.TryAdd(key, payload);
    return Results.Ok(new JsonObject { ["status"] = "created", ["findingId"] = key });
});

app.MapPost("/api/v1/evidence-binder/link", (JsonObject payload) =>
{
    var key = $"{payload["objectiveElementId"]}:{payload["evidenceId"]}";
    linkedEvidence.TryAdd(key, payload);
    return Results.Ok(new JsonObject { ["status"] = "linked", ["linkKey"] = key });
});

app.MapPost("/api/v1/ai/ask", (JsonObject payload) =>
{
    var answer = payload["question"]?.GetValue<string>() ?? "stub-answer";
    return Results.Ok(new JsonObject
    {
        ["answer"] = $"stub:{answer}",
        ["sources"] = new JsonArray
        {
            new JsonObject
            {
                ["sourceType"] = "stub",
                ["sourceId"] = "kb:local",
                ["summary"] = "Stubbed knowledge response."
            }
        }
    });
});

// Test-only helpers used by the HTTP matrix.
app.MapPost("/__stub/context-version/{hospitalId:guid}/{version}", (Guid hospitalId, string version) =>
{
    contextVersionByHospital[hospitalId] = version;
    return Results.Ok(new JsonObject { ["hospitalId"] = hospitalId, ["contextVersionId"] = version });
});

app.MapGet("/__stub/state", () => Results.Ok(new JsonObject
{
    ["tasks"] = createdTasks.Count,
    ["findings"] = createdFindings.Count,
    ["links"] = linkedEvidence.Count
}));

app.Run("http://localhost:7087");
