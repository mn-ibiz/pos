using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HospitalityPOS.Infrastructure.Data;

/// <summary>
/// Factory for creating POSDbContext instances at design time for EF Core migrations.
/// This is used by 'dotnet ef migrations add' and Package Manager Console commands.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<POSDbContext>
{
    /// <summary>
    /// Creates a new instance of POSDbContext for design-time operations.
    /// </summary>
    /// <param name="args">Command line arguments (not used).</param>
    /// <returns>A configured POSDbContext instance.</returns>
    public POSDbContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings.json
        // Look for appsettings.json in the WPF project during design time
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "HospitalityPOS.WPF");

        IConfiguration configuration;

        if (Directory.Exists(basePath) && File.Exists(Path.Combine(basePath, "appsettings.json")))
        {
            configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
        }
        else
        {
            // Fallback: use current directory (for when running from solution root)
            var solutionRoot = FindSolutionRoot(Directory.GetCurrentDirectory());
            var wpfPath = Path.Combine(solutionRoot, "src", "HospitalityPOS.WPF");

            if (Directory.Exists(wpfPath) && File.Exists(Path.Combine(wpfPath, "appsettings.json")))
            {
                configuration = new ConfigurationBuilder()
                    .SetBasePath(wpfPath)
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();
            }
            else
            {
                // Last resort: use default connection string
                configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            "Server=.\\SQLEXPRESS;Database=HospitalityPOS;Trusted_Connection=True;TrustServerCertificate=True;"
                    })
                    .Build();
            }
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in configuration. " +
                "Ensure appsettings.json exists in HospitalityPOS.WPF project or provide a valid configuration.");

        var optionsBuilder = new DbContextOptionsBuilder<POSDbContext>();
        optionsBuilder.UseSqlServer(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(POSDbContext).Assembly.FullName);
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });

        return new POSDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Finds the solution root directory by looking for .sln file.
    /// </summary>
    private static string FindSolutionRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);

        while (directory != null)
        {
            if (directory.GetFiles("*.sln").Length > 0)
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        // If no solution found, return current directory
        return startPath;
    }
}
