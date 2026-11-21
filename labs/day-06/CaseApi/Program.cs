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

builder.Services.AddOpenApi();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Hello World!");
app.MapGet("/cases/count", async (CaseDbContext db) =>
{
    int count = await db.Cases.CountAsync();
    return Results.Ok(new { count });
});

app.MapGet("/cases", async (CaseDbContext db, string? page, string? pageSize) =>
{
    int currentPage = 1;
    int size = 20;
     if ((
            page is not null 
            && !int.TryParse(page, out currentPage)) 
        || (
            pageSize is not null 
            && !int.TryParse(pageSize, out size)) 
        || currentPage < 1 
        || size < 1 
        || size > 100)
    {
        return Results.BadRequest(new { error = "Invalid pagination parameter" });
    }

    long offset = ((long) currentPage -1) * size;
    if (offset > int.MaxValue)
    {
        return Results.BadRequest(new { error = "Invalid pagination parameter" });
    }

    List<CaseResponse> cases = await db.Cases
    .OrderBy(c => c.Id)
    .Skip((int)offset)
    .Take(size)
    .Select(c => new CaseResponse(c.Id,c.Title,c.Status))
    .ToListAsync();

    CasesResponse response = new(cases, currentPage, size);

    return Results.Ok(response);
})
.WithSummary("List cases by ID")
.WithDescription(
    "Returns cases sorted by Id in ascending order. Page starts at 1 and defaults to 1. " +
    "PageSize defaults to 20 and accepts values from 1 to 100. " +
    "A valid page beyond the last case returns an empty cases array. " +
    "Invalid pagination values or an offset above 2147483647 return 400."
)
.Produces<CaseResponse>(StatusCodes.Status200OK)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

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