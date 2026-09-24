using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using EventApi.Data;
using EventApi.Models;

namespace EventApi.Tests
{
    public class CreateEventTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _baseFactory;

        public CreateEventTests(WebApplicationFactory<Program> factory)
        {
            _baseFactory = factory;
        }

        [Fact]
        public async Task PostEvent_CreatesEventAndPersists()
        {
            // Unique in-memory database name shared between server and test context
            var dbName = Guid.NewGuid().ToString();

            // Create a factory that replaces the app's DbContext with an InMemory one
            var factory = _baseFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing AppDbContext registrations (DbContextOptions and AppDbContext)
                    var descriptors = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                        d.ServiceType == typeof(AppDbContext) ||
                        d.ImplementationType == typeof(AppDbContext)
                    ).ToList();

                    foreach (var d in descriptors)
                    {
                        services.Remove(d);
                    }

                    // Register AppDbContext using InMemory with the shared name
                    services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
                });
            });

            var client = factory.CreateClient();

            // Prepare payload
            var startsAt = DateTimeOffset.UtcNow.AddHours(1).TrimToSeconds();
            var endsAt = startsAt.AddHours(2).TrimToSeconds();

            var title = "Integration Test Event";
            var description = "Created by integration test";
            var location = "Test Venue";
            var capacity = 42;

            var payload = new
            {
                Title = title,
                Description = description,
                Location = location,
                StartsAt = startsAt,
                EndsAt = endsAt,
                Capacity = capacity
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = null });

            // Act: POST to create a new event
            var response = await client.PostAsync("/api/events", new StringContent(json, Encoding.UTF8, "application/json"));

            // Assert: status 201 Created
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // Assert: response body has Id > 0 and returned fields match
            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // Helper to read either PascalCase or camelCase property
            string ReadString(string name)
            {
                if (root.TryGetProperty(name, out var p) && p.ValueKind != JsonValueKind.Null) return p.GetString()!;
                var camel = char.ToLowerInvariant(name[0]) + name.Substring(1);
                if (root.TryGetProperty(camel, out p) && p.ValueKind != JsonValueKind.Null) return p.GetString()!;
                throw new Xunit.Sdk.XunitException($"Property '{name}' not found in response JSON. Response: {responseBody}");
            }

            DateTimeOffset ReadDateTimeOffset(string name)
            {
                if (root.TryGetProperty(name, out var p)) return p.GetDateTimeOffset();
                var camel = char.ToLowerInvariant(name[0]) + name.Substring(1);
                if (root.TryGetProperty(camel, out p)) return p.GetDateTimeOffset();
                throw new Xunit.Sdk.XunitException($"Property '{name}' not found in response JSON.");
            }

            int id;
            if (root.TryGetProperty("Id", out var idProp))
            {
                id = idProp.GetInt32();
            }
            else if (root.TryGetProperty("id", out idProp))
            {
                id = idProp.GetInt32();
            }
            else
            {
                throw new Xunit.Sdk.XunitException($"Id property missing in response JSON. Response: {responseBody}");
            }

            Assert.True(id > 0, "Returned Id should be greater than zero.");

            // Field assertions
            Assert.Equal(title, ReadString("Title"));
            Assert.Equal(description, ReadString("Description"));
            Assert.Equal(location, ReadString("Location"));

            var returnedStarts = ReadDateTimeOffset("StartsAt");
            var returnedEnds = ReadDateTimeOffset("EndsAt");
            Assert.Equal(startsAt, returnedStarts);
            Assert.Equal(endsAt, returnedEnds);

            int returnedCapacity;
            if (root.TryGetProperty("Capacity", out var capProp)) returnedCapacity = capProp.GetInt32();
            else if (root.TryGetProperty("capacity", out capProp)) returnedCapacity = capProp.GetInt32();
            else throw new Xunit.Sdk.XunitException("Capacity property missing in response JSON.");

            Assert.Equal(capacity, returnedCapacity);

            // Assert: Location header ends with /api/events/{id}
            Assert.NotNull(response.Headers.Location);
            var path = response.Headers.Location!.AbsolutePath;
            Assert.EndsWith($"/api/events/{id}", path);

            // Persistence check: create a fresh AppDbContext that uses the same InMemory database name
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options;
            await using (var db = new AppDbContext(options))
            {
                var events = await db.Events.ToListAsync();
                // Exactly one Event with the returned id exists
                var matching = events.Where(e => e.Id == id).ToList();
                Assert.Single(matching);

                var stored = matching.Single();
                Assert.Equal(id, stored.Id);
                Assert.Equal(title, stored.Title);
                Assert.Equal(description, stored.Description);
                Assert.Equal(location, stored.Location);
                Assert.Equal(startsAt, stored.StartsAt);
                Assert.Equal(endsAt, stored.EndsAt);
                Assert.Equal(capacity, stored.Capacity);
            }
        }
    }

    internal static class JsonElementExtensions
    {
        public static DateTimeOffset GetDateTimeOffset(this JsonElement element)
        {
            // Parse as ISO 8601 string
            if (element.ValueKind == JsonValueKind.String)
            {
                var s = element.GetString()!;
                return DateTimeOffset.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind);
            }

            // If it's a number, treat as Unix milliseconds (not expected here but defensive)
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var v))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(v);
            }

            throw new FormatException("Unable to convert JsonElement to DateTimeOffset.");
        }

        public static DateTimeOffset TrimToSeconds(this DateTimeOffset dto)
        {
            return new DateTimeOffset(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, dto.Offset);
        }
    }
}
