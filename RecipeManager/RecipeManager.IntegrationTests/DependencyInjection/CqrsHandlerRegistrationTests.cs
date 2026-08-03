using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.Validators.Recipes;

namespace RecipeManager.IntegrationTests.DependencyInjection;

/// <summary>
/// Handlers are discovered by Scrutor assembly scanning (ADR-008) instead of being listed by hand, so nothing
/// in the source code shows that a handler is registered. These tests are what keeps that verifiable: a handler
/// the container cannot resolve fails here rather than on the first request that dispatches it.
/// </summary>
public class CqrsHandlerRegistrationTests : IntegrationTestBase
{
    private static readonly Assembly ApplicationAssembly = typeof(CreateRecipeCommandValidator).Assembly;

    public static TheoryData<Type> HandlerServiceTypes()
    {
        var data = new TheoryData<Type>();

        foreach (Type serviceType in GetHandlerServiceTypes())
        {
            data.Add(serviceType);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(HandlerServiceTypes))]
    public void EveryCqrsHandler_ShouldBeResolvableFromTheContainer(Type handlerServiceType)
    {
        // ==================== ARRANGE ====================
        // The service type is a closed handler interface, e.g. ICommandHandler<CreateRecipeCommand, Result<RecipeDto>>.

        // ==================== ACT ====================
        object? handler = Scope.ServiceProvider.GetService(handlerServiceType);

        // ==================== ASSERT ====================
        handler.Should().NotBeNull(
            "{0} must be registered — the dispatchers resolve it with GetRequiredService and would throw at runtime",
            handlerServiceType);
    }

    [Fact]
    public void HandlerDiscovery_ShouldFindEveryHandlerInTheApplicationAssembly()
    {
        // ==================== ARRANGE ====================
        // Guards the theory above: with no discovered types it would pass without asserting anything.

        // ==================== ACT ====================
        List<Type> serviceTypes = GetHandlerServiceTypes().ToList();

        // ==================== ASSERT ====================
        serviceTypes.Should().NotBeEmpty();
        serviceTypes.Should().OnlyHaveUniqueItems(
            "two handlers sharing one closed interface would make the container resolution ambiguous");
    }

    /// <summary>
    /// Every closed <c>ICommandHandler&lt;,&gt;</c> / <c>IQueryHandler&lt;,&gt;</c> interface implemented by a
    /// concrete class in the Application assembly. Reflection rather than a hard-coded list, so a new handler is
    /// covered without touching this file — the same reason the registration itself is no longer hand-written.
    /// </summary>
    private static IEnumerable<Type> GetHandlerServiceTypes()
    {
        return ApplicationAssembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type.GetInterfaces())
            .Where(@interface => @interface.IsGenericType
                                 && (@interface.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)
                                     || @interface.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
            .Distinct();
    }
}
