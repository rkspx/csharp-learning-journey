using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CaseApi.Tests;

public class CaseApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var configuration = new ConfigurationBuilder()
        .AddUserSecrets<CaseApiFactory>()
        .Build();

        string connectionString = configuration.GetConnectionString("CaseDatabase")
        ?? throw new InvalidOperationException(
            "Test database connection string is missing."
        );

        var connectionSettings = new NpgsqlConnectionStringBuilder(connectionString);

        if (connectionSettings.Database != "case_api_tests")
        {
            throw new InvalidOperationException(
                "Database tests must use case_api_tests."
            );
        }

        builder.UseSetting(
            "ConnectionStrings:CaseDatabase",
            connectionString
        );
    }
}