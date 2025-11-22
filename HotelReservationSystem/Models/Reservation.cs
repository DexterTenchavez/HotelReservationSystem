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

        public DateTime? ActualCheckOut { get; set; }

        public DateTime? CancelledDate { get; set; }

        public string? CancellationReason { get; set; }

        // Payment Information
        public string? PaymentMethod { get; set; }

        public string? PaymentStatus { get; set; } = "Pending";

        public string? TransactionNumber { get; set; }

        // ✅ CASHIER SYSTEM: Official Receipt and Cashier Tracking
        public string? ReceiptNumber { get; set; }
        public string? CashierName { get; set; }        // Who received the cash
        public DateTime? CashReceivedDate { get; set; } // When cash was physically received

        public DateTime? PaymentDate { get; set; }

        // Rating and Feedback
        public int? Rating { get; set; }

        public string? Feedback { get; set; }

        public DateTime? RatingDate { get; set; }

        public int NumberOfNights { get; set; } = 1;

        // Spam protection properties
        public int CancellationAttempts { get; set; } = 0;

        public DateTime? LastCancellationAttempt { get; set; }

        // Soft Delete Properties
        public bool IsDeletedByUser { get; set; } = false;

        public DateTime? DeletedByUserDate { get; set; }

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
        public bool CanRate => Status == "Completed" && !Rating.HasValue;

        [NotMapped]
        public bool HasFeedback => Rating.HasValue && !string.IsNullOrEmpty(Feedback);
    }
}