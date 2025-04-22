using RecipeManager.Api.Startup;
using RecipeManager.Api.Startup.CustomObjects;

namespace RecipeManager.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var dbContextConfiguration = builder.Configuration.GetSection("ConnectionStrings").Get<DatabaseConnectionConfiguration>();

            if (dbContextConfiguration is null || dbContextConfiguration.DefaultConnection == String.Empty)
            {
                throw new Exception("Error while parsing appsettings data");
            }

            builder.Services
                .RegisterDbContext(dbContextConfiguration)
                .RegisterApiDependencies()
                .RegisterApplicationDependencies()
                .RegisterInfrastructureDependencies()
                .RegisterSwagger()
                .RegisterBuildersServices();

            var app = builder.Build();

            app.MigrateDatabase()
                .SetupSwagger()
                .ConfigurePipeline();

            app.Run();
        }
    }
}
