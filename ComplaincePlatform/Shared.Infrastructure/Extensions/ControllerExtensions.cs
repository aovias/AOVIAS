using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.DTO;

namespace Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for controllers to create standardized ApiResponse results.
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Creates a successful 200 OK response with data.
    /// </summary>
    public static IActionResult OkResponse<T>(this ControllerBase controller, T data)
    {
        var traceId = controller.HttpContext.TraceIdentifier;
        var response = ApiResponse<T>.SuccessResponse(data, traceId);
        return controller.Ok(response);
    }

    /// <summary>
    /// Creates a successful 201 Created response with data.
    /// </summary>
    public static IActionResult CreatedResponse<T>(this ControllerBase controller, string uri, T data)
    {
        var traceId = controller.HttpContext.TraceIdentifier;
        var response = ApiResponse<T>.SuccessResponse(data, traceId);
        return controller.Created(uri, response);
    }

    /// <summary>
    /// Creates a successful 201 Created response with data (using action name).
    /// </summary>
    public static IActionResult CreatedResponse<T>(this ControllerBase controller, string actionName, object routeValues, T data)
    {
        var traceId = controller.HttpContext.TraceIdentifier;
        var response = ApiResponse<T>.SuccessResponse(data, traceId);
        return controller.CreatedAtAction(actionName, routeValues, response);
    }

    /// <summary>
    /// Creates a 204 No Content response (for successful DELETE operations).
    /// </summary>
    public static ActionResult NoContentResponse(this ControllerBase controller)
    {
        return controller.NoContent();
    }

    /// <summary>
    /// Creates a 400 BadRequest response with validation errors.
    /// </summary>
    public static IActionResult BadRequestResponse(this ControllerBase controller, string message, Dictionary<string, string[]>? validationErrors = null)
    {
        var traceId = controller.HttpContext.TraceIdentifier;
        var error = ApiError.ValidationError(message, validationErrors ?? new Dictionary<string, string[]>());
        var response = ApiResponse<object>.ErrorResponse(error, traceId);
        return controller.BadRequest(response);
    }

    /// <summary>
    /// Creates a 400 BadRequest response with a single error message.
    /// </summary>
    public static IActionResult BadRequestResponse(this ControllerBase controller, string message, string fieldName, string errorMessage)
    {
        var traceId = controller.HttpContext.TraceIdentifier;
        var validationErrors = new Dictionary<string, string[]> { { fieldName, new[] { errorMessage } } };
        var error = ApiError.ValidationError(message, validationErrors);
        var response = ApiResponse<object>.ErrorResponse(error, traceId);
        return controller.BadRequest(response);
    }

    /// <summary>
    /// Creates a 401 Unauthorized response.
    /// </summary>
    public static IActionResult UnauthorizedResponse(this ControllerBase controller, string message = "Authentication required")
    {
        var traceId = controller.HttpContext.TraceIdentifier;
        var error = ApiError.Unauthorized(message);
        var response = ApiResponse<object>.ErrorResponse(error, traceId);
        return controller.Unauthorized(response);
    }

    /// <summary>
    /// Creates a 403 Forbidden response.
    /// </summary>
    public static IActionResult ForbiddenResponse(this ControllerBase controller, string message = "You do not have permission to access this resource")
    {
        var traceId = controller.HttpContext.TraceIdentifier;
        var error = ApiError.Forbidden(message);
        var response = ApiResponse<object>.ErrorResponse(error, traceId);
        return controller.StatusCode(403, response);
    }

    /// <summary>
    /// Creates a 404 NotFound response.
    /// </summary>
    public static IActionResult NotFoundResponse(this ControllerBase controller, string code, string message)
    {
        var traceId = controller.HttpContext.TraceIdentifier;
        var error = ApiError.NotFound(code, message);
        var response = ApiResponse<object>.ErrorResponse(error, traceId);
        return controller.NotFound(response);
    }

    /// <summary>
    /// Creates a 409 Conflict response.
    /// </summary>
    public static IActionResult ConflictResponse(this ControllerBase controller, string code, string message)
    {
        var traceId = controller.HttpContext.TraceIdentifier;
        var error = ApiError.Conflict(code, message);
        var response = ApiResponse<object>.ErrorResponse(error, traceId);
        return controller.Conflict(response);
    }

    /// <summary>
    /// Creates a 422 UnprocessableEntity response.
    /// </summary>
    public static IActionResult UnprocessableEntityResponse(this ControllerBase controller, string code, string message)
    {
        var traceId = controller.HttpContext.TraceIdentifier;
        var error = ApiError.UnprocessableEntity(code, message);
        var response = ApiResponse<object>.ErrorResponse(error, traceId);
        return controller.UnprocessableEntity(response);
    }
}
