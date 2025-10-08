using RecipeManager.Api.Startup;
using RecipeManager.Api.Startup.CustomObjects;

namespace RecipeManager.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Skip database registration for integration tests
            // Integration tests will register their own InMemory database
            if (builder.Environment.EnvironmentName != "IntegrationTest")
            {
                var dbContextConfiguration = builder.Configuration
                    .GetSection("ConnectionStrings")
                    .Get<DatabaseConnectionConfiguration>();

                if (dbContextConfiguration is null ||
                    dbContextConfiguration.DefaultConnection == string.Empty)
                {
                    throw new Exception("Error while parsing appsettings data");
                }

                builder.Services.RegisterDbContext(dbContextConfiguration);
            }

            builder.Services
                .RegisterCors()
                .RegisterApiDependencies()
                .RegisterApplicationDependencies()
                .RegisterInfrastructureDependencies()
                .RegisterSwagger()
                .RegisterBuildersServices();

            var app = builder.Build();

            // Skip migrations for integration tests (uses InMemory database)
            if (app.Environment.EnvironmentName != "IntegrationTest")
            {
                app.MigrateDatabase();
            }

            app.SetupSwagger()
                .ConfigurePipeline();

            app.Run();
        }
    }
}
