using FluentResults;
using Microsoft.AspNetCore.Mvc;
using RecipeManager.Domain.Errors;

namespace RecipeManager.Api.Extensions;

public static class ResultExtensions
{
    public static ActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return CreateProblemDetails(result.Errors);
    }

    public static ActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();

        return CreateProblemDetails(result.Errors);
    }

    public static ActionResult ToCreatedAtActionResult<T>(this Result<T> result, string actionName, object? routeValues = null)
    {
        if (result.IsSuccess)
            return new CreatedAtActionResult(actionName, null, routeValues, result.Value);

        return CreateProblemDetails(result.Errors);
    }

    public static ActionResult CreateProblemDetails(IReadOnlyList<IError> errors)
    {
        // The most severe error decides the response; on a tie, the first one wins.
        var primaryError = errors.MaxBy(e => Classify(e).Severity)
            ?? throw new ArgumentException("A failed result must carry at least one error.", nameof(errors));

        var statusCode = Classify(primaryError).StatusCode;

        var field = GetErrorField(primaryError);

        var problemDetails = new ProblemDetails
        {
            Title = GetErrorTitle(statusCode),
            Detail = primaryError.Message,
            Status = statusCode,
        };

        if (!string.IsNullOrEmpty(field))
        {
            problemDetails.Extensions.Add("field", field);
        }

        if (errors.Count > 1)
        {
            problemDetails.Extensions.Add("errors", errors.Select(e => new
            {
                message = e.Message,
                field = GetErrorField(e),
                code = Classify(e).StatusCode
            }));
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }

    // The only place a domain error kind becomes HTTP. An error without a kind is a failure the domain did not
    // model, so it outranks everything rather than hiding behind a 4xx the client could act on.
    private static (int StatusCode, int Severity) Classify(IError error) =>
        (error as DomainError)?.Kind switch
        {
            null => (StatusCodes.Status400BadRequest, 3),
            ErrorKind.NotFound => (StatusCodes.Status404NotFound, 2),
            ErrorKind.Validation => (StatusCodes.Status422UnprocessableEntity, 1),
            var kind => throw new ArgumentOutOfRangeException(nameof(error), kind, "Error kind has no HTTP mapping.")
        };

    private static string? GetErrorField(IError error)
    {
        if (error.Metadata.TryGetValue("field", out var field) && field is string fieldName)
            return fieldName;

        return null;
    }

    private static string GetErrorTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status404NotFound => "Resource not found",
        StatusCodes.Status422UnprocessableEntity => "Validation failed",
        StatusCodes.Status400BadRequest => "Bad request",
        StatusCodes.Status500InternalServerError => "Internal server error",
        _ => "An error occurred"
    };
}
