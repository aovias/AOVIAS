using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Queries;

public sealed record GetOnboardingJourneyQuery : IRequest<OnboardingJourneyDto>;
