using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EventApi.Data;

var builder = WebApplication.CreateBuilder(args);

// add configuration for DbContext; tests may override the connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=eventapi.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

// Register controllers so MVC controllers are available to the test host
builder.Services.AddControllers();

var app = builder.Build();

// Bring the database up to the checked-in migrations once, at startup, so the
// SQLite file exists before the first request and a migration failure stops
// the app loudly instead of surfacing later as a missing table.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Only apply migrations when using a relational provider. InMemory provider
    // doesn't support migrations and is used by tests.
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
}


app.MapGet("/health", () => Results.Text("Healthy"));

// Map controller routes
app.MapControllers();

app.Run();

/// <summary>
/// Program class used as entrypoint for WebApplicationFactory in tests.
/// </summary>
public partial class Program { }
