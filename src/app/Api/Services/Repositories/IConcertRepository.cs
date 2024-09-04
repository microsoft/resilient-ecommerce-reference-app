using Api.Models.Entities;

namespace Api.Services.Repositories
{
    /// <summary>
    /// Interface exposing methods for interacting with the 'Concert' entities in the database.
    /// </summary>
    public interface IConcertRepository
    {
        /// <summary>
        /// Creates and stores a new concert entry in the database.
        /// </summary>
        /// <param name="newConcert">the concert entity representation to store</param>
        /// <returns>the newly created and tracked entity model in the DB</returns>
        /// <exception cref="NotFoundException">if the concert entity could not be found</exception>
        Task<Concert> CreateConcertAsync(Concert newConcert);

        /// <summary>
        /// Updates an existing concert entry in the database.
        /// </summary>
        /// <param name="newConcert">the concert entity representation to update, containing the updates to apply</param>
        /// <returns>the newly updated and tracked entity model in the DB</returns>
        /// <exception cref="NotFoundException">if the concert entity could not be found</exception>
        Task<Concert> UpdateConcertAsync(Concert model);

        /// <summary>
        /// Deletes the concert entry with the specified ID from the database.
        /// </summary>
        /// <param name="concertId">the ID of the concert to delete</param>
        /// <exception cref="NotFoundException">if the user entity could not be found</exception>
        Task DeleteConcertAsync(string concertId);

        /// <summary>
        /// Retrieves the concert entry with the specified ID from the database.
        /// </summary>
        /// <param name="concertId"></param>
        /// <returns>the tracked concert entity</returns>
        /// <exception cref="NotFoundException">if the user entity could not be found</exception>
        Task<Concert> GetConcertByIdAsync(string concertId);

        /// <summary>
        /// Retrieves the latest upcoming concerts from the database.
        /// </summary>
        /// <param name="count">the maximum number of concerts to retrieve</param>
        /// <returns>the concert model entities of the first upcoming concerts</returns>
        Task<IEnumerable<Concert>> GetUpcomingConcertsAsync(int count);
    }
}
