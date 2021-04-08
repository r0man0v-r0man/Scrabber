using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scrabber;

namespace webScrabber
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddDbContextFactory<ScrabberContext>(options =>
                    {
                        var connection = new SqliteConnection("DataSource=D:\\halupa\\scrabber_db.db");
                        options.UseSqlite(connection);
                    });
                    services.AddTransient(o => o.GetRequiredService<IDbContextFactory<ScrabberContext>>().CreateDbContext());
                })
            ;
    }
}
