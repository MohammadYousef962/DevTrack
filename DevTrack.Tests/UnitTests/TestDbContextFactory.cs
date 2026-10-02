using DevTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Tests.UnitTests;

public static class TestDbContextFactory
{
    public static DevTrackDbContext Create()
    {
        var options = new DbContextOptionsBuilder<DevTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DevTrackDbContext(options);
    }
}