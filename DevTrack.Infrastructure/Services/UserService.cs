using DevTrack.Application.DTOs.Users;
using DevTrack.Application.Interfaces;
using DevTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly DevTrackDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(DevTrackDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserResponse?> GetByIdAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user is null ? null : UserResponse.FromEntity(user);
    }
    public async Task<UserResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new InvalidOperationException("Full name is required.");

        user.FullName = request.FullName;
        user.ProfileImageUrl = request.ProfileImageUrl;

        await _db.SaveChangesAsync();

        return UserResponse.FromEntity(user);
    }
    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        if (!_passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
            throw new InvalidOperationException("Current password is incorrect.");

        if (request.NewPassword != request.ConfirmNewPassword)
            throw new InvalidOperationException("New password and confirmation do not match.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync();
    }
    public async Task<IEnumerable<UserResponse>> GetAllAsync()
    {
        var users = await _db.Users.AsNoTracking().ToListAsync();
        return users.Select(UserResponse.FromEntity);
    }
    public async Task<UserResponse> UpdateStatusAsync(int userId, bool isActive)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        if (!isActive && user.Role == Domain.Enums.UserRole.Admin)
        {
            var activeAdminCount = await _db.Users
                .CountAsync(u => u.Role == Domain.Enums.UserRole.Admin && u.IsActive);

            if (activeAdminCount <= 1)
                throw new InvalidOperationException("Cannot deactivate the last active admin.");
        }

        user.IsActive = isActive;
        await _db.SaveChangesAsync();
        return UserResponse.FromEntity(user);
    }
    public async Task<UserResponse> UpdateRoleAsync(int userId, string newRole)
    {
        if (!Enum.TryParse<Domain.Enums.UserRole>(newRole, ignoreCase: true, out var parsedRole))
            throw new InvalidOperationException($"'{newRole}' is not a valid role.");

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        if (user.Role == Domain.Enums.UserRole.Admin && parsedRole != Domain.Enums.UserRole.Admin)
        {
            var activeAdminCount = await _db.Users
                .CountAsync(u => u.Role == Domain.Enums.UserRole.Admin && u.IsActive);

            if (activeAdminCount <= 1)
                throw new InvalidOperationException("Cannot remove the role of the last active admin.");
        }

        user.Role = parsedRole;
        await _db.SaveChangesAsync();
        return UserResponse.FromEntity(user);
    }
}