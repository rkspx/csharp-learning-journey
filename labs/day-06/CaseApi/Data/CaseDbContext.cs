using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CaseApi.Data;

public class CaseDbContext: IdentityDbContext<IdentityUser>
{
    public CaseDbContext(DbContextOptions<CaseDbContext> options): base(options)
    {
        
    }

    public DbSet<CaseEntity> Cases => Set<CaseEntity>();
    public DbSet<CaseActivityEntity> CaseActivities => Set<CaseActivityEntity>();

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

        cases.Property(c => c.AssignedTo)
            .HasColumnName("assigned_to")
            .IsRequired(false);


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

        var activity = modelBuilder.Entity<CaseActivityEntity>();
        activity.ToTable("case_activity", table =>
        {
            table.HasCheckConstraint(
                "activity_message_not_blank",
                "length(trim(message)) > 0"
            );
        });

        activity.HasKey(a => a.Id);
        activity.Property(a => a.Id)
            .HasColumnName("id")
            .IsRequired();

        activity.Property(a => a.CaseId)
            .HasColumnName("case_id")
            .IsRequired();

        activity.Property(a => a.Message)
            .HasColumnName("message")
            .IsRequired();

        activity.HasOne<CaseEntity>()
            .WithMany()
            .HasForeignKey(a => a.CaseId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}