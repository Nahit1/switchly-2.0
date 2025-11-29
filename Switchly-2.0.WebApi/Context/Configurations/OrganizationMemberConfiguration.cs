using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class OrganizationMemberConfiguration:IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Organization)
            .WithMany(o => o.Members)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany(u => u.OrganizationMembers)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Aynı user aynı org'da 1 kere üye olsun
        builder.HasIndex(x => new { x.OrganizationId, x.UserId })
            .IsUnique();

        // Role enum'unu string saklayalım (log/debug daha okunur)
        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(32);
    }
}