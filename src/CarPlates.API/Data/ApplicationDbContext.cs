using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Vehicle> Vehicles { get; set; } = null!;
    public DbSet<ScanRecord> ScanRecords { get; set; } = null!;
    public DbSet<fw_Users> FwUsers { get; set; } = null!;

    public DbSet<RefreshTokens> RefreshTokens => Set<RefreshTokens>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<fw_Users>(entity =>
        {
            entity.ToTable("fw_Users");
            entity.HasKey(u => u.ID);
            entity.HasIndex(u => u.UserName);
            entity.Property(u => u.UserName).HasMaxLength(256);
            entity.Property(u => u.Password).HasMaxLength(256);
            entity.Property(u => u.MobilePassword).HasMaxLength(256);
            entity.Property(u => u.UserFullName_Ar).HasMaxLength(256);
            entity.Property(u => u.UserFullName_En).HasMaxLength(256);
            entity.Property(u => u.email).HasMaxLength(256);
        });

        builder.Entity<Vehicle>(entity =>
        {
            entity.ToView("VW_WH_VEHICLES");
            entity.HasKey(v => v.Id);
            entity.HasIndex(v => v.PlateNumber).IsUnique();
            entity.HasIndex(v => v.AccessStatus);
            entity.Property(v => v.PlateNumber).HasMaxLength(50);
            entity.Property(v => v.PlateType).HasMaxLength(20);
            entity.Property(v => v.Brand).HasMaxLength(100);
            entity.Property(v => v.Model).HasMaxLength(100);
            entity.Property(v => v.Color).HasMaxLength(50);
            entity.Property(v => v.OwnerName).HasMaxLength(200);
            entity.Property(v => v.AccessStatus).HasMaxLength(20);
            entity.HasQueryFilter(v => !v.IsDeleted);
        });

        builder.Entity<ScanRecord>(entity =>
        {
            entity.ToView("vw_CarsPlatesDashBoard");
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.PlateNumber);
            entity.HasIndex(s => s.ScanTime);
            entity.Property(s => s.PlateNumber).HasMaxLength(50);
            
        });

        builder.Entity<RefreshTokens>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Token)
                  .HasMaxLength(512);

            entity.HasIndex(x => x.Token)
                  .IsUnique();

            entity.HasIndex(x => x.UserId);

        });
    }
}
