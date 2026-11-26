using CaseApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CaseApi.Tests;

[Collection("Case database")]
public class DatabaseTests
{

    private readonly string _dbName = "case_api_tests";
    
    [Fact]
    public async Task TestDatabase_AppliesCaseMigrations()
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();

        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();
        var applied = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains("20261119073210_InitialCreate", applied);
        Assert.Contains("20261119122506_AddCaseAssignedTo", applied);
    }

    [Fact]
    public async Task StartInvestigation_WithEmptyMesage_Throws()
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();

        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();

        using var client = factory.CreateClient();

        string caseId = Guid.NewGuid().ToString();
        var cs = await db.AddAsync(new CaseEntity
        {
            Id = caseId,
            Title = "Repeated Login"
        });

        await db.SaveChangesAsync();

        try
        {
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                CaseTransitions.StartInvestigationAsync(db, caseId, ""));

            Assert.Equal("New", await db.Cases.Where(c => c.Id == caseId).Select(c => c.Status).SingleAsync());
            Assert.Equal(0, await db.CaseActivities.CountAsync(a => a.CaseId == caseId));
        
        }
        finally
        {            
            await db.Cases.Where(c => c.Id == caseId).ExecuteDeleteAsync();
        }

    }

    [Fact]
    public async Task StartInvestgation_WithMessage_ChangesStatus_AndAddCaseActivityEntry()
    {
        using var factory = new CaseApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CaseDbContext>();

        Assert.Equal(_dbName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();

        using var client = factory.CreateClient();

        string caseId = Guid.NewGuid().ToString();
        var cs = await db.AddAsync(new CaseEntity
        {
            Id = caseId,
            Title = "Repeated Login"
        });

        await db.SaveChangesAsync();

        try
        {
            var updated = await CaseTransitions.StartInvestigationAsync(db, caseId, "Case confirmed");
            Assert.True(updated);
            var savedStatus = await db.Cases
                .Where(c => c.Id == caseId)
                .Select(c => c.Status)
                .SingleAsync();
            Assert.Equal("Investigating", savedStatus);

            var messages = await db.CaseActivities
                .Where(a => a.CaseId == caseId)
                .Select(a => a.Message)
                .ToListAsync();

            Assert.Equal("Case confirmed", Assert.Single(messages));
        } 
        finally
        {
            await db.CaseActivities.Where(c => c.CaseId == caseId).ExecuteDeleteAsync();
            await db.Cases.Where(c => c.Id == caseId).ExecuteDeleteAsync();
        }
    }
}