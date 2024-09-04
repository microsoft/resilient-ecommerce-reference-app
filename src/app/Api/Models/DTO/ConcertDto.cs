namespace Api.Models.DTO
{
    /// <summary>
    /// The representation of a concert.
    /// This is the model the API reports to clients.
    /// </summary>
    public class ConcertDto
    {
        public required string Id { get; set; }
        public string Artist { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double Price { get; set; }
        public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow.AddDays(30);
    }
}
