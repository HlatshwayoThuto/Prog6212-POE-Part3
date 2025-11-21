using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace WebApplication1.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts)
            : base(opts) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<Document> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Claim → Documents
            modelBuilder.Entity<Claim>()
                .HasMany(c => c.Documents)
                .WithOne()
                .HasForeignKey(d => d.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            // CORRECT DECIMAL MAPPING
            modelBuilder.Entity<Claim>()
                .Property(c => c.HoursWorked)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Claim>()
                .Property(c => c.HourlyRate)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<User>()
                .Property(u => u.HourlyRate)
                .HasColumnType("decimal(10,2)");

            base.OnModelCreating(modelBuilder);
        }
    }
}