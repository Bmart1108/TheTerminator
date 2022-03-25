using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AtlasTracker.Models
{
    public class Notification
    {
        public int Id { get; set; }

        //FOREIGN KEY
        [DisplayName("Ticket")]
        public int? TicketId { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [DisplayName("Title")]
        public string? Title { get; set; }

        [Required]
        [StringLength(4000, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [DisplayName("Message")]
        public string? Message { get; set; }

        [DataType(DataType.Date)]
        [DisplayName("Date")]
        public DateTimeOffset Created { get; set; }


        [DisplayName("Has been viewed")]
        public bool Viewed { get; set; }

        //FOREIGN KEY
        [Required]        
        public string? SenderId { get; set; }


        //FOREIGN KEY
        [Required]        
        public string? RecipientId { get; set; }

 
        [Required]
        public int NotificationTypeId { get; set; }

        //FOREIGN KEY
        //[DisplayName("Project")]
        //public int? ProjectId { get; set; }


        //NAVIGATIONAL PROPERTIES 

        public virtual NotificationType? NotificationType { get; set; }        
        public virtual Ticket? Ticket { get; set; }
        public virtual BTUser? Recipient { get; set; }
        public virtual BTUser? Sender { get; set; }
        //public virtual Project? Project { get; set; }
    }
}
