using Microsoft.EntityFrameworkCore;
using OrderApi.Infrastructure.Repositories;

namespace OrderApi.Extensions
{
    internal static class Extensions
    {
        public static void AddApplicationServices(this IHostApplicationBuilder builder)
        {
            var services = builder.Services;

            services.AddDbContext<OrderDbContext>(options => {
                options.UseNpgsql(builder.Configuration.GetConnectionString("OrderDB"));
            });
        }
    }
}
