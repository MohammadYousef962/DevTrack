using DevTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevTrack.Infrastructure.Data.Configurations;

public class UserAvatarConfiguration : IEntityTypeConfiguration<UserAvatar>
{
    public void Configure(EntityTypeBuilder<UserAvatar> builder)
    {
        builder.HasKey(a => a.UserId);
        builder.Property(a => a.Data).IsRequired();
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(50);

        // One picture per user, removed together with the user.
        builder.HasOne<User>().WithOne().HasForeignKey<UserAvatar>(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}