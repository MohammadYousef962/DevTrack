using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(DevTrackDbContext db, IPasswordHasher passwordHasher)
    {
        var adminExists = await db.Users.AnyAsync(u => u.Role == UserRole.Admin);
        if (adminExists) return;

        var admin = new User
        {
            FullName = "System Admin",
            Email = "admin@devtrack.local",
            PasswordHash = passwordHasher.Hash("Admin123!"),
            Role = UserRole.Admin
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}