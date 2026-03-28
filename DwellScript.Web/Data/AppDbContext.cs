using DwellScript.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DwellScript.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Generation> Generations => Set<Generation>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Property>(e =>
        {
            e.HasOne(p => p.User)
             .WithMany(u => u.Properties)
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(p => p.MonthlyRent).HasColumnType("decimal(18,2)");
            e.Property(p => p.Bathrooms).HasColumnType("decimal(4,1)");
        });

        builder.Entity<Generation>(e =>
        {
            e.HasOne(g => g.Property)
             .WithMany(p => p.Generations)
             .HasForeignKey(g => g.PropertyId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(g => g.UsageUnitsConsumed).HasColumnType("decimal(5,2)");
        });

        builder.Entity<MagicLinkToken>(e =>
        {
            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
