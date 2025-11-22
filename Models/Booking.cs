using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace TravelAgencyProject.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; } //who made the booking?

        public int TripId { get; set; }
        public Trip? Trip { get; set; } // which trip was booked?

        public DateTime BookingDate { get; set; } = DateTime.Now;

        public int PeopleCount { get; set; } 

        public decimal TotalPrice { get; set; }
    }
}
