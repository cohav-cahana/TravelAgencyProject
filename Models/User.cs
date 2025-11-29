using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;


namespace TravelAgencyProject.Models
{
    [Index(nameof(Email), IsUnique = true)]
    public class User
    {
        [Key]
        public int UserId { get; set; } // Primary Key.

        [Required(ErrorMessage = "Please enter an email address")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(100, ErrorMessage = "The email can have a maximum of 100 characters.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } // it will be used as username(unique).

        [Required(ErrorMessage ="Please enter a password")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.")]
        public string Password { get; set; }

        [Required(ErrorMessage ="Please enter your first name")]
        [StringLength(50,MinimumLength =2 ,ErrorMessage = "First Name must be between 2 and 50 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage ="Please enter your last name")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last Name must be between 2 and 50 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        
        public bool IsAdmin { get; set; } = false; //managger = true, user/customer = false.
        public bool IsActive { get; set; } = true;
    }
}
 

