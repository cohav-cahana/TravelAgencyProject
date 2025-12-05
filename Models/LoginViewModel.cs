using System.ComponentModel.DataAnnotations;

namespace TravelAgencyProject.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}