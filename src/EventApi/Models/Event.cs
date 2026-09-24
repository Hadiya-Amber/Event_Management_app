using System;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Models
{
    /// <summary>
    /// Represents an event that can be scheduled and attended.
    /// </summary>
    public class Event
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Title of the event. Required and non-empty.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        /// <summary>
        /// Optional longer description of the event.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Optional location for the event.
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// When the event starts (timezone-aware).
        /// </summary>
        public DateTimeOffset StartsAt { get; set; }

        /// <summary>
        /// When the event ends (timezone-aware).
        /// </summary>
        public DateTimeOffset EndsAt { get; set; }

        /// <summary>
        /// Capacity of attendees.
        /// </summary>
        public int Capacity { get; set; }
    }
}
