namespace HotelReservationSystem.Models
{
    public class CashReceiptsViewModel
    {
      
            public List<Reservation> PendingPayments { get; set; } = new List<Reservation>();
            public List<Reservation> TodayReceipts { get; set; } = new List<Reservation>();
            public decimal TotalPendingAmount { get; set; }
            public decimal TotalTodayCash { get; set; }
        
    }
}
