using Microsoft.EntityFrameworkCore;

namespace CaseApi.Data;

public class CaseDbContext: DbContext
{
    public CaseDbContext(DbContextOptions<CaseDbContext> options): base(options)
    {
        
    }

    public DbSet<CaseEntity> Cases => Set<CaseEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var cases = modelBuilder.Entity<CaseEntity>();
        cases.HasKey(c => c.Id);
        cases.Property(c => c.Id)
            .HasColumnName("id")
            .IsRequired();  // NOT NULL

        cases.Property(c => c.Title)
            .HasColumnName("title")
            .IsRequired();  // NOT NULL

        cases.Property(c => c.Status)
            .HasColumnName("status")
            .IsRequired() // NOT NULL
            .HasDefaultValue("New"); // DEFAULT 'New'


        cases.ToTable("cases", table =>
        {
            // CONSTRAINT cases_title_not_blank CHECK(length(trim(title)) > 0)
            table.HasCheckConstraint(
                "cases_title_not_blank",
                "length(trim(title)) > 0"
            );

            // CONSTRAINT cases_status_allowed CHECK(status IN ('New', 'Investigating', 'ClosureRequested', 'Closed'))
            table.HasCheckConstraint(
                "cases_status_allowed",
                "status IN ('New', 'Investigating', 'ClosureRequested', 'Closed')"
            );
        });

    }
}