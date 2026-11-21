using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CaseApi.Tests;

public class BasicEndpointTests
{
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
        using var response = await client.PostAsync("/cases", content:null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "Title is required",
            body.GetProperty("error").GetString()
        );
    }
}
