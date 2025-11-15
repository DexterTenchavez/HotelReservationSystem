using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelReservationSystem.Models
{
    public class Room
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RoomId { get; set; }

        [Required]
        public string RoomNumber { get; set; } = string.Empty;

        [Required]
        public string RoomType { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerNight { get; set; }

        public string? Description { get; set; }

        public int MaxGuests { get; set; } = 2;

        public bool IsAvailable { get; set; } = true;

        public bool UnderMaintenance { get; set; } = false;

        public string? Features { get; set; }

        // Rating properties
        [Column(TypeName = "decimal(3,2)")]
        public decimal AverageRating { get; set; } = 4.2m;

        public int TotalRatings { get; set; } = 0;

        public virtual ICollection<Reservation>? Reservations { get; set; }

        [NotMapped]
        public string RatingDisplay => AverageRating.ToString("0.0");

        [NotMapped]
        public int FullStars => (int)AverageRating;

        [NotMapped]
        public bool HasHalfStar => AverageRating - FullStars >= 0.5m;

        [NotMapped]
        public int EmptyStars => 5 - FullStars - (HasHalfStar ? 1 : 0);
    }
}