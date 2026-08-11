using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

/// <summary>
/// Query for refreshing authentication token.
/// </summary>
public class RefreshTokenQuery : IRequest<IActionResult>
{
    public object RequestBody { get; set; } = null!;

    public RefreshTokenQuery(object requestBody)
    {
        RequestBody = requestBody;
    }
}
