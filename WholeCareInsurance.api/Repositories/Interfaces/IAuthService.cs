using WholeCareInsurance.api.DTOs.Auth;
using WholeCareInsurance.api.DTOs.Users;

namespace WholeCareInsurance.api.Services
{
    public enum ChangePasswordResult
    {
        Success,
        UserNotFound,
        InvalidCurrentPassword
    }

    public interface IAuthService
    {
        Task<UserResponseDto?> Register(AuthRegisterDto dto);
        Task<AuthResponseDto?> Login(AuthLoginDto dto);
        Task<AuthResponseDto?> Refresh(string refreshToken);
        Task Logout(int userId);
        Task<ChangePasswordResult> ChangePassword(int userId, string currentPassword, string newPassword);
        Task ForgotPassword(string email);
        Task<bool> ResetPassword(string token, string newPassword);
    }
}
