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

        public string Status { get; set; } = "Confirmed"; // Confirmed, Checked-In, Completed, Cancelled

        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }

        public DateTime? ActualCheckOut { get; set; }

        public DateTime? CancelledDate { get; set; }
        public string? CancellationReason { get; set; }

        // Spam protection properties
        public int CancellationAttempts { get; set; } = 0;
        public DateTime? LastCancellationAttempt { get; set; }

        [NotMapped]
        public bool CanBeCancelled =>
            Status == "Confirmed" &&
            (!CheckInDate.HasValue || (CheckInDate.Value - DateTime.Now).TotalHours > 1);

        [NotMapped]
        public bool CanAttemptCancellation =>
            CancellationAttempts == 0 ||
            (LastCancellationAttempt.HasValue &&
             DateTime.Now.Subtract(LastCancellationAttempt.Value).TotalSeconds > 30);

        [NotMapped]
        public string CancellationStatus
        {
            get
            {
                if (Status != "Confirmed")
                    return $"Cannot cancel: Status is {Status}";

                if (!CheckInDate.HasValue)
                    return "Can cancel";

                var timeUntilCheckIn = CheckInDate.Value - DateTime.Now;
                return timeUntilCheckIn.TotalHours > 1
                    ? "Can cancel"
                    : "Cannot cancel: Check-in is within 1 hour";
            }
        }

        [NotMapped]
        public int NumberOfNights =>
            (CheckInDate.HasValue && CheckOutDate.HasValue)
                ? (CheckOutDate.Value - CheckInDate.Value).Days
                : 1;
    }
}