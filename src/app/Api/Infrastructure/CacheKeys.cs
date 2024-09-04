namespace Api.Infrastructure
{
    /// <summary>
    /// Defines and uniformizes all the caching keys used in the application.
    /// </summary>
    public static class CacheKeys
    {
        // Caching key referencing the IDs of all the upcoming concerts.
        // The <see cref="IConcertRepository"/> implementation handles what concerts are considered "upcoming".
        public const string UpcomingConcerts = "UpcomingConcerts";

        // Caching key referencing the cart of a specific user.
        // The <see cref="ICartRepository"/> implementation handles how cart items are stored in the cache.
        public static readonly Func<string, string> Cart = (userId) => $"Cart_{userId}";
    }
}
