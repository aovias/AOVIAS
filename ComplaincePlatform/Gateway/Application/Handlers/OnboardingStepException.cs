using System;

namespace Gateway.Application.Handlers;

/// <summary>
/// Represents a failure in a specific onboarding step with rich downstream details.
/// </summary>
internal sealed class OnboardingStepException : Exception
{
    public string StepName { get; }
    public int HttpStatusCode { get; }
    public string? ResponseCode { get; }
    public string? ResponseBody { get; }

    public OnboardingStepException(
        string stepName,
        int statusCode,
        string? responseCode,
        string? responseBody,
        string message)
        : base(message)
    {
        StepName = stepName;
        HttpStatusCode = statusCode;
        ResponseCode = responseCode;
        ResponseBody = responseBody;
    }
}

