using FluentAssertions;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RecipeManager.Api.Extensions;
using RecipeManager.Domain.Errors;

namespace RecipeManager.UnitTests.Api.Extensions;

public class ResultExtensionsTests
{
    #region Kind to status

    [Theory]
    [InlineData(ErrorKind.Validation, StatusCodes.Status422UnprocessableEntity)]
    [InlineData(ErrorKind.NotFound, StatusCodes.Status404NotFound)]
    public void CreateProblemDetails_WithSingleDomainError_ShouldMapKindToStatus(ErrorKind kind, int expectedStatus)
    {
        // Arrange
        var error = new DomainError("Something failed", kind);

        // Act
        var problem = ProblemFor(error);

        // Assert
        problem.Status.Should().Be(expectedStatus);
        problem.Detail.Should().Be("Something failed");
    }

    [Fact]
    public void CreateProblemDetails_WithErrorWithoutKind_ShouldReturnBadRequest()
    {
        // Arrange
        var error = new Error("Something failed");

        // Act
        var problem = ProblemFor(error);

        // Assert
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void CreateProblemDetails_ForEveryErrorKind_ShouldNotThrow()
    {
        foreach (var kind in Enum.GetValues<ErrorKind>())
        {
            // Arrange
            IReadOnlyList<IError> errors = [new DomainError("Something failed", kind)];

            // Act
            var act = () => ResultExtensions.CreateProblemDetails(errors);

            // Assert
            act.Should().NotThrow($"{kind} must have a status mapping");
        }
    }

    [Fact]
    public void CreateProblemDetails_WithUnmappedErrorKind_ShouldThrow()
    {
        // Arrange
        IReadOnlyList<IError> errors = [new DomainError("Something failed", (ErrorKind)999)];

        // Act
        var act = () => ResultExtensions.CreateProblemDetails(errors);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Most severe error wins

    [Fact]
    public void CreateProblemDetails_WithValidationThenNotFound_ShouldUseNotFoundError()
    {
        // Arrange
        var validation = ErrorWithField(ErrorKind.Validation, "Title is required", "title");
        var notFound = ErrorWithField(ErrorKind.NotFound, "Recipe was not found", "id");

        // Act
        var problem = ProblemFor(validation, notFound);

        // Assert
        problem.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Detail.Should().Be("Recipe was not found");
        problem.Extensions["field"].Should().Be("id");
        ErrorsExtension(problem).Should().BeEquivalentTo(new[]
        {
            new { message = "Title is required", field = (string?)"title", code = 422 },
            new { message = "Recipe was not found", field = (string?)"id", code = 404 }
        }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void CreateProblemDetails_WithValidationAndErrorWithoutKind_ShouldReturnBadRequest()
    {
        // Arrange
        var validation = ErrorWithField(ErrorKind.Validation, "Title is required", "title");
        var unexpected = new Error("Error while deleting the recipe");

        // Act
        var problem = ProblemFor(validation, unexpected);

        // Assert
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Detail.Should().Be("Error while deleting the recipe");
        problem.Extensions.Should().NotContainKey("field");
    }

    [Fact]
    public void CreateProblemDetails_WithTwoValidationErrors_ShouldUseFirstAndListBoth()
    {
        // Arrange
        var title = ErrorWithField(ErrorKind.Validation, "Title is required", "title");
        var description = ErrorWithField(ErrorKind.Validation, "Description is required", "description");

        // Act
        var problem = ProblemFor(title, description);

        // Assert
        problem.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        problem.Detail.Should().Be("Title is required");
        problem.Extensions["field"].Should().Be("title");
        ErrorsExtension(problem).Should().BeEquivalentTo(new[]
        {
            new { message = "Title is required", field = (string?)"title", code = 422 },
            new { message = "Description is required", field = (string?)"description", code = 422 }
        }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void CreateProblemDetails_WithSingleError_ShouldNotAddErrorsExtension()
    {
        // Act
        var problem = ProblemFor(ErrorWithField(ErrorKind.Validation, "Title is required", "title"));

        // Assert
        problem.Extensions.Should().NotContainKey("errors");
    }

    #endregion

    #region Result conversion

    [Fact]
    public void ToActionResult_WithSuccessfulResult_ShouldReturnNoContent()
    {
        // Act
        var actionResult = Result.Ok().ToActionResult();

        // Assert
        actionResult.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void ToActionResult_WithSuccessfulValueResult_ShouldReturnOkWithValue()
    {
        // Act
        var actionResult = Result.Ok(42).ToActionResult();

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(42);
    }

    [Fact]
    public void ToCreatedAtActionResult_WithSuccessfulResult_ShouldReturnCreated()
    {
        // Act
        var actionResult = Result.Ok(42).ToCreatedAtActionResult("GetById", new { id = 42 });

        // Assert
        var created = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be("GetById");
        created.Value.Should().Be(42);
    }

    [Fact]
    public void ToActionResult_WithFailedResult_ShouldReturnProblemDetailsWithMappedStatus()
    {
        // Act
        var actionResult = Result.Fail(new DomainError("Recipe was not found", ErrorKind.NotFound)).ToActionResult();

        // Assert
        actionResult.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    #endregion

    private static Error ErrorWithField(ErrorKind kind, string message, string field) =>
        new DomainError(message, kind).WithMetadata("field", field);

    private static ProblemDetails ProblemFor(params IError[] errors)
    {
        var objectResult = ResultExtensions.CreateProblemDetails(errors)
            .Should().BeOfType<ObjectResult>().Subject;
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        objectResult.StatusCode.Should().Be(problem.Status, "the response status and the body status must agree");
        return problem;
    }

    private static IEnumerable<object> ErrorsExtension(ProblemDetails problem) =>
        problem.Extensions["errors"].Should().BeAssignableTo<IEnumerable<object>>().Subject;
}
