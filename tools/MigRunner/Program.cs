using System;
using Microsoft.EntityFrameworkCore;
using EventApi.Data;

class P
{
    static int Main(string[] args)
    {
        var dbFile = args.Length > 0 ? args[0] : ".migrtest.db";
        var conn = $"Data Source={dbFile}";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        using var ctx = new AppDbContext(options);
        ctx.Database.Migrate();
        Console.WriteLine("Migrations applied to " + dbFile);
        return 0;
    }
}
