namespace Api.Services.Repositories
{
    /// <summary>
    /// Interface exposing methods for interacting with the 'Cart' entities in the database.
    /// </summary>
    public interface ICartRepository
    {
        /// <summary>
        /// Clears all the items in cart for the given user.
        /// </summary>
        /// <param name="userId">the user ID whose cart to wipe</param>
        Task ClearCartAsync(string userId);

        /// <summary>
        /// Retrieves the cart for the specified user.
        /// </summary>
        /// <param name="userId">the uesr ID whose cart to retrieve</param>
        /// <returns>a mapping of {concertID}-{ticketsCount} representing the items in the user's cart</returns>
        Task<IDictionary<string, int>> GetCartAsync(string userId);

        /// <summary>
        /// Updates the cart for the specified user with the specified number of tickets
        /// for the specified concert.
        /// </summary>
        /// <param name="userId">the user ID whose cart to update</param>
        /// <param name="concertId">the concert ID the new tickets are for</param>
        /// <param name="count">the number of tickets for the specified concert to update the cart with</param>
        Task<IDictionary<string, int>> UpdateCartAsync(string userId, string concertId, int count);
    }
}
