using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;

namespace TravelAgencyProject.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Please enter User Id")]
        public int UserId { get; set; }
        public User? User { get; set; } //who made the booking?

        [Required(ErrorMessage = "Please enter Trip Id")]
        public int TripId { get; set; }
        public Trip? Trip { get; set; } // which trip was booked?

        [Required]
        [Display(Name = "Booking Date")]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Please enter number of people")]
        [Range(1, 10 ErrorMessage = "People count must be at least 1")]
        public int PeopleCount { get; set; }

        [Required(ErrorMessage = "Please enter total price")]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Total price must be positive")]
        [DataType(DataType.Currency)]
        public decimal TotalPrice { get; set; }

        [Required]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public DateTime? CancellationDate { get; set; }
        public string? transactionId { get; set; }
        public tripStatus bookingStatus { get; set; } = tripStatus.Upcoming;

        public enum PaymentStatus
        {
            Pending,
            Completed,
            Failed,
            Refunded
        }
        public enum tripStatus
        {
            Upcoming,
            Completed,
            Canceled
        }
    }
}
