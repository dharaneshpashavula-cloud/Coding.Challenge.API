using System;
using System.ComponentModel.DataAnnotations;

namespace Coding.Challenge.API.Models
{
    /// <summary>
    /// Represents an event that failed to be processed and was moved to a dead-letter store for later inspection.
    /// </summary>
    public class DeadLetter
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
        public string Type { get; set; } = string.Empty;

        public string? Error { get; set; }
    }
}
