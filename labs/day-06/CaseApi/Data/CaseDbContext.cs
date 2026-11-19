using Microsoft.EntityFrameworkCore;

namespace CaseApi.Data;

public class CaseDbContext: DbContext
{
    public CaseDbContext(DbContextOptions<CaseDbContext> options): base(options)
    {
        
    }

    public DbSet<CaseEntity> Cases => Set<CaseEntity>();
}