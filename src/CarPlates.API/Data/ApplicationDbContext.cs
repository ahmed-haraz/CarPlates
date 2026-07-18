using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<ScanRecord> ScanRecords { get; set; } = null!;
    public DbSet<fw_Users> FwUsers { get; set; } = null!;

    public DbSet<RefreshTokens> RefreshTokens => Set<RefreshTokens>();

    // wh_ tables / view
    public DbSet<CustomerCar> CustomerCars { get; set; } = null!;
    public DbSet<WhCustomer> WhCustomers { get; set; } = null!;
    public DbSet<CustomerBranch> CustomerBranches { get; set; } = null!;
    public DbSet<CarMake> CarMakes { get; set; } = null!;
    public DbSet<CarModel> CarModels { get; set; } = null!;
    public DbSet<VehicleTypeLookup> VehicleTypes { get; set; } = null!;
    public DbSet<VehicleStatusLookup> VehicleStatuses { get; set; } = null!;
    public DbSet<EngineTypeLookup> EngineTypes { get; set; } = null!;
    public DbSet<CustomerCarFull> CustomerCarsFull { get; set; } = null!;
    public DbSet<ScanEvent> ScanEvents { get; set; } = null!;

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

        // ---- wh_ tables: these already exist in MobileDemo (see database/*.sql).
        // ExcludeFromMigrations so `dotnet ef migrations add` never tries to (re)create them. ----

        builder.Entity<CustomerCar>(entity =>
        {
            entity.ToTable("wh_CustomerCars", t => t.ExcludeFromMigrations());
            entity.HasKey(c => c.Id);
            entity.Property(c => c.PlateNumber).HasMaxLength(50);
            entity.Property(c => c.VIN).HasMaxLength(17);
            entity.Property(c => c.Color).HasMaxLength(50);
            entity.HasIndex(c => c.PlateNumber);
        });

        builder.Entity<WhCustomer>(entity =>
        {
            entity.ToTable("wh_Customers", t => t.ExcludeFromMigrations());
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Code).IsRequired();
            entity.Property(c => c.Name_Ar).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Name_En).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Phone1).HasMaxLength(50);
            entity.Property(c => c.Phone2).HasMaxLength(50);
            entity.Property(c => c.Mobile).HasMaxLength(50);
            entity.Property(c => c.email).HasMaxLength(200);
            entity.HasIndex(c => c.Mobile);
        });

        builder.Entity<CustomerBranch>(entity =>
        {
            entity.ToTable("wh_CustomersBranch", t => t.ExcludeFromMigrations());
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => new { c.ParentID, c.BranchID }).IsUnique();
        });

        builder.Entity<CarMake>(entity =>
        {
            entity.ToTable("wh_CarMakes", t => t.ExcludeFromMigrations());
            entity.HasKey(m => m.MakeID);
            entity.Property(m => m.MakeName).HasMaxLength(255).IsRequired();
        });

        builder.Entity<CarModel>(entity =>
        {
            entity.ToTable("wh_CarModels", t => t.ExcludeFromMigrations());
            entity.HasKey(m => m.ModelID);
            entity.Property(m => m.ModelName).HasMaxLength(255).IsRequired();
            entity.HasIndex(m => m.MakeID);
        });

        builder.Entity<VehicleTypeLookup>(entity =>
        {
            entity.ToTable("wh_VehicleType", t => t.ExcludeFromMigrations());
            entity.HasKey(v => v.Id);
        });

        builder.Entity<VehicleStatusLookup>(entity =>
        {
            entity.ToTable("wh_VehicleStatus", t => t.ExcludeFromMigrations());
            entity.HasKey(v => v.Id);
        });

        builder.Entity<EngineTypeLookup>(entity =>
        {
            entity.ToTable("wh_CarsEngineType", t => t.ExcludeFromMigrations());
            entity.HasKey(v => v.Id);
        });

        builder.Entity<CustomerCarFull>(entity =>
        {
            entity.ToView("VW_WH_CustomerCarsFull");
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.PlateNumber);
        });

        builder.Entity<ScanEvent>(entity =>
        {
            entity.ToTable("wh_ScanRecords", t => t.ExcludeFromMigrations());
            entity.HasKey(s => s.Id);
            entity.Property(s => s.PlateNumber).HasMaxLength(50).IsRequired();
            entity.Property(s => s.DeviceId).HasMaxLength(200);
            entity.HasIndex(s => s.PlateNumber);
            entity.HasIndex(s => s.ScanTime);
        });
    }
}
