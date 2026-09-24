using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/health", () => Results.Text("Healthy"));

app.Run();

/// <summary>
/// Program class used as entrypoint for WebApplicationFactory in tests.
/// </summary>
public partial class Program { }
