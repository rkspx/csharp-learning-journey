using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using System.Net.Http.Headers;
using Xunit;

namespace CaseApi.Tests;

public class BasicEndpointTests
{

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
    [Fact]
    public async Task GetRoot_ReturnsOkAndGreeting()
    {
        using var factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:CaseDatabase",
                "Host=127.0.0.1;Port=1;Database=unused;Username=unused"
            );
        });

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/");
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello World!", body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateCase_WithInvalidTitle_ReturnsBadRequest(string? title)
    {
        using var factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionString:CaseDatabase",
                "Host=127.0.0.1;Port=1;Database=unused;Username=unused"
            );
        });

        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        await SignInAsync(client, scope.ServiceProvider, "Analyst");
        using var response = await client.PostAsJsonAsync("/cases", new { title });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            "Title is required",
            body.GetProperty("error").GetString()
        );
    }

    [Fact]
    public async Task CreateCase_WithNullBody_returnsBadRequest()
    {
        using var factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
           builder.UseSetting(
                "ConnectionString:CaseDatabase",
                "Host=127.0.0.1;Port=1;Database=unused;Username=unused"
            );
        });

        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        await SignInAsync(client, scope.ServiceProvider, "Analyst");
        using var response = await client.PostAsync("/cases", content:null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "Title is required",
            body.GetProperty("error").GetString()
        );
    }
}
