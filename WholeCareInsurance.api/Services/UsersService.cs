using Microsoft.EntityFrameworkCore;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public class UsersService : IUsersService
    {
        private readonly AppDbContext _context;

        public UsersService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAll()
            => await _context.Users.AsNoTracking().ToListAsync();

        public async Task<User?> GetById(int id)
            => await _context.Users.FindAsync(id);

        public async Task<User?> GetByEmail(string email)
            => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User?> GetByRefreshTokenHash(string hash)
            => await _context.Users.FirstOrDefaultAsync(u => u.RefreshTokenHash == hash);

        public async Task<User?> GetByPasswordResetTokenHash(string hash)
            => await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetTokenHash == hash);

        public async Task<User> Create(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> Update(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task Delete(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
