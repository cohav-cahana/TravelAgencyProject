using System.ComponentModel.DataAnnotations;


namespace TravelAgencyProject.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [Required(ErrorMessage ="Please enter user ID")]
        public int UserId { get; set; }
        public User? User { get; set; } // Who wrote the review?

        public int? TripId { get; set; } // Can be null if the review is general.
        public Trip? Trip { get; set; }

        [Required]
        public DateTime PostedDate { get; set; }=DateTime.Now;

        [Required(ErrorMessage = "Please enter a rating")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars."))]
        public int Rating { get; set; } //Score between 1-5 stars.

        [Required(ErrorMessage = "Please enter your review comment")]
        [StringLength(500,ErrorMessage = "can be maximum 500 characters.")]
        public string Comment { get; set; } //The review text.

        public DateTime PostedDate { get; set; } = DateTime.Now;
    }
}
