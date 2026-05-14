using Microsoft.Extensions.Configuration;
using System.IO;

namespace musicshop.Helpers
{
    public static class AppConfig
    {
        private static readonly IConfiguration _config;

        static AppConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _config = builder.Build();
        }

        public static string ConnectionString =>
            _config.GetConnectionString("DefaultConnection")!;
    }
}