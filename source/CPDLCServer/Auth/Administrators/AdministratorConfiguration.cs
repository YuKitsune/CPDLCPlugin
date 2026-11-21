using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CPDLCServer.Auth.Administrators;

public class AdministratorConfiguration : IEntityTypeConfiguration<Administrator>
{
    public void Configure(EntityTypeBuilder<Administrator> builder)
    {
        builder.ToTable("Administrators");

        builder.HasKey(a => a.Cid);
        builder.Property(a => a.Cid).ValueGeneratedNever();
    }
}
