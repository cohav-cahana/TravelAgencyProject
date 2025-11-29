using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace TravelAgencyProject.Models
{
    public class WaitingList
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter User Id")]
        public int UserId { get; set; }
        public User? User { get; set; } //who is waiting?

        [Required(ErrorMessage = "Please enter Trip Id")]
        public int TripId { get; set; }
        public Trip? Trip { get; set; } //for which trip?

        [Required]
        [Display(Name = "Request Date")]
        public DateTime RequestDate { get; set; } = DateTime.Now; // who  signed first is served first, FIFO

        [Required]
        public bool HasBeenNotified { get; set; } = false; // to avoid multiple notifications
    }
}
