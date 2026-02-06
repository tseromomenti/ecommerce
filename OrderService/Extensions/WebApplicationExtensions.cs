using Microsoft.EntityFrameworkCore;
using OrderService.Infrustructure;

namespace OrderService.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ApplyOrderMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.Migrate();
        }

        return app;
    }
}
