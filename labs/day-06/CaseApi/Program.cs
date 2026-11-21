using CaseApi;
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
app.MapGet("/cases/count", async (CaseDbContext db) =>
{
    int count = await db.Cases.CountAsync();
    return Results.Ok(new { count });
});

app.MapGet("/cases", async (CaseDbContext db) =>
{
    List<CaseResponse> cases = await db.Cases
    .OrderBy(c => c.Id)
    .Select(c => new CaseResponse(
        c.Id,
        c.Title,
        c.Status)).ToListAsync();

    return Results.Ok(cases);
});

app.MapPost("/cases", async(CreateCaseRequest? request, CaseDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request?.Title))
    {
        return Results.BadRequest(new
        {
            error = "Title is required"
        });
    }

    CaseEntity entity = new()
    {
        Title = request.Title.Trim()
    };

    db.Cases.Add(entity);
    await db.SaveChangesAsync();
    CaseResponse response = new(
        entity.Id,
        entity.Title,
        entity.Status
    );

    return Results.Created($"/cases/{entity.Id}", response);
});

app.MapGet("/cases/{id}", async (
    string id, CaseDbContext db) =>
{
    CaseResponse? response = await db.Cases
    .Where(c => c.Id == id)
    .Select(c => new CaseResponse(
        c.Id,
        c.Title,
        c.Status)).SingleOrDefaultAsync();

    if (response is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(response);
});

app.Run();

public partial class Program
{
    
}