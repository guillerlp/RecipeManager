using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace RecipeManager.Api.Extensions;

public static class ResultExtensions
{
    public static ActionResult ToActionResult<T>(this Result<T> result)
    {
        if(result.IsSuccess)
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

    public static ActionResult CreateProblemDetails(List<IError> errors)
    {
        var firstError = errors.First();
        
        var statusCode = GetErrorCode(firstError);
        
        var field = GetErrorField(firstError);
            
        var problemDetails = new ProblemDetails
        {
            Title = GetErrorTitle(statusCode),
            Detail = firstError.Message,
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
                code = GetErrorCode(e)
            }));
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }

    private static int GetErrorCode(IError error)
    {
        if (error.Metadata.TryGetValue("ErrorCode", out var code) && code is int errorCode)
            return errorCode;
            
        return StatusCodes.Status400BadRequest;
    }
    
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