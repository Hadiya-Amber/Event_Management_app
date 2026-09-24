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

var app = builder.Build();

app.MapGet("/health", () => Results.Text("Healthy"));

app.Run();

/// <summary>
/// Program class used as entrypoint for WebApplicationFactory in tests.
/// </summary>
public partial class Program { }
