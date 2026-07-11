using Microsoft.EntityFrameworkCore;
using BusinessOps.Backend.Models;

namespace BusinessOps.Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<ModulePermission> ModulePermissions => Set<ModulePermission>();

    public DbSet<FieldPermission> FieldPermissions => Set<FieldPermission>();

    public DbSet<EntryVisibilityPermission> EntryVisibilityPermissions => Set<EntryVisibilityPermission>();

    public DbSet<Module> Modules => Set<Module>();

    public DbSet<Entry> Entries => Set<Entry>();

    public DbSet<Visualization> Visualizations => Set<Visualization>();

    public DbSet<DashboardWidget> DashboardWidgets => Set<DashboardWidget>();
    public DbSet<AnalyticsWidget> AnalyticsWidgets => Set<AnalyticsWidget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();

            entity.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Module>(entity =>
        {
            entity.ToTable("modules");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Fields).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<ModulePermission>(entity =>
        {
            entity.ToTable("module_permissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CanView).IsRequired();
            entity.Property(x => x.CanCreate).IsRequired();
            entity.Property(x => x.CanEdit).IsRequired();
            entity.Property(x => x.CanDelete).IsRequired();
            entity.Property(x => x.CanManagePermissions).IsRequired();
            entity.HasIndex(x => new { x.RoleId, x.ModuleId }).IsUnique();

            entity.HasOne(x => x.Role)
                .WithMany(x => x.ModulePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Module)
                .WithMany(x => x.ModulePermissions)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FieldPermission>(entity =>
        {
            entity.ToTable("field_permissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FieldName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.CanView).IsRequired();
            entity.Property(x => x.CanEdit).IsRequired();
            entity.HasIndex(x => new { x.RoleId, x.ModuleId, x.FieldName }).IsUnique();

            entity.HasOne(x => x.Role)
                .WithMany(x => x.FieldPermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Module)
                .WithMany(x => x.FieldPermissions)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Entry>(entity =>
        {
            entity.ToTable("entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Timestamp).IsRequired();
            entity.Property(x => x.Data).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasOne(x => x.Module)
                .WithMany(x => x.Entries)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.CreatedByUser)
                .WithMany(x => x.CreatedEntries)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ModuleId, x.Timestamp });
            entity.HasIndex(x => x.CreatedByUserId);
        });

        modelBuilder.Entity<EntryVisibilityPermission>(entity =>
        {
            entity.ToTable("entry_visibility_permissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CanView).IsRequired();
            entity.Property(x => x.CanEdit).IsRequired();
            entity.HasIndex(x => new { x.EntryId, x.RoleId }).IsUnique();

            entity.HasOne(x => x.Entry)
                .WithMany(x => x.VisibilityPermissions)
                .HasForeignKey(x => x.EntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.OwnerUser)
                .WithMany(x => x.EntryVisibilityPermissions)
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Role)
                .WithMany(x => x.EntryVisibilityPermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Visualization>(entity =>
        {
            entity.ToTable("visualizations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.XField).HasMaxLength(120);
            entity.Property(x => x.XAggregation).HasMaxLength(40);
            entity.Property(x => x.YField).HasMaxLength(120);
            entity.Property(x => x.FieldName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.SecondaryFieldName).HasMaxLength(120);
            entity.Property(x => x.ChartType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.WidgetSize).HasMaxLength(20).IsRequired();
            entity.Property(x => x.AggregationType).HasMaxLength(40);
            entity.Property(x => x.DateRange).HasMaxLength(40);
            entity.Property(x => x.DateRangeType).HasMaxLength(40);
            entity.Property(x => x.CustomStartTimestamp);
            entity.Property(x => x.CustomEndTimestamp);
            entity.Property(x => x.SummaryMetric).HasMaxLength(40);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.GeneralOptions).HasColumnType("jsonb");
            entity.Property(x => x.ChartSpecificOptions).HasColumnType("jsonb");
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasOne(x => x.Module)
                .WithMany(x => x.Visualizations)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.ModuleId, x.CreatedAt });
        });

        modelBuilder.Entity<DashboardWidget>(entity =>
        {
            entity.ToTable("dashboard_widgets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Size).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Width).IsRequired();
            entity.Property(x => x.Height).IsRequired();
            entity.Property(x => x.X);
            entity.Property(x => x.Y);
            entity.Property(x => x.Position).IsRequired();
            entity.Property(x => x.VisualSettings).HasColumnType("jsonb");
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasOne(x => x.Module)
                .WithMany(x => x.DashboardWidgets)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Visualization)
                .WithMany(x => x.DashboardWidgets)
                .HasForeignKey(x => x.VisualizationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.Position);
            entity.HasIndex(x => new { x.Y, x.X });
            entity.HasIndex(x => x.VisualizationId);
        });

        modelBuilder.Entity<AnalyticsWidget>(entity =>
        {
            entity.ToTable("analytics_widgets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.FieldAName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.FieldBName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ChartType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.DateRange).HasMaxLength(40);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasIndex(x => x.CreatedAt);
        });
    }
}
