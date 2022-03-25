using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AtlasTracker.Models
{
    public class TicketHistory
    {
        //PRIMARY KEY
        public int Id { get; set; }

        // nullible, NOT required

        [DisplayName("Updated Ticket Item")]
        public string? PropertyName { get; set; }

        [DisplayName("Description of Change")]
        public string? Description { get; set; }

        [DisplayName("Date Created")]
        [DataType(DataType.Date)]
        public DateTimeOffset Created { get; set; }

        [DisplayName("Previous")]
        public string? OldValue { get; set; }

        [DisplayName("Current")]
        public string? NewValue { get; set; }


        //FOREIGN KEY
        public int TicketId { get; set; }

        //FOREIGN KEY
        [Required]
        public string? UserId { get; set; }

        //NAVIGATION PROPERTIES

        [DisplayName("Ticket")]
        public virtual Ticket? Ticket { get; set; }

        [DisplayName("Team Member")]
        public virtual BTUser? User { get; set; }
    }
}
