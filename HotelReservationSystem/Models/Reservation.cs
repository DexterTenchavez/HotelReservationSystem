using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelReservationSystem.Models
{
    public class Reservation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReservationId { get; set; }

        public string? ReservationNo { get; set; }

        [Required]
        public string? GuestName { get; set; }

        [Required]
        public string? RoomType { get; set; }

        [Required]
        public int NumberOfGuests { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // ✅ ADD THESE: Link reservation to user
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // ✅ ADD: Reservation status
        public string Status { get; set; } = "Confirmed";

        // ✅ ADD: Check-in and Check-out dates
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
    }
}