using FluentValidation;
using Gateway.Application.Queries;
using System.Text.Json;

namespace Gateway.Application.Validators;

/// <summary>
/// Validator for RefreshTokenQuery using FluentValidation.
/// Request body is deserialized as <see cref="JsonElement"/> (camelCase JSON: refreshToken).
/// </summary>
public class RefreshTokenQueryValidator : AbstractValidator<RefreshTokenQuery>
{
    public RefreshTokenQueryValidator()
    {
        RuleFor(x => x.RequestBody)
            .NotNull()
            .WithMessage("Request body is required.");

        RuleFor(x => x.RequestBody)
            .Must(HaveNonEmptyRefreshToken)
            .WithMessage("Refresh token is required.");
    }

    private static bool HaveNonEmptyRefreshToken(object? body)
    {
        if (body is null)
            return false;

        if (body is JsonElement je)
            return JsonBodyContainsRefreshToken(je);

        return false;
    }

    private static bool JsonBodyContainsRefreshToken(JsonElement je)
    {
        if (je.ValueKind != JsonValueKind.Object)
            return false;

        if (je.TryGetProperty("refreshToken", out var p))
            return IsNonEmptyJsonString(p);

        if (je.TryGetProperty("RefreshToken", out var p2))
            return IsNonEmptyJsonString(p2);

        return false;
    }

    private static bool IsNonEmptyJsonString(JsonElement p)
    {
        if (p.ValueKind == JsonValueKind.Null || p.ValueKind == JsonValueKind.Undefined)
            return false;
        if (p.ValueKind != JsonValueKind.String)
            return false;
        return !string.IsNullOrWhiteSpace(p.GetString());
    }
}
