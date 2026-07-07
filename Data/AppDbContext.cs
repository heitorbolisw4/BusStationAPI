using BusStation_API.Entities;
using Microsoft.EntityFrameworkCore;
using Route = BusStation_API.Entities.Route;

namespace BusStation_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected AppDbContext()
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Distance> Distances { get; set; }
        public DbSet<Origin> Origins { get; set; }
        public DbSet<Route> Routes { get; set; }
        public DbSet<Destination> Destinations { get; set; }
        public DbSet<City> City {get;set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Email);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.Name).IsRequired().HasMaxLength(150);
                entity.Property(u => u.Password).IsRequired().HasMaxLength(255);

                entity.HasMany(u => u.Tickets).WithOne(t => t.User).HasForeignKey(t => t.UserId);
            });
            modelBuilder.Entity<Ticket>(entity =>
            {
               entity.HasKey(t => t.Id);
               entity.HasOne(t => t.Route).WithMany(t => t.Tickets).HasForeignKey(t => t.RouteId);
            });
            modelBuilder.Entity<Distance>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.HasOne(d => d.Origin).WithMany(d => d.Distances).HasForeignKey(d => d.OriginId);
                entity.HasOne(d => d.Destination).WithMany(d => d.Distances).HasForeignKey(d => d.DestinationId);
            });
            modelBuilder.Entity<Origin>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.HasOne(o => o.City).WithMany(c => c.Origins).HasForeignKey(o => o.CityId);
            });
            modelBuilder.Entity<Destination>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.HasOne(d => d.City).WithMany(c => c.Destinations).HasForeignKey(d => d.CityId);
            });
            
            modelBuilder.Entity<Route>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.RouteName).HasMaxLength(20);
                entity.HasOne(r => r.Distance).WithMany(d => d.Routes).HasForeignKey(r => r.DistanceId);
            });
            modelBuilder.Entity<City>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.CityName).HasMaxLength(150).IsRequired();
                entity.Property(c => c.State).HasMaxLength(50);
                entity.Property(c => c.Acronym).HasMaxLength(5);
                entity.HasIndex(c => c.Acronym).IsUnique();


            });
        }
    }
}