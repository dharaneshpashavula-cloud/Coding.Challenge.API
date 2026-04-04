using System;
using System.ComponentModel.DataAnnotations;

namespace Coding.Challenge.API.DTOs
{
    /// <summary>
    /// DTO for clients to POST sensor events to the API (optional endpoint we expose for manual testing).
    /// </summary>
    public record SensorEventDto(
        [Required, MaxLength(100)] string Gate,
        [Required] DateTime Timestamp,
        [Required, Range(0, int.MaxValue)] int NumberOfPeople,
        [Required, MaxLength(20)] string Type);

    public record AggregatedResultDto(string Gate, string Type, int NumberOfPeople);
}
