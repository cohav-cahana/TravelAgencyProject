using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;


namespace TravelAgencyProject.Models
{
    public class Trip : IValidatableObject
    {
        [Key]
        public int TripId { get; set; }

        [Required(ErrorMessage = "The name of destination is required")]
        [StringLength(20, ErrorMessage = "The name of the destination can have only 20 letters")]
        public string Destination { get; set; }

        [Required(ErrorMessage = "The name of country is required")]
        [StringLength(20, ErrorMessage = "The name of the country can have only 20 letters")]
        public string Country { get; set; }

        [Required(ErrorMessage = "The description is required")]
        [StringLength(2000, MinimumLength = 20, ErrorMessage = "The name of the description can be between 20-2000 letters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "The date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "The date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "End date")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "The price is required")]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Has to be positive")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; } // the regular price.

        [DataType(DataType.Currency)]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Has to be positive")]
        public decimal? SalePrice { get; set; } // Sale price, if we have a discount it shows - otherwise null.

        public DateTime? DiscountEndDate { get; set; }

        [Required(ErrorMessage = "The amount of available rooms is required")]
        [Range(1, 1000, ErrorMessage = "Has to be positive")]
        public int Stock { get; set; } // amaount of available rooms.

        [DataType(DataType.ImageUrl)]
        public string? ImageUrl { get; set; } // URL if we have an image.

        [NotMapped] // we don't want to store it in the database.
        [Display(Name = "Upload Image")] // what will be shown in the form.
        public IFormFile ImageFile { get; set; } // the image file that the user uploads.

        [Required(ErrorMessage = "The category is required")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Age limit is required")]
        [Range(0, 120, ErrorMessage = "The age a positive number")]
        public int? AgeLimitaion { get; set; }
        public bool IsVisible { get; set; } = true;

        [Display(Name = "Cancellation Deadline (Hours)")]
        public int CancellationDeadlineHours { get; set; } = 24;

        // Navigation property for reviews associated with this trip
        public List<Review> Reviews { get; set; } = new List<Review>();
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate <= StartDate)
            {
                yield return new ValidationResult(
                    "End date must be after start date",
                    new[] { nameof(EndDate) });
            }
            if (StartDate.Date < DateTime.Today.Date)
            {
                yield return new ValidationResult(
                    "Start date must be today or in the future",
                    new[] { nameof(StartDate) });
            }
            if (SalePrice.HasValue)
            {
                if (SalePrice.Value > Price)
                {
                    yield return new ValidationResult(
                        "Sale price must be less than regular price",
                        new[] { nameof(SalePrice) });
                }
                if (!DiscountEndDate.HasValue)
                {
                    yield return new ValidationResult(
                        "Discount end date is required when sale price is set",
                        new[] { nameof(DiscountEndDate) });
                }
                else if (DiscountEndDate.Value.Date > DateTime.Today.Date.AddDays(7))
                {
                    yield return new ValidationResult(
                       "The sale can onlu be for a week",
                       new[] { nameof(DiscountEndDate) });
                }
            }


            else if (DiscountEndDate.HasValue)
            {
                yield return new ValidationResult(
                    "Cannot set a discount end date without a sale price",
                    new[] { nameof(DiscountEndDate) });
            }

        }

    }


}


