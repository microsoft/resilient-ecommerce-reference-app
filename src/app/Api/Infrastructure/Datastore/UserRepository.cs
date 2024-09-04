using Api.Models.Entities;
using Api.Services.Repositories;
using Api.Services.Repositories.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Datastore
{
    /// <summary>
    /// Implementation of the ticket data model repository that uses the Entity Framework.
    /// </summary>
    public class UserRepository : IUserRepository, IDisposable
    {
        private readonly DatabaseContext _database;

        public UserRepository(DatabaseContext database)
        {
            _database = database;
        }

        /// <inheritdoc />
        public async Task<User> CreateUserAsync(User user)
        {
            _database.Users.Add(user);
            await _database.SaveChangesAsync();

            return user;
        }

        /// <inheritdoc />
        public async Task<User> UpdateUserAsync(User user)
        {
            var updatedUser = await _database.Users.FindAsync(user.Id);
            if (updatedUser == null)
            {
                throw new NotFoundException($"Can't update user info. User with id '{user.Id}' not found");
            }

            updatedUser.DisplayName = user.DisplayName;
            updatedUser.Email = user.Email;
            updatedUser.Phone = user.Phone;

            await _database.SaveChangesAsync();
            return updatedUser;
        }

        /// <inheritdoc />
        public async Task<User> GetUserByIdAsync(string userId)
        {
            var user = await _database.Users.AsNoTracking().Where(user => user.Id == userId).SingleOrDefaultAsync();
            return user ?? throw new NotFoundException($"User with id '{userId}' does not exist");
        }

        public void Dispose() => _database?.Dispose();
    }
}
