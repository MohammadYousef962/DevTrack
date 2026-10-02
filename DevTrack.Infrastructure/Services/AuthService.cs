using DevTrack.Application.DTOs.Auth;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using DevTrack.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;
namespace DevTrack.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly DevTrackDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(DevTrackDbContext db, IPasswordHasher passwordHasher, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            throw new InvalidOperationException("Password and confirmation do not match.");

        var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            throw new ConflictException("A user with this email already exists.");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Developer
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("New user registered: {Email} (Id: {UserId})", user.Email, user.Id);

        var token = _tokenService.GenerateAccessToken(user);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            _logger.LogWarning("Failed login attempt for {Email}: invalid credentials", request.Email);
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Failed login attempt for {Email}: account deactivated", request.Email);
            throw new InvalidCredentialsException("This account has been deactivated.");
        }

        var token = _tokenService.GenerateAccessToken(user);
        _logger.LogInformation("User {Email} (Id: {UserId}) logged in successfully", user.Email, user.Id);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }
}