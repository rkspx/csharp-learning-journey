using Microsoft.EntityFrameworkCore;

namespace CaseApi.Data;

public static class CaseTransitions
{
    public static async Task<bool> StartInvestigationAsync(
        CaseDbContext db,
        string caseId,
        string activityMessage
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        int rowsChanged = await db.Cases
            .Where(c => c.Id == caseId && c.Status == "New")
            .ExecuteUpdateAsync(update => update
                .SetProperty(c => c.Status, "Investigating"));

        if (rowsChanged == 0)
        {
            await transaction.RollbackAsync();
            return false;
        }

        db.CaseActivities.Add(
            new CaseActivityEntity
            {
                CaseId = caseId,
                Message = activityMessage,
            }
        );

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }
}