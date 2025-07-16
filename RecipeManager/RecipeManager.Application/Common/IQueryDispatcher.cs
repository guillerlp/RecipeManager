namespace RecipeManager.Application.Common;

public interface IQueryDispatcher
{
    Task<TResult> Dispatch<TQuery, TResult>(TQuery query, CancellationToken cancellationToken);
}