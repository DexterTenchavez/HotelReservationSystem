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
        [Column(TypeName = "decimal(18,2)")]  // ✅ Add this line
        public decimal TotalAmount { get; set; }
    }
}