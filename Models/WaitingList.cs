using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace TravelAgencyProject.Models
{
    public class WaitingList
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; } //who is waiting?

        public int TripId { get; set; }
        public Trip? Trip { get; set; } //for which trip?

        public DateTime RequestDate { get; set; } = DateTime.Now; // who  signed first is served first.
    }
}
