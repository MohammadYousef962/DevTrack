using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DevTrack.Infrastructure.Data;

public static class DbSeeder
{
    private const int MinConfiguredPasswordLength = 12;

    public static Task SeedAsync(DevTrackDbContext db, IPasswordHasher passwordHasher) =>
        SeedAsync(db, passwordHasher, configuration: null, allowDefaults: true);

    public static async Task SeedAsync(
        DevTrackDbContext db,
        IPasswordHasher passwordHasher,
        IConfiguration? configuration,
        bool allowDefaults)
    {
        var adminExists = await db.Users.AnyAsync(u => u.Role == UserRole.Admin);
        if (adminExists) return;

        var configuredEmail = configuration?["AdminSeed:Email"];
        var configuredPassword = configuration?["AdminSeed:Password"];
        var configuredName = configuration?["AdminSeed:FullName"];

        string email;
        string password;

        if (allowDefaults)
        {
            email = string.IsNullOrWhiteSpace(configuredEmail) ? "admin@devtrack.local" : configuredEmail;
            password = string.IsNullOrWhiteSpace(configuredPassword) ? "Admin123!" : configuredPassword;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(configuredEmail) || string.IsNullOrWhiteSpace(configuredPassword))
                return;

            if (configuredPassword.Length < MinConfiguredPasswordLength)
                throw new InvalidOperationException(
                    $"AdminSeed:Password must be at least {MinConfiguredPasswordLength} characters.");

            email = configuredEmail;
            password = configuredPassword;
        }

        var admin = new User
        {
            FullName = string.IsNullOrWhiteSpace(configuredName) ? "System Admin" : configuredName,
            Email = email.Trim(),
            PasswordHash = passwordHasher.Hash(password),
            Role = UserRole.Admin,
            IsActive = true
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}