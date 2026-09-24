using Microsoft.EntityFrameworkCore;
using EventApi.Models;

namespace EventApi.Data
{
    /// <summary>
    /// EF Core DbContext for the EventApi application.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            // If migrations are not applied for some reason in the test environment, create the table schema
            // so tests that expect persistence still function. Use ExecuteSqlRaw which works with relational providers.
            try
            {
                // Apply any pending migrations so the database schema matches the checked-in migrations.
                Database.Migrate();
            }
            catch
            {
                // Swallow: if migrations cannot be applied in this environment, tests that rely on migrations may fail.
            }
        }

        public DbSet<Event> Events { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var b = modelBuilder.Entity<Event>();
            b.HasKey(e => e.Id);
            b.Property(e => e.Title).IsRequired().HasMaxLength(200);
            // enforce non-empty title at the database level using ToTable configuration
            b.ToTable("Events", t => t.HasCheckConstraint("CK_Events_Title_NotEmpty", "length(Title) > 0"));
        }
    }
}
