using WholeCareInsurance.api.DTOs.Auth;
using WholeCareInsurance.api.DTOs.Users;

namespace WholeCareInsurance.api.Services
{
    public interface IAuthService
    {
        Task<UserResponseDto?> Register(AuthRegisterDto dto);
        Task<AuthResponseDto?> Login(AuthLoginDto dto);
        Task<AuthResponseDto?> Refresh(string refreshToken);
        Task Logout(int userId);
    }
}
