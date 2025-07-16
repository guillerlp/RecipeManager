using Microsoft.Extensions.DependencyInjection;
using RecipeManager.Application.Common;

namespace RecipeManager.Application.Dispatchers;

public class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public QueryDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResult> Dispatch<TQuery, TResult>(TQuery query, CancellationToken cancellationToken)
    {
        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return handler.Handle(query, cancellationToken);
    }
}