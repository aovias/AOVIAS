using System.Text.Json;
using System.Text.Json.Serialization;
using Platform.Dashboard.Models;

var json = """{"success":true,"data":{"summary":null,"widgets":[{"id":"w1","application":"Home","title":"T","priority":10,"phase":"criticalWidgets","permissions":[],"lazy":false,"critical":true,"visibility":"Visible"}],"sections":[{"id":"s1","title":"S","priority":10,"phase":"summary","widgetIds":[],"critical":true}]}}""";
var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
opts.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));
var env = JsonSerializer.Deserialize<Env>(json, opts);
Console.WriteLine($"widgets={env?.Data?.Widgets.Count} phase={env?.Data?.Widgets[0].Phase} sectionPhase={env?.Data?.Sections[0].Phase}");

sealed class Env { public DashboardExperienceResult? Data { get; set; } }
