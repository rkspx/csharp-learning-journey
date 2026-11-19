using CaseApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
string connectionString = 
    builder.Configuration.GetConnectionString("CaseDatabase") 
    ?? throw new InvalidOperationException(
        "Connection string 'CaseDatabase' is missing.");

builder.Services.AddDbContext<CaseDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
