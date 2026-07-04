using BusStation_API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusStation_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        protected AppDbContext()
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Distance> Distances { get; set; }
        public DbSet<Origin> Origins { get; set; }
        public DbSet<Destination> Destinations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Email);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.Name).IsRequired().HasMaxLength(150);
                entity.Property(u => u.Password).IsRequired().HasMaxLength(255);

                entity.HasMany(u => u.Tickets).WithOne(t => t.User).HasForeignKey();
            });
        }
    }
}