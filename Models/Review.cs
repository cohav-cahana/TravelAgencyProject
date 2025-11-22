using System.ComponentModel.DataAnnotations;


namespace TravelAgencyProject.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; } // Who wrote the review?

        public int? TripId { get; set; } // Can be null if the review is general.
        public Trip? Trip { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } //Score between 1-5 stars.

        [StringLength(500)]
        public string Comment { get; set; } //The review text.

        public DateTime PostedDate { get; set; } = DateTime.Now;
    }
}
