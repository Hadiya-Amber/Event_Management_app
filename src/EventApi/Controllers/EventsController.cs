using Microsoft.AspNetCore.Mvc;
using EventApi.Data;
using EventApi.Models;
using EventApi.Dtos;
using System.Threading.Tasks;

namespace EventApi.Controllers
{
        /// <summary>
        /// Controller exposing CRUD operations for Events.
        /// </summary>
    [ApiController]
    [Route("api/events")]
        public class EventsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EventsController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Retrieves an Event by id.
        /// </summary>
        /// <param name="id">Event id</param>
        /// <returns>The Event if found or 404 Not Found.</returns>
        [HttpGet("{id:int}")]
        public ActionResult<Event> Get(int id)
        {
            var ev = _db.Events.Find(id);
            if (ev == null) return NotFound();
            return Ok(ev);
        }

        /// <summary>
        /// Creates a new Event from the provided payload.
        /// Returns 201 Created with the stored entity and Location header.
        /// </summary>
        /// <param name="request">Create payload</param>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var ev = new Event
            {
                Title = request.Title,
                Description = request.Description,
                Location = request.Location,
                StartsAt = request.StartsAt,
                EndsAt = request.EndsAt,
                Capacity = request.Capacity
            };

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = ev.Id }, ev);
        }
    }
}
