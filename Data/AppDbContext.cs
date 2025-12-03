
using Microsoft.EntityFrameworkCore;
using Starter.Api.Models;

namespace Starter.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<LogEntry> Logs => Set<LogEntry>();
    public DbSet<SysDates> SysDates => Set<SysDates>();

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RevokedAccessToken> RevokedAccessTokens => Set<RevokedAccessToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>(); // opcional

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.Property(i => i.Name).IsRequired().HasMaxLength(120);
            //entity.Property(i => i.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
        });

        modelBuilder.Entity<LogEntry>(e =>
        {
            e.Property(x => x.Description).IsRequired().HasMaxLength(250);
            e.Property(x => x.ExecDate).HasColumnType("datetime");

            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.RoleId);
            e.HasIndex(x => x.PermissionId);
            e.HasIndex(x => x.ExecDate);
        });

        // UserRole
        modelBuilder.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });
        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId);
        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId);

        // RolePermission
        modelBuilder.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });
        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId);
        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId);

        modelBuilder.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Permission>().HasIndex(x => x.Name).IsUnique();

        base.OnModelCreating(modelBuilder);

        // índices
        modelBuilder.Entity<RevokedAccessToken>()
            .HasIndex(x => x.Jti)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => new { x.UserId, x.TokenHash })
            .IsUnique();
    }

    
}
