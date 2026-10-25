using Gateway.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public sealed record RecordOnboardingTelemetryCommand(OnboardingTelemetryEventRequestDto Body)
    : IRequest<IActionResult>;
