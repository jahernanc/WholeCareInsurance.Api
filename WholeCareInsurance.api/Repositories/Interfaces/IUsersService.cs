using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public interface IUsersService
    {
        Task<IEnumerable<User>> GetAll();
        Task<User?> GetById(int id);
        Task<User?> GetByEmail(string email);
        Task<User?> GetByRefreshTokenHash(string hash);
        Task<User?> GetByPasswordResetTokenHash(string hash);
        Task<User> Create(User user);
        Task<User> Update(User user);
        Task Delete(User user);
    }
}
