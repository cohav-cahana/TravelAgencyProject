using System.ComponentModel.DataAnnotations;


namespace TravelAgencyProject.Models
{
    public class Trip
    {
        [Key]
        public int TripId { get; set; }

        [Required]
        public string Destination { get; set; } 

        [Required]
        public string Country { get; set; } 

        [Required]
        public string Description { get; set; } 

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [Range(0, 100000)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; } // the regular price.

        [DataType(DataType.Currency)]
        public decimal? SalePrice { get; set; } // Sale price, if we have a discount it shows - otherwise null.

        [Required]
        [Range(0, 1000)]
        public int Stock { get; set; } // amaount of available rooms.

        public string? ImageUrl { get; set; } // URL if we have an image.

        [Required]
        public string Category { get; set; } 
    }
}
