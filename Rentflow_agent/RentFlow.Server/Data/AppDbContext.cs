using Microsoft.EntityFrameworkCore;
using RentFlow.Shared.Models;

namespace RentFlow.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Property> Properties { get; set; } = null!;
        public DbSet<Unit> Units { get; set; } = null!;
        public DbSet<Lease> Leases { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<MaintenanceTicket> MaintenanceTickets { get; set; } = null!;
        public DbSet<WeatherAlertLog> WeatherAlertLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Property configuration
            modelBuilder.Entity<Property>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(p => p.Landlord)
                    .WithMany(u => u.Properties)
                    .HasForeignKey(p => p.LandlordId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Unit configuration
            modelBuilder.Entity<Unit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(u => u.Property)
                    .WithMany(p => p.Units)
                    .HasForeignKey(u => u.PropertyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Lease configuration
            modelBuilder.Entity<Lease>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(l => l.Unit)
                    .WithMany(u => u.Leases)
                    .HasForeignKey(l => l.UnitId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(l => l.Tenant)
                    .WithMany(u => u.Leases)
                    .HasForeignKey(l => l.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Payment configuration
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(p => p.Lease)
                    .WithMany(l => l.Payments)
                    .HasForeignKey(p => p.LeaseId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // No inverse navigation on User for Payments in our model, so we configure just the FK
                entity.HasOne(p => p.Tenant)
                    .WithMany()
                    .HasForeignKey(p => p.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // MaintenanceTicket configuration
            modelBuilder.Entity<MaintenanceTicket>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(t => t.Tenant)
                    .WithMany(u => u.MaintenanceTickets)
                    .HasForeignKey(t => t.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(t => t.Property)
                    .WithMany(p => p.MaintenanceTickets)
                    .HasForeignKey(t => t.PropertyId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(t => t.Unit)
                    .WithMany()
                    .HasForeignKey(t => t.UnitId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // WeatherAlertLog configuration
            modelBuilder.Entity<WeatherAlertLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(w => w.Property)
                    .WithMany(p => p.WeatherAlertLogs)
                    .HasForeignKey(w => w.PropertyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
