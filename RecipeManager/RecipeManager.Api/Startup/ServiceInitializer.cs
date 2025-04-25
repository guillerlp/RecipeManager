using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RecipeManager.Api.Startup.CustomObjects;
using RecipeManager.Application.Queries.Recipes;
using RecipeManager.Application.Validators.Recipes;
using RecipeManager.Domain.Interfaces.Repositories;
using RecipeManager.Infrastructure.Context;
using RecipeManager.Infrastructure.Repositories;
using System.Text.Json.Serialization;

namespace RecipeManager.Api.Startup
{
    public static class ServiceInitializer
    {
        public static IServiceCollection RegisterDbContext(this IServiceCollection services, DatabaseConnectionConfiguration databaseContextConfiguration)
        {
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(databaseContextConfiguration.DefaultConnection));
            return services;
        }

        public static IServiceCollection RegisterInfrastructureDependencies(this IServiceCollection services)
        {
            services.AddScoped<IRecipeRepository, RecipeRepository>();
            return services;
        }

        public static IServiceCollection RegisterApiDependencies(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection RegisterApplicationDependencies(this IServiceCollection services)
        {
            var appAssembly = typeof(CreateRecipeCommandValidator).Assembly;

            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssemblies(appAssembly);
            });

            services.AddAutoMapper(appAssembly);

            services.AddValidatorsFromAssembly(appAssembly);

            return services;
        }

        public static IServiceCollection RegisterSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Recipemanager", Version = "v1" });
                options.CustomSchemaIds(type => type.ToString());
                options.DescribeAllParametersInCamelCase();
            });
            return services;
        }

        public static IServiceCollection RegisterBuildersServices(this IServiceCollection services)
        {
            services.AddControllers()
                    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            services.AddFluentValidationAutoValidation();
            return services;
        }
    }
}
