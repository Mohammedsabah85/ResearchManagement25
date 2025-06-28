using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

using System;
using System.IO;

namespace ResearchManagement.Infrastructure.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Get the environment
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Find the project root directory (where the solution file is)
            var currentDirectory = Directory.GetCurrentDirectory();
            var projectRoot = currentDirectory;

            // Navigate up until we find the src directory or reach the root
            while (!Directory.Exists(Path.Combine(projectRoot, "src")) &&
                   Directory.GetParent(projectRoot) != null)
            {
                projectRoot = Directory.GetParent(projectRoot).FullName;
            }

            // If we found the src directory, use the Web project's appsettings
            var webProjectPath = Path.Combine(projectRoot, "src", "ResearchManagement.Web");
            var configPath = Directory.Exists(webProjectPath) ?
                webProjectPath :
                Directory.GetCurrentDirectory();

            // Build configuration
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(configPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var configuration = configBuilder.Build();

            // Get connection string
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback to a default connection string if none is found
                connectionString = "Server=MSI\\MSSQLSERVER01;Database=ResearchManagementDb;user id=sa;password=memo@100;Trusted_Connection=false;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=false;";
            }

            // Create DbContext options
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}