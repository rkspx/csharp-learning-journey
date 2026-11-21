using System.Net;
using System.Net.Http.Json;
using CaseApi;
using CaseApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaseApi.Tests;

public class DatabaseTests
{

    [Fact]
    public async Task TestDatabase_AppliesCaseMigrations()
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();

        Assert.Equal("case_api_tests", db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();
        var applied = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains("20261119073210_InitialCreate", applied);
        Assert.Contains("20261119122506_AddCaseAssignedTo", applied);
    }

    [Fact]
    public async Task CreateCase_FollowLocation_ReturnsSavedCase()
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();

        Assert.Equal("case_api_tests", db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();
        using var client = factory.CreateClient();
        string title = $"Repeated login failures {Guid.NewGuid()}";

        try
        {
            using var post = await client.PostAsJsonAsync("/cases", new { title });
            Assert.Equal(HttpStatusCode.Created, post.StatusCode);
            var created = await post.Content.ReadFromJsonAsync<CaseResponse>();
            Assert.NotNull(created);
            Assert.False(string.IsNullOrWhiteSpace(created.Id));
            Assert.Equal(title, created.Title);
            Assert.Equal("New", created.Status);

            var location = post.Headers.Location;
            Assert.NotNull(location);

            using var get = await client.GetAsync(location);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var fetched = await get.Content.ReadFromJsonAsync<CaseResponse>();
            Assert.Equal(created, fetched);
        }
        finally
        {
            await db.Cases
            .Where(c => c.Title == title)
            .ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task GetCase_WithMissingId_ReturnsNotFound()
    {
        using var factory = new CaseApiFactory();
        using  var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();

        Assert.Equal("case_api_tests", db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();

        using var client = factory.CreateClient();
        string missingId = Guid.NewGuid().ToString();

        using var get = await client.GetAsync($"/cases/${missingId}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task CreateCase_WithBlankTitle_DoesNotInsertRow()
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();

        Assert.Equal("case_api_tests", db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();

        using var client = factory.CreateClient();
        int countBefore = await db.Cases.CountAsync();

        using var response = await client.PostAsJsonAsync("/cases", new { title = " "});
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        int countAfter = await db.Cases.CountAsync();
        Assert.Equal(countBefore, countAfter);
    }

}