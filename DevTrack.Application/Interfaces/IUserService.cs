using DevTrack.Application.DTOs.Users;

namespace DevTrack.Application.Interfaces;

public interface IUserService
{
    Task<UserResponse?> GetByIdAsync(int userId);
    Task<UserResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<UserResponse> UpdateStatusAsync(int userId, bool isActive);
    Task<UserResponse> UpdateRoleAsync(int userId, string newRole);
    Task<IEnumerable<UserResponse>> GetAllAsync();
}