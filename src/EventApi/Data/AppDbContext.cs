using Microsoft.EntityFrameworkCore;
using EventApi.Models;

namespace EventApi.Data
{
    /// <summary>
    /// EF Core DbContext for the EventApi application.
    /// </summary>
    public class AppDbContext : DbContext
    {
                /// <summary>
        /// Creates the context. Migrations are applied once at startup
        /// (Program.cs), not here: a constructor runs for every context the
        /// container creates, and a migration failure must not be swallowed.
        /// </summary>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
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
