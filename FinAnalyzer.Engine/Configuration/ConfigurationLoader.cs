using Microsoft.Extensions.Configuration;
using System.IO;

namespace FinAnalyzer.Engine.Configuration
{
    public static class ConfigurationLoader
    {
        public static IConfigurationRoot Load(string fileName = "appsettings.json", string? basePath = null)
        {
            var builder = new ConfigurationBuilder();
            
            basePath ??= Directory.GetCurrentDirectory();
            builder.SetBasePath(basePath);

            builder.AddJsonFile(fileName, optional: false, reloadOnChange: true);

            return builder.Build();
        }
    }
}
