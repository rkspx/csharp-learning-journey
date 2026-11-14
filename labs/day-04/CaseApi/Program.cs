using Microsoft.Data.Sqlite;
using CaseApi;

var builder = WebApplication.CreateBuilder(args);


string databasePath = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "data", "cases.db")
);

string connectionString = new SqliteConnectionStringBuilder
{   
    DataSource=databasePath,
    Mode=SqliteOpenMode.ReadWrite,
}.ToString();

builder.Services.AddScoped<CaseRepository>(serviceProvider =>
{
    return new CaseRepository(connectionString);
});


var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGet("/health", () => "Healthy");
app.MapGet("/cases/count", (CaseRepository repository) =>
{   
    long count = repository.Count();
    return Results.Ok(new {count});
});

app.MapGet("/cases/", (CaseRepository repository) =>
{
    List<CaseResponse> cases = repository.List();
    return Results.Ok(cases);
});

app.MapGet("/cases/{id}", (CaseRepository repository, string id) =>
{
     
    CaseResponse? response = repository.Get(id);

    if (response is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(response);
});

app.MapPost("/cases", (CaseRepository repository, CreateCaseRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { error = "Title is required."});
    }
     

    CaseResponse caseResponse = repository.Create(request);
    return Results.Created($"/cases/{caseResponse.Id}", caseResponse);
});

app.Run();
