using Microsoft.Extensions.DependencyInjection;
using RecipeManager.Application.Common;

namespace RecipeManager.Application.Dispatchers;

public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResult> Dispatch<TCommand, TResult>(TCommand command, CancellationToken cancellationToken)
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        return handler.Handle(command, cancellationToken);
    }
}