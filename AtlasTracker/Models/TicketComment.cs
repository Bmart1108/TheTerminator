using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AtlasTracker.Models
{
    public class TicketComment
    {
        //PRIMARY KEY
        public int Id { get; set; }

        [Required]
        [DisplayName("Member Comment")]
        [StringLength(2000)]
        public string? Comment { get; set; }

        [DataType(DataType.Date)]
        [DisplayName("Comment Date")]
        public DateTimeOffset Created { get; set; }


        //FOREIGN KEY

        public int TicketId { get; set; }

        //FOREIGN KEY
        [Required]
        public string? UserId { get; set; }


        //NAVIGATION PROPERTIES
        [DisplayName("Ticket")]
        public virtual Ticket? Ticket { get; set; } // A comment belongs to one ticket

        [DisplayName("Team Member")]
        public virtual BTUser? User { get; set; } // A comment only has one user

    }
}
