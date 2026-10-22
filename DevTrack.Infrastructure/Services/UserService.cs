using DevTrack.Application.DTOs.Users;
using DevTrack.Application.Interfaces;
using DevTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using DevTrack.Application.Common;
using DevTrack.Domain.Entities;

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

        // Only an admin who is currently active counts toward "the last active admin".
        if (!isActive && user.IsActive && user.Role == Domain.Enums.UserRole.Admin)
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
        // Enum.TryParse also accepts numbers like "99", so check the value is a real member too.
        if (!Enum.TryParse<Domain.Enums.UserRole>(newRole, ignoreCase: true, out var parsedRole) || !Enum.IsDefined(parsedRole))
            throw new InvalidOperationException($"'{newRole}' is not a valid role.");

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        if (user.IsActive && user.Role == Domain.Enums.UserRole.Admin && parsedRole != Domain.Enums.UserRole.Admin)
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
    public async Task<UserLookupResponse> LookupByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required.");

        var normalized = email.Trim();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalized && u.IsActive);

        if (user is null)
            throw new KeyNotFoundException("No active user found with that email.");

        return new UserLookupResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email
        };
    }
    public async Task<UserResponse> SetAvatarAsync(int userId, byte[] data)
    {
        if (data.Length == 0)
            throw new InvalidOperationException("No picture was uploaded.");

        if (data.Length > AvatarRules.MaxBytes)
            throw new InvalidOperationException($"The picture is too large. The limit is {AvatarRules.MaxBytes / 1024} KB.");

        var contentType = AvatarRules.DetectContentType(data);
        if (contentType is null)
            throw new InvalidOperationException("The picture must be a JPEG, PNG or WebP image.");

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            throw new KeyNotFoundException("User not found.");

        var avatar = await _db.UserAvatars.FindAsync(userId);
        if (avatar is null)
        {
            avatar = new UserAvatar { UserId = userId };
            _db.UserAvatars.Add(avatar);
        }

        avatar.Data = data;
        avatar.ContentType = contentType;
        avatar.UpdatedAt = DateTime.UtcNow;

        // The version in the address changes with every upload, so a browser never shows a stale picture.
        user.ProfileImageUrl = $"/api/users/{userId}/avatar?v={avatar.UpdatedAt.Ticks}";

        await _db.SaveChangesAsync();

        return UserResponse.FromEntity(user);
    }

    public async Task<UserResponse> RemoveAvatarAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            throw new KeyNotFoundException("User not found.");

        var avatar = await _db.UserAvatars.FindAsync(userId);
        if (avatar is not null)
            _db.UserAvatars.Remove(avatar);

        user.ProfileImageUrl = null;
        await _db.SaveChangesAsync();

        return UserResponse.FromEntity(user);
    }

    public async Task<AvatarFile?> GetAvatarAsync(int userId)
    {
        return await _db.UserAvatars
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new AvatarFile(a.Data, a.ContentType))
            .FirstOrDefaultAsync();
    }
}