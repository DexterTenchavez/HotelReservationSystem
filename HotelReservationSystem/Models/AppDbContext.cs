using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Models
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Room> Rooms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
            modelBuilder.Entity<Room>().HasData(
                // Single Rooms (6 rooms)
                new Room { RoomId = 1, RoomNumber = "101", RoomType = "Single", PricePerNight = 1000, MaxGuests = 1, IsAvailable = true, Description = "Cozy single room with basic amenities" },
                new Room { RoomId = 2, RoomNumber = "102", RoomType = "Single", PricePerNight = 1000, MaxGuests = 1, IsAvailable = true, Description = "Cozy single room with city view" },
                new Room { RoomId = 3, RoomNumber = "103", RoomType = "Single", PricePerNight = 1000, MaxGuests = 1, IsAvailable = true, Description = "Cozy single room with garden view" },
                new Room { RoomId = 4, RoomNumber = "104", RoomType = "Single", PricePerNight = 1000, MaxGuests = 1, IsAvailable = true, Description = "Single room with work desk" },
                new Room { RoomId = 5, RoomNumber = "105", RoomType = "Single", PricePerNight = 1000, MaxGuests = 1, IsAvailable = true, Description = "Single room with mountain view" },
                new Room { RoomId = 6, RoomNumber = "106", RoomType = "Single", PricePerNight = 1000, MaxGuests = 1, IsAvailable = true, Description = "Single room with premium amenities" },

                // Double Rooms (6 rooms)
                new Room { RoomId = 7, RoomNumber = "201", RoomType = "Double", PricePerNight = 2000, MaxGuests = 2, IsAvailable = true, Description = "Spacious double room with queen bed" },
                new Room { RoomId = 8, RoomNumber = "202", RoomType = "Double", PricePerNight = 2000, MaxGuests = 2, IsAvailable = true, Description = "Spacious double room with twin beds" },
                new Room { RoomId = 9, RoomNumber = "203", RoomType = "Double", PricePerNight = 2000, MaxGuests = 2, IsAvailable = true, Description = "Spacious double room with balcony" },
                new Room { RoomId = 10, RoomNumber = "204", RoomType = "Double", PricePerNight = 2000, MaxGuests = 2, IsAvailable = true, Description = "Double room with city skyline view" },
                new Room { RoomId = 11, RoomNumber = "205", RoomType = "Double", PricePerNight = 2000, MaxGuests = 2, IsAvailable = true, Description = "Double room with extra seating area" },
                new Room { RoomId = 12, RoomNumber = "206", RoomType = "Double", PricePerNight = 2000, MaxGuests = 2, IsAvailable = true, Description = "Double room with premium bedding" },

                // Suite Rooms (6 rooms)
                new Room { RoomId = 13, RoomNumber = "301", RoomType = "Suite", PricePerNight = 3500, MaxGuests = 4, IsAvailable = true, Description = "Luxury suite with living area" },
                new Room { RoomId = 14, RoomNumber = "302", RoomType = "Suite", PricePerNight = 3500, MaxGuests = 4, IsAvailable = true, Description = "Luxury suite with jacuzzi" },
                new Room { RoomId = 15, RoomNumber = "303", RoomType = "Suite", PricePerNight = 3500, MaxGuests = 4, IsAvailable = true, Description = "Presidential suite with ocean view" },
                new Room { RoomId = 16, RoomNumber = "304", RoomType = "Suite", PricePerNight = 3500, MaxGuests = 4, IsAvailable = true, Description = "Executive suite with kitchenette" },
                new Room { RoomId = 17, RoomNumber = "305", RoomType = "Suite", PricePerNight = 3500, MaxGuests = 4, IsAvailable = true, Description = "Family suite with separate bedrooms" },
                new Room { RoomId = 18, RoomNumber = "306", RoomType = "Suite", PricePerNight = 3500, MaxGuests = 4, IsAvailable = true, Description = "Honeymoon suite with private balcony" }
            );
        }
    }

    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = null!;
        public virtual ICollection<Reservation>? Reservations { get; set; }
    }
}