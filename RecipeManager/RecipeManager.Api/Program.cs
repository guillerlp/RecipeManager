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
            // SECURITY: IntegrationTest environment is only allowed in DEBUG builds
            if (builder.Environment.EnvironmentName == "IntegrationTest")
            {
#if DEBUG
                // Integration test environment - DbContext will be registered by test infrastructure
#else
                throw new InvalidOperationException(
                    "IntegrationTest environment is not allowed in RELEASE builds. " +
                    "This environment is reserved for automated testing only.");
#endif
            }
            else
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
