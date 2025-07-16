namespace RecipeManager.Application.Common;

public interface ICommandDispatcher
{
    Task<TResult> Dispatch<TCommand, TResult>(TCommand query, CancellationToken cancellationToken);
}