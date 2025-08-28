using System.Text.Json.Serialization;
using FluentResults;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RecipeManager.Api.Startup.CustomObjects;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Common.Interfaces;
using RecipeManager.Application.Common.Interfaces.Caching;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.Dispatchers;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Handlers.Recipes;
using RecipeManager.Application.Queries.Recipes;
using RecipeManager.Application.Validators.Recipes;
using RecipeManager.Domain.Interfaces.Repositories;
using RecipeManager.Infrastructure.Context;
using RecipeManager.Infrastructure.Repositories;
using RecipeManager.Infrastructure.Services;

namespace RecipeManager.Api.Startup
{
    public static class ServiceInitializer
    {
        public static IServiceCollection RegisterDbContext(this IServiceCollection services,
            DatabaseConnectionConfiguration databaseContextConfiguration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(databaseContextConfiguration.DefaultConnection));
            return services;
        }

        public static IServiceCollection RegisterCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", builder =>
                {
                    builder.WithOrigins("http://localhost:3000")
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
            return services;
        }

        public static IServiceCollection RegisterApiDependencies(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection RegisterApplicationDependencies(this IServiceCollection services)
        {
            var appAssembly = typeof(CreateRecipeCommandValidator).Assembly;
            services.AddValidatorsFromAssembly(appAssembly);
            services.RegisterCqrsDispatchers();
            return services;
        }

        private static IServiceCollection RegisterCqrsDispatchers(this IServiceCollection services)
        {
            services.AddScoped<ICommandDispatcher, CommandDispatcher>();
            services.AddScoped<IQueryDispatcher, QueryDispatcher>();

            return services.RegisterCqrsHandlers();
        }

        public static IServiceCollection RegisterInfrastructureDependencies(this IServiceCollection services)
        {
            services.RegisterRepositories();
            services.RegisterCachingServices();
            return services;
        }

        private static IServiceCollection RegisterRepositories(this IServiceCollection services)
        {
            services.AddScoped<IRecipeRepository, RecipeRepository>();
            return services;
        }

        private static IServiceCollection RegisterCachingServices(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddScoped<ICacheService, MemoryCacheService>();
            return services;
        }

        public static IServiceCollection RegisterSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "RecipeManager", Version = "v1" });
                options.CustomSchemaIds(type => type.ToString());
                options.DescribeAllParametersInCamelCase();
            });
            return services;
        }

        public static IServiceCollection RegisterBuildersServices(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.Converters.Add(
                        new JsonStringEnumConverter())
                );
            services.AddFluentValidationAutoValidation();
            return services;
        }

        private static IServiceCollection RegisterCqrsHandlers(this IServiceCollection services)
        {
            services.AddScoped<ICommandHandler<CreateRecipeCommand, Result<RecipeDto>>, CreateRecipeHandler>();
            services.AddScoped<ICommandHandler<UpdateRecipeCommand, Result>, UpdateRecipeHandler>();
            services.AddScoped<ICommandHandler<DeleteRecipeCommand, Result>, DeleteRecipeHandler>();
            services.AddScoped<IQueryHandler<GetAllRecipesQuery, IEnumerable<RecipeDto>>, GetAllRecipesHandler>();
            services.AddScoped<IQueryHandler<GetRecipeByIdQuery, Result<RecipeDto>>, GetRecipeByIdHandler>();
            return services;
        }
    }
}