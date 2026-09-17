using FluentResults;

namespace RecipeManager.Domain.Errors;

/// <summary>
/// An expected domain failure, classified by what went wrong. How a transport reports it is not the domain's concern.
/// </summary>
public sealed class DomainError(string message, ErrorKind kind) : Error(message)
{
    public ErrorKind Kind { get; } = kind;
}
