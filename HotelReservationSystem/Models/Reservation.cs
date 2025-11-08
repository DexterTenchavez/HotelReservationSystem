// Models/Reservation.cs
using System;
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

       
        public string? RoomNumber { get; set; }

        [Required]
        [Range(1, 10)]
        public int NumberOfGuests { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

       
        public int? RoomId { get; set; }

        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Confirmed";

        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }

        [Display(Name = "Number of Nights")]
        public int NumberOfNights
        {
            get
            {
                if (CheckInDate.HasValue && CheckOutDate.HasValue)
                {
                    return (CheckOutDate.Value - CheckInDate.Value).Days;
                }
                return 1;
            }
        }
    }
}