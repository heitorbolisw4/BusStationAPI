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
        public DbSet<Price> Prices { get; set; }
        public DbSet<Boarding> Boardings { get; set; }

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
               entity.HasOne(t => t.Boarding).WithOne(b => b.Ticket).HasForeignKey<Boarding>(b => b.TicketId);
            });
            modelBuilder.Entity<Distance>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.HasOne(d => d.Origin).WithMany(d => d.Distances).HasForeignKey(d => d.OriginId);
                entity.HasOne(d => d.Destination).WithMany(d => d.Distances).HasForeignKey(d => d.DestinationId);
                entity.HasData(
                    new Distance {Id = 1, OriginId = 1 ,DestinationId = 2, Kilometers = 60},
                    new Distance {Id = 2, OriginId = 2 ,DestinationId = 1, Kilometers = 60},
                    new Distance {Id = 3, OriginId = 2 ,DestinationId = 3, Kilometers = 45},
                    new Distance {Id = 4, OriginId = 3 ,DestinationId = 2, Kilometers = 45},
                    new Distance {Id = 5, OriginId = 2 ,DestinationId = 4, Kilometers = 100},
                    new Distance {Id = 6, OriginId = 4 ,DestinationId = 2, Kilometers = 100}
                );
            });
            modelBuilder.Entity<Origin>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.HasOne(o => o.City).WithMany(c => c.Origins).HasForeignKey(o => o.CityId);
                entity.HasData(
                    new Origin {Id = 1,CityId = 1, CityAcronym = "Indi"},
                    new Origin {Id = 2,CityId = 2, CityAcronym = "Udia"},
                    new Origin {Id = 3,CityId = 3, CityAcronym = "Reri"},
                    new Origin {Id = 4,CityId = 4, CityAcronym = "Ura"}
                );
            });
            modelBuilder.Entity<Destination>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.HasOne(d => d.City).WithMany(c => c.Destinations).HasForeignKey(d => d.CityId);
                entity.HasData(
                    new Destination {Id = 1,CityId = 1, CityAcronym = "Indi"},
                    new Destination {Id = 2,CityId = 2, CityAcronym = "Udia"},
                    new Destination {Id = 3,CityId = 3, CityAcronym = "Reri"},
                    new Destination {Id = 4,CityId = 4, CityAcronym = "Ura"}
                );
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
                entity.HasData(
                    new City{Id = 1, CityName = "Indianopolis", Acronym = "Indi",State = "MG"},
                    new City{Id = 2, CityName = "Uberlandia", Acronym = "Udia",State = "MG"},
                    new City{Id = 3, CityName = "Araguari", Acronym = "Reri",State = "MG"},
                    new City{Id = 4, CityName = "Uberaba", Acronym = "Ura",State = "MG"}

                );


            });
        
            modelBuilder.Entity<Price>(entity =>
            {
               entity.HasKey(p => p.Id);
               entity.HasOne(p => p.Distance).WithMany(d => d.Prices).HasForeignKey(p => p.DistanceId);
               entity.HasData(
                    new Price {Id = 1, DistanceId = 1, PricePerKm = 0.50f},
                    new Price {Id = 2, DistanceId = 2, PricePerKm = 0.50f},
                    new Price {Id = 3, DistanceId = 3, PricePerKm = 0.35f},
                    new Price {Id = 4, DistanceId = 4, PricePerKm = 0.35f},
                    new Price {Id = 5, DistanceId = 5, PricePerKm = 0.90f},
                    new Price {Id = 6, DistanceId = 6, PricePerKm = 0.90f}
                    
               );
            });
            modelBuilder.Entity<Boarding>(entity =>
            {
               entity.HasKey(x => x.Id);
               entity.HasMany(x => x.Routes).WithOne(x => x.Boarding).HasForeignKey(x=> x.BoardingId);

            });

        
        }
    }
}