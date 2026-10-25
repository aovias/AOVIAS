using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Commands;

public sealed record OnboardWizardCommand(OnboardWizardRequestDto Request) : IRequest<OnboardWizardResponseDto>;
