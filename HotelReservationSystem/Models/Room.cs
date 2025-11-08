
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

      
        public string? Features { get; set; }

 
        public virtual ICollection<Reservation>? Reservations { get; set; }
    }
}