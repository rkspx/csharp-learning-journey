using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CaseApi.Data;
using Microsoft.OpenApi;

namespace CaseApi.Tests;
[Collection("Case database")]
public class CaseAPITests
{
    private readonly string _dbName = "case_api_tests";
    private static async Task SignInAsync(HttpClient client, IServiceProvider services, string role)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var users = services.GetRequiredService<UserManager<IdentityUser>>();

        if (!await roles.RoleExistsAsync(role))
        {
            var result = await roles.CreateAsync(new IdentityRole(role));
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        string email = $"{role.ToLowerInvariant()}@example.text";
        const string password = "Day9-Local-Test!42";
        var user = await users.FindByEmailAsync(email);

        if (user is null)
        {
            user = new IdentityUser { UserName = email, Email = email };
            var result = await users.CreateAsync(user, password);
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));            
        }

        if (!await users.IsInRoleAsync(user, role))
        {
            var result = await users.AddToRoleAsync(user, role);
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        using var login = await client.PostAsJsonAsync(
            "/auth/login?useCookies=false",
            new {email, password}
        );

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        string? token = body.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Theory]
    [InlineData("Analyst", HttpStatusCode.Created)]
    [InlineData("SystemOwner", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task CreateCase_ReturnsProperAuthorization_FollowLocation_WithSavedCase(string? role, HttpStatusCode expectedStatusCode)
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();

        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();
        using var client = factory.CreateClient();

        if (role is not null)
        {
            await SignInAsync(client, scope.ServiceProvider, role);
        }

        string title = $"Repeated login failures {Guid.NewGuid()}";

        try
        {
            using var post = await client.PostAsJsonAsync("/cases", new { title });
            Assert.Equal(expectedStatusCode, post.StatusCode);
            if (expectedStatusCode == HttpStatusCode.Created)
            {                
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
        }
        finally
        {
            await db.Cases
            .Where(c => c.Title == title)
            .ExecuteDeleteAsync();
        }
    }

    [Theory]
    [InlineData("Analyst", HttpStatusCode.Created)]
    [InlineData("SystemOwner", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task CreateCase_ReturnsProperAuthorization(string? role, HttpStatusCode expectedStatusCode)
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();

        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();

        using var client = factory.CreateClient();
        if (role is not null)
        {
            await SignInAsync(client, scope.ServiceProvider, role);
        }

        string title = $"Day 9 authorization test {Guid.NewGuid()}";

        try
        {
            using var response = await client.PostAsJsonAsync("/cases", new { title });
            Assert.Equal(expectedStatusCode, response.StatusCode);

            bool caseExists = await db.Cases.AnyAsync(c => c.Title == title);
            Assert.Equal(expectedStatusCode == HttpStatusCode.Created, caseExists);
        }
        finally
        {
            await db.Cases.Where(c => c.Title == title).ExecuteDeleteAsync();
        }
    }


    [Theory]
    [InlineData("Analyst", HttpStatusCode.NotFound)]
    [InlineData("SystemOwner", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task GetCase_WithMissingId_ReturnsProperAuthorization_AndNotFound(string? role, HttpStatusCode expectedStatuscode)
    {
        using var factory = new CaseApiFactory();
        using  var scope = factory.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();

        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();

        using var client = factory.CreateClient();

        if (role is not null)
        {
            await SignInAsync(client, scope.ServiceProvider, role);
        }

        string missingId = Guid.NewGuid().ToString();

        using var get = await client.GetAsync($"/cases/${missingId}");
        Assert.Equal(expectedStatuscode, get.StatusCode);
    }

    [Theory]
    [InlineData("Analyst", HttpStatusCode.BadRequest)]
    [InlineData("SystemOwner", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task CreateCase_WithBlankTitle_DoesNotInsertRow(string? role, HttpStatusCode expectedStatusCode)
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();

        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();

        using var client = factory.CreateClient();

        if (role is not null)
        {
            await SignInAsync(client, scope.ServiceProvider, role);
        }

        int countBefore = await db.Cases.CountAsync();
        using var response = await client.PostAsJsonAsync("/cases", new { title = " "});
        Assert.Equal(expectedStatusCode, response.StatusCode);

        int countAfter = await db.Cases.CountAsync();
        Assert.Equal(countBefore, countAfter);
    }

    /* Pagination Tests */

    [Theory]
    [InlineData("Analyst", HttpStatusCode.OK)]
    [InlineData("SystemOwner", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task GetCases_ReturnsPageOneWithSize(string? role, HttpStatusCode expectedStatusCode)
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();
        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();

        using var client = factory.CreateClient();

        if (role is not null)
        {
            await SignInAsync(client, scope.ServiceProvider, role);
        }

        using var get = await client.GetAsync("/cases");
        Assert.Equal(expectedStatusCode, get.StatusCode);
        if (expectedStatusCode == HttpStatusCode.OK)
        {
            var parsed = await get.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("1", parsed.GetProperty("page").ToString());
            Assert.Equal("20", parsed.GetProperty("pageSize").ToString());
        }

        // we probably need to test if non-authorized response doesn't contain anything.
    }

    [Fact]
    public async Task GetCases_WithNoData_WithPagination_ReturnsProperly()
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();
        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();

        using var client = factory.CreateClient();
        await SignInAsync(client, scope.ServiceProvider, "Analyst");

        using var get = await client.GetAsync("/cases?page=2&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var parsed = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2", parsed.GetProperty("page").ToString());
        Assert.Equal("5", parsed.GetProperty("pageSize").ToString());
    }

    [Theory]
    [InlineData("-1", "10")]
    [InlineData("1", "101")]
    [InlineData("1","-1")]
    [InlineData("0", "10")]
    [InlineData("a", "10")]
    [InlineData("1", "boo")]
    [InlineData("null", "10")]
    [InlineData("", "10")]
    [InlineData("1", "")]
    [InlineData("1", "null")]
    [InlineData("1073741825", "5")]
    public async Task GetCases_WithInvalidPagination_ReturnsBadRequest(string page, string pageSize)
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();
        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();

        using var client = factory.CreateClient();
        await SignInAsync(client, scope.ServiceProvider, "Analyst");
        using var get = await client.GetAsync($"/cases?page={page}&pageSize={pageSize}");
        Assert.Equal(HttpStatusCode.BadRequest, get.StatusCode);

        JsonElement body = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Invalid pagination parameter", body.GetProperty("error").ToString());
    }

    [Fact]
    public async Task GetCases_PageTwoWithSizeTwo_ReturnsOnlyThirdCase()
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();


        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();

        string[] fixtureIds = ["case-001", "case-002", "case-003"];
        try
        {
            db.Cases.AddRange(
                new CaseEntity{ Id = fixtureIds[2], Title = "Third" },
                new CaseEntity{ Id = fixtureIds[1], Title = "Second" },
                new CaseEntity{ Id = fixtureIds[0], Title = "First" }
            );

            await db.SaveChangesAsync();

            using var client = factory.CreateClient();
            await SignInAsync(client, scope.ServiceProvider, "Analyst");
            using var get = await client.GetAsync($"/cases?page=2&pageSize=2");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);

            CasesResponse? body = await get.Content.ReadFromJsonAsync<CasesResponse>();
            Assert.NotNull(body);

            Assert.Equal(2, body.Page);
            Assert.Equal(2, body.PageSize);

            var onlyCase = Assert.Single(body.Cases);
            Assert.Equal("case-003", onlyCase.Id);
            Assert.Equal("Third", onlyCase.Title);
            Assert.Equal("New", onlyCase.Status);
        }
        finally
        {
            await db.Cases
            .Where(c => fixtureIds.Contains(c.Id))
            .ExecuteDeleteAsync();
        }
    }

    /** Start Investigation test */
    [Fact]
    public async Task StartInvestigation_ReturnsNoContentOnFirstCall_ConflictOnSecondCall()
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();


        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();

        string caseId = Guid.NewGuid().ToString();

        db.Cases.Add(new CaseEntity{ Id= caseId, Title="First Case" });
        await db.SaveChangesAsync();

        try
        {
            using var client = factory.CreateClient();
            await SignInAsync(client, scope.ServiceProvider, "Analyst");

            var statusForCase = db.Cases
                .Where(c => c.Id == caseId)
                .Select(c => c.Status);

            Assert.Equal("New", await statusForCase.SingleAsync());

            string url = $"/cases/{caseId}/start-investigation";
            
            using var first = await client.PostAsync(url, content: null);
            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

            var activityForCase = db.CaseActivities
                .Where(a => a.CaseId == caseId)
                .Select(a => new { a.Id, a.Message });

            
            
            var firstActivity = await activityForCase.SingleAsync();

            Assert.Equal("Investigation started.", firstActivity.Message);
            Assert.Equal("Investigating", await statusForCase.SingleAsync());

            using var second = await client.PostAsync(url, content: null);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

            var activityAfterConflict = await activityForCase.SingleAsync();
            Assert.Equal(firstActivity.Id, activityAfterConflict.Id);
            Assert.Equal(firstActivity.Message, activityAfterConflict.Message);

            Assert.Equal("Investigating", await statusForCase.SingleAsync());
        }
        finally
        {
            await db.CaseActivities.Where(c => c.CaseId == caseId).ExecuteDeleteAsync();
            await db.Cases.Where(c => c.Id == caseId).ExecuteDeleteAsync();
        }
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("SystemOwner", HttpStatusCode.Forbidden)]
    [InlineData("Analyst", HttpStatusCode.NoContent)]
    public async Task StartInvestigation_ApplyProperAuthorization(string? role, HttpStatusCode expectedStatusCode)
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();


        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();

        string caseId = Guid.NewGuid().ToString();

        db.Cases.Add(new CaseEntity{ Id= caseId, Title="First Case" });
        await db.SaveChangesAsync();

        try
        {
            using var client = factory.CreateClient();
            if (role is not null)
            {
                await SignInAsync(client, scope.ServiceProvider, role);
            }

            var statusForCase = db.Cases
                .Where(c => c.Id == caseId)
                .Select(c => c.Status);

            Assert.Equal("New", await statusForCase.SingleAsync());

            string url = $"/cases/{caseId}/start-investigation";
            
            using var post = await client.PostAsync(url, content: null);
            Assert.Equal(expectedStatusCode, post.StatusCode);
            bool started = expectedStatusCode == HttpStatusCode.NoContent;
            Assert.Equal(
                started 
                ? "Investigating"
                : "New",
                await statusForCase.SingleAsync()
            );

            Assert.Equal(
                started
                ? 1
                : 0,
                await db.CaseActivities.CountAsync(a => a.CaseId == caseId)
            );
        }
        finally
        {
            await db.CaseActivities.Where(c => c.CaseId == caseId).ExecuteDeleteAsync();
            await db.Cases.Where(c => c.Id == caseId).ExecuteDeleteAsync();
        }
    }

    /** Request Closure tests */
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("SystemOwner", HttpStatusCode.Forbidden)]
    [InlineData("Analyst", HttpStatusCode.NoContent)]
    public async Task RequestClosure_ApplyProperAuthorization(string? role, HttpStatusCode expectedStatusCode)
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();

        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();
        var caseId = Guid.NewGuid().ToString();
        db.Cases.Add(new CaseEntity
        {
            Id=caseId,
            Title="Repeated login failures"
        });
        await db.SaveChangesAsync();
        try
        {
            Assert.True(
            await CaseTransitions
                .StartInvestigationAsync(db, caseId, "Investigation Started (test).")
            );

            Assert.Equal("Investigating", 
                await db.Cases
                    .Where(c => c.Id == caseId)
                    .Select(c => c.Status)
                    .SingleAsync());
            using var client = factory.CreateClient();
            if (role is not null)
            {
                await SignInAsync(client, scope.ServiceProvider, role);
            }
            string url = $"/cases/{caseId}/request-closure";
            using var post = await client.PostAsync(url, content: null);
            Assert.Equal(expectedStatusCode, post.StatusCode);
            Assert.Equal(role == "Analyst"
                ? "ClosureRequested"
                : "Investigating",
                await db.Cases.Where(c => c.Id == caseId)
                    .Select(c => c.Status)
                    .SingleAsync());
            Assert.Equal(
                role == "Analyst"
                ? 2
                : 1,
                await db.CaseActivities
                    .Where(a => a.CaseId == caseId)
                    .CountAsync()
            );
        }

        finally
        {
            await db.CaseActivities.Where(c => c.CaseId == caseId).ExecuteDeleteAsync();
            await db.Cases.Where(c => c.Id == caseId).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task RequestClosure_ChangesStatusToClosureRequested_AddCaseActivitiesEntry()
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();
        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();
        var caseId = Guid.NewGuid().ToString();
        db.Cases.Add(new CaseEntity
        {
            Id=caseId,
            Title="Repeated login failures"
        });
        await db.SaveChangesAsync();
        try
        {
            Assert.True(
            await CaseTransitions
                .StartInvestigationAsync(db, caseId, "Investigation Started (test).")
            );

            Assert.Equal("Investigating", 
                await db.Cases
                    .Where(c => c.Id == caseId)
                    .Select(c => c.Status)
                    .SingleAsync());
            using var client = factory.CreateClient();
            await SignInAsync(client, scope.ServiceProvider, "Analyst");
            string url = $"/cases/{caseId}/request-closure";
            using var post = await client.PostAsync(url, content: null);
            Assert.Equal(HttpStatusCode.NoContent, post.StatusCode);
            Assert.Equal("ClosureRequested",
                await db.Cases.Where(c => c.Id == caseId)
                    .Select(c => c.Status)
                    .SingleAsync());
            Assert.Equal(2,
                await db.CaseActivities
                    .Where(a => a.CaseId == caseId)
                    .CountAsync());
            }

        finally
        {
            await db.CaseActivities.Where(c => c.CaseId == caseId).ExecuteDeleteAsync();
            await db.Cases.Where(c => c.Id == caseId).ExecuteDeleteAsync();
        }
    }

    [Theory]
    [InlineData("New")]
    [InlineData("ClosureRequested")]
    [InlineData("Closed")]
    public async Task RequestClosure_FromInvalidStatus_ReturnsConflict(string currentStatus)
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();
        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();
        var caseId = Guid.NewGuid().ToString();
        db.Cases.Add(new CaseEntity
        {
            Id=caseId,
            Title="Repeated login failures",
            Status=currentStatus
        });
        await db.SaveChangesAsync();

        try
        {
            using var client = factory.CreateClient();
            await SignInAsync(client, scope.ServiceProvider, "Analyst");
            string url = $"/cases/{caseId}/request-closure";
            using var post = await client.PostAsync(url, content: null);
            Assert.Equal(HttpStatusCode.Conflict, post.StatusCode);
            Assert.Equal(currentStatus,
                await db.Cases.Where(c => c.Id == caseId)
                    .Select(c => c.Status)
                    .SingleAsync());
            Assert.Equal(0,
                await db.CaseActivities
                    .Where(a => a.CaseId == caseId)
                    .CountAsync());
            }

        finally
        {
            await db.CaseActivities.Where(c => c.CaseId == caseId).ExecuteDeleteAsync();
            await db.Cases.Where(c => c.Id == caseId).ExecuteDeleteAsync();
        }
    }
}