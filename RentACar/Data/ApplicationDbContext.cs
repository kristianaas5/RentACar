using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentACar.Models;
using Microsoft.AspNetCore.Identity;
using System.Data;

namespace RentACar.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Reservation -> User
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Reservation -> Car
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Car)
                .WithMany(c => c.Reservations)
                .HasForeignKey(r => r.CarId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Reservations)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Car>()
                .HasMany(c => c.Reservations)
                .WithOne(r => r.Car)
                .HasForeignKey(r => r.CarId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.UserId)
                      .IsRequired();
                entity.Property(r => r.CarId)
                      .IsRequired();
                entity.Property(r => r.StartDate)
                      .IsRequired();
                entity.Property(r => r.EndDate)
                      .IsRequired();
                entity.Property(r => r.IsReserved);
                entity.HasQueryFilter(e => !e.IsDeleted);

            });
            modelBuilder.Entity<Car>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Brand)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(c => c.Model)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(c => c.Year)
                      .IsRequired();
                entity.Property(c => c.DailyPrice)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");
                entity.HasQueryFilter(e => !e.IsDeleted);
            });
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.FirstName)
                      .IsRequired();
                entity.Property(u => u.LastName)
                        .IsRequired();
                entity.Property(u => u.EGN)
                        .IsRequired()
                        .HasMaxLength(10);
                entity.HasQueryFilter(e => !e.IsDeleted);
            });
        }
    }
}
