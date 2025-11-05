using System.Collections.Generic;
using System.Linq; // ✅ ADD THIS

namespace HotelReservationSystem.Models
{
    public class UserDashboardViewModel
    {
        public ApplicationUser User { get; set; }
        public List<Reservation> Reservations { get; set; }
        public int TotalReservations => Reservations?.Count ?? 0;
        public decimal TotalSpent => Reservations?.Sum(r => r.TotalAmount) ?? 0;
    }
}