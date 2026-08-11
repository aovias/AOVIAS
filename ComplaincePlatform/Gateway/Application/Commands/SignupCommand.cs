using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Commands;

/// <summary>
/// Command for user signup/registration with organization and hospital creation.
/// </summary>
public class SignupCommand : IRequest<SignupResponseDto>
{
    public SignupRequestDto Request { get; set; } = null!;

    public SignupCommand(SignupRequestDto request)
    {
        Request = request;
    }
}
