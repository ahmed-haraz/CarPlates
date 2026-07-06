using CarPlates.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles { get; set; } = null!;
    public DbSet<ScanRecord> ScanRecords { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Vehicle>(entity =>
        {
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
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.PlateNumber);
            entity.HasIndex(s => s.ScanTime);
            entity.Property(s => s.PlateNumber).HasMaxLength(50);
            entity.HasOne(s => s.Vehicle)
                  .WithMany(v => v.ScanRecords)
                  .HasForeignKey(s => s.VehicleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
