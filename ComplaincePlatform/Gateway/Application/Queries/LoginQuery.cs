using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

/// <summary>
/// Query for user login.
/// </summary>
public class LoginQuery : IRequest<IActionResult>
{
    public object RequestBody { get; set; } = null!;

    public LoginQuery(object requestBody)
    {
        RequestBody = requestBody;
    }
}
