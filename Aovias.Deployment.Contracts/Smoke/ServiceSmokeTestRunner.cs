using System.Diagnostics;
using System.Net.Http;
using Aovias.Deployment.Contracts.Smoke;

namespace Aovias.Deployment.Contracts.Smoke;

/// <summary>
/// Executes contract-declared smoke tests against deployed service endpoints.
/// </summary>
public interface IServiceSmokeTestRunner
{
    Task<SmokeTestSuiteResult> RunAsync(
        IReadOnlyDictionary<string, string> serviceBaseUrls,
        IEnumerable<string>? serviceIds = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// HTTP-based smoke test runner for platform services.
/// </summary>
public sealed class ServiceSmokeTestRunner : IServiceSmokeTestRunner
{
    public async Task<SmokeTestSuiteResult> RunAsync(
        IReadOnlyDictionary<string, string> serviceBaseUrls,
        IEnumerable<string>? serviceIds = null,
        CancellationToken cancellationToken = default)
    {
        var targets = serviceIds?.ToList()
            ?? serviceBaseUrls.Keys.ToList();

        var results = new List<SmokeTestExecutionResult>();
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        foreach (var serviceId in targets)
        {
            if (!serviceBaseUrls.TryGetValue(serviceId, out var baseUrl))
            {
                results.Add(new SmokeTestExecutionResult
                {
                    ServiceId = serviceId,
                    TestId = "base-url",
                    TestName = "Base URL",
                    Passed = false,
                    Message = "Service base URL was not configured."
                });
                continue;
            }

            var suite = ServiceSmokeTestCatalog.GetSuite(serviceId);
            foreach (var test in suite.Tests)
            {
                results.Add(await ExecuteAsync(client, serviceId, baseUrl, test, cancellationToken));
            }
        }

        var failed = results.Count(IsRequiredFailure);
        return new SmokeTestSuiteResult
        {
            Passed = failed == 0,
            Results = results,
            PassedCount = results.Count(result => result.Passed),
            FailedCount = failed
        };
    }

    private static bool IsRequiredFailure(SmokeTestExecutionResult result)
    {
        var suite = ServiceSmokeTestCatalog.GetSuite(result.ServiceId);
        var definition = suite.Tests.FirstOrDefault(test =>
            string.Equals(test.Id, result.TestId, StringComparison.OrdinalIgnoreCase));

        return definition is { Required: true, } && !result.Passed;
    }

    private static async Task<SmokeTestExecutionResult> ExecuteAsync(
        HttpClient client,
        string serviceId,
        string baseUrl,
        ServiceSmokeTestDefinition test,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var url = Combine(baseUrl, test.Path);

        try
        {
            using var request = new HttpRequestMessage(
                test.Kind == SmokeTestKind.HttpHead ? HttpMethod.Head : HttpMethod.Get,
                url);

            using var response = await client.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            var passed = (int)response.StatusCode == test.ExpectedStatus;
            if (passed && !string.IsNullOrWhiteSpace(test.ExpectedContentContains))
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                passed = body.Contains(test.ExpectedContentContains, StringComparison.OrdinalIgnoreCase);
            }

            return new SmokeTestExecutionResult
            {
                ServiceId = serviceId,
                TestId = test.Id,
                TestName = test.Name,
                Passed = passed,
                StatusCode = (int)response.StatusCode,
                Duration = stopwatch.Elapsed,
                Message = passed ? "PASS" : $"Expected {test.ExpectedStatus}, got {(int)response.StatusCode}"
            };
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return new SmokeTestExecutionResult
            {
                ServiceId = serviceId,
                TestId = test.Id,
                TestName = test.Name,
                Passed = !test.Required,
                Duration = stopwatch.Elapsed,
                Message = exception.Message
            };
        }
    }

    private static string Combine(string baseUrl, string path)
    {
        var trimmedBase = baseUrl.TrimEnd('/');
        var trimmedPath = path.StartsWith('/') ? path : $"/{path}";
        return $"{trimmedBase}{trimmedPath}";
    }
}
