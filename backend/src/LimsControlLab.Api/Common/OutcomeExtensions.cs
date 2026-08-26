using LimsControlLab.Api.Middleware;
using LimsControlLab.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Common;

public static class OutcomeExtensions
{
    public static IActionResult ToActionResult<T>(this Outcome<T> outcome, ControllerBase controller)
    {
        var correlationId = controller.HttpContext.GetCorrelationId();
        var extensions = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(correlationId))
            extensions.Add("correlationId", correlationId);

        return outcome switch
        {
            Outcome<T>.Ok ok => controller.Ok(ok.Data),
            Outcome<T>.NotFound nf => controller.NotFound(CreateProblemDetails(
                "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                "Not Found",
                StatusCodes.Status404NotFound,
                nf.Message,
                extensions)),
            Outcome<T>.Invalid inv => controller.BadRequest(new ValidationProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Validation Failed",
                Status = StatusCodes.Status400BadRequest,
                Errors = new Dictionary<string, string[]> { { inv.Field, new[] { inv.Message } } },
                Extensions = extensions,
            }),
            Outcome<T>.Unauthorized unauth => controller.Unauthorized(CreateProblemDetails(
                "https://tools.ietf.org/html/rfc7235#section-3.1",
                "Unauthorized",
                StatusCodes.Status401Unauthorized,
                unauth.Message,
                extensions)),
            Outcome<T>.Forbidden _ => new ObjectResult(CreateProblemDetails(
                "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                "Forbidden",
                StatusCodes.Status403Forbidden,
                "Access to this resource is forbidden.",
                extensions))
            {
                StatusCode = StatusCodes.Status403Forbidden,
            },
            Outcome<T>.Conflict conf => controller.Conflict(CreateProblemDetailsWithRowVersion(
                "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                "Conflict",
                StatusCodes.Status409Conflict,
                conf.Message,
                extensions,
                conf.CurrentRowVersion)),
            _ => new ObjectResult(CreateProblemDetails(
                "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                "Internal Server Error",
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                extensions))
            {
                StatusCode = StatusCodes.Status500InternalServerError,
            },
        };
    }

    private static ProblemDetails CreateProblemDetails(
        string type,
        string title,
        int status,
        string detail,
        Dictionary<string, object?> extensions)
    {
        return new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail,
            Extensions = extensions,
        };
    }

    private static ProblemDetails CreateProblemDetailsWithRowVersion(
        string type,
        string title,
        int status,
        string detail,
        Dictionary<string, object?> extensions,
        object? currentRowVersion)
    {
        var allExtensions = new Dictionary<string, object?>(extensions);
        if (currentRowVersion != null)
            allExtensions.Add("currentRowVersion", currentRowVersion);

        return new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail,
            Extensions = allExtensions,
        };
    }
}
