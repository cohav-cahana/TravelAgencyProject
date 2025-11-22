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
        [EmailAddress]
        public string Email { get; set; } // it will be used as username(unique).

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        
        public bool IsAdmin { get; set; } = false; //managger = true, user/customer = false.
    }
}
 

