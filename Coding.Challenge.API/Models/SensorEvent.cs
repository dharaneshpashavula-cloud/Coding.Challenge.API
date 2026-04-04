using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coding.Challenge.API.Models
{
    /// <summary>
    /// Represents a single sensor event produced by a gate sensor.
    /// This event records the gate name, timestamp, number of people and type (enter/leave).
    /// </summary>
    public class SensorEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Gate { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        public int NumberOfPeople { get; set; }

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty; // "enter" or "leave"
    }
}
