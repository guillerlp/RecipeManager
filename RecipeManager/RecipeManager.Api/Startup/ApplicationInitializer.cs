using Microsoft.EntityFrameworkCore;
using RecipeManager.Api.Extensions;
using RecipeManager.Infrastructure.Context;

namespace RecipeManager.Api.Startup
{
    public static class ApplicationInitializer
    {
        public static WebApplication SetupSwagger(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            return app;
        }

        public static WebApplication ConfigurePipeline(this WebApplication app)
        {
            app.UseCors("AllowReactApp");
            app.UseHttpsRedirection();
            app.UseErrorHandler();
            app.UseRouting();
            app.MapControllers();
            app.UseAuthorization();
            return app;
        }

        public static WebApplication MigrateDatabase(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pending = dbContext.Database.GetPendingMigrations();
            if (pending.Any())
            {
                Console.WriteLine("Applying pending migrations...");
                dbContext.Database.Migrate();
                Console.WriteLine("Migrations applied successfully.");
            }
            else
            {
                Console.WriteLine("No pending migrations found.");
            }

            return app;
        }
    }
}
