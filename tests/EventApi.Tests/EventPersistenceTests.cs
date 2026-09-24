using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using EventApi.Data;
using EventApi.Models;

namespace EventApi.Tests
{
    public class EventPersistenceTests
    {
        private static string TempDbFile()
        {
            var path = Path.Combine(Path.GetTempPath(), $"eventapi_{Guid.NewGuid():N}.db");
            return path;
        }

        [Fact]
        public void MigrationCreatesEventsTable()
        {
            var dbFile = TempDbFile();
            try
            {
                var connString = $"Data Source={dbFile}";
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(connString)
                    .Options;

                using (var ctx = new AppDbContext(options))
                {
                    ctx.Database.Migrate();
                }

                using (var conn = new SqliteConnection(connString))
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "PRAGMA table_info('Events');";
                    using var reader = cmd.ExecuteReader();
                    bool hasId = false, hasTitle = false, hasDescription = false, hasLocation = false, hasStartsAt = false, hasEndsAt = false, hasCapacity = false;
                    while (reader.Read())
                    {
                        var name = reader.GetString(1);
                        switch (name)
                        {
                            case "Id": hasId = true; break;
                            case "Title": hasTitle = true; break;
                            case "Description": hasDescription = true; break;
                            case "Location": hasLocation = true; break;
                            case "StartsAt": hasStartsAt = true; break;
                            case "EndsAt": hasEndsAt = true; break;
                            case "Capacity": hasCapacity = true; break;
                        }
                    }

                    Assert.True(hasId, "Id column missing");
                    Assert.True(hasTitle, "Title column missing");
                    Assert.True(hasDescription, "Description column missing");
                    Assert.True(hasLocation, "Location column missing");
                    Assert.True(hasStartsAt, "StartsAt column missing");
                    Assert.True(hasEndsAt, "EndsAt column missing");
                    Assert.True(hasCapacity, "Capacity column missing");
                }
            }
            finally
            {
                if (File.Exists(dbFile)) File.Delete(dbFile);
            }
        }

        [Fact]
        public void RoundtripPersistsAllFields()
        {
            var dbFile = TempDbFile();
            try
            {
                var connString = $"Data Source={dbFile}";
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(connString)
                    .Options;

                var ev = new Event
                {
                    Title = "Concert",
                    Description = "Live performance",
                    Location = "Stadium",
                    StartsAt = DateTimeOffset.UtcNow,
                    EndsAt = DateTimeOffset.UtcNow.AddHours(2),
                    Capacity = 5000
                };

                using (var ctx = new AppDbContext(options))
                {
                    ctx.Database.Migrate();
                    ctx.Events.Add(ev);
                    ctx.SaveChanges();
                }

                using (var ctx2 = new AppDbContext(options))
                {
                    var got = ctx2.Events.Find(ev.Id) ?? throw new Exception("Event not found");
                    Assert.Equal(ev.Title, got.Title);
                    Assert.Equal(ev.Description, got.Description);
                    Assert.Equal(ev.Location, got.Location);
                    Assert.Equal(ev.StartsAt.ToUniversalTime(), got.StartsAt.ToUniversalTime());
                    Assert.Equal(ev.EndsAt.ToUniversalTime(), got.EndsAt.ToUniversalTime());
                    Assert.Equal(ev.Capacity, got.Capacity);
                }
            }
            finally
            {
                if (File.Exists(dbFile)) File.Delete(dbFile);
            }
        }

        [Fact]
        public void SaveRejectsNullOrEmptyTitle()
        {
            var dbFile = TempDbFile();
            try
            {
                var connString = $"Data Source={dbFile}";
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(connString)
                    .Options;

                using (var ctx = new AppDbContext(options))
                {
                    ctx.Database.Migrate();
                }

                // Null title
                using (var ctx = new AppDbContext(options))
                {
                    var ev = new Event { Title = null!, StartsAt = DateTimeOffset.UtcNow, EndsAt = DateTimeOffset.UtcNow.AddHours(1), Capacity = 10 };
                    ctx.Events.Add(ev);
                    Assert.ThrowsAny<Exception>(() => ctx.SaveChanges());
                }

                // Empty title
                using (var ctx = new AppDbContext(options))
                {
                    var ev = new Event { Title = "", StartsAt = DateTimeOffset.UtcNow, EndsAt = DateTimeOffset.UtcNow.AddHours(1), Capacity = 10 };
                    ctx.Events.Add(ev);
                    Assert.ThrowsAny<Exception>(() => ctx.SaveChanges());
                }
            }
            finally
            {
                if (File.Exists(dbFile)) File.Delete(dbFile);
            }
        }
    }
}
