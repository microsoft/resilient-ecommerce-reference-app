using Api.Models.Entities;

namespace Api.Services.Repositories
{
    /// <summary>
    /// Interface exposing methods for interacting with the 'User' entities in the database.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Creates a new user, as specified by the provided user entity.
        /// </summary>
        /// <param name="user">the user model representation of the new user to create</param>
        /// <returns>the tracked user entity that has been created</returns>
        Task<User> CreateUserAsync(User user);

        /// <summary>
        /// Updates an existing user.
        /// </summary>
        /// <param name="user">the user entity to update. Contains all the new attributes to update, alongside the ID</param>
        /// <returns>the tracked, updated user model entity</returns>
        Task<User> UpdateUserAsync(User user);

        /// <summary>
        /// Retrieves a user by their ID.
        /// </summary>
        /// <param name="id">the ID of the user to retrieve</param>
        /// <returns>the tracked user entity found</returns>
        /// <exception cref="NotFoundException">if the user entity could not be found</exception>"
        Task<User> GetUserByIdAsync(string id);
    }
}
