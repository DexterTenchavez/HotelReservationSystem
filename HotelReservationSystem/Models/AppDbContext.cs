using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace HotelReservationSystem.Models
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

       
        public DbSet<Reservation> Reservations { get; set; }
    }

    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = null!;
        public virtual ICollection<Reservation>? Reservations { get; set; }
    }
}
