using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AtlasTracker.Models
{
    public class Ticket
    {
        //PRIMARY KEY
        public int Id { get; set; }


        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [DisplayName("Ticket Title")]
        public string? Title { get; set; }

        [DisplayName("Ticket Description")]
        [StringLength(2000, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        public string? Description { get; set; }

        [DataType(DataType.Date)]
        [DisplayName("Created")]
        public DateTimeOffset Created { get; set; }

        [DataType(DataType.Date)]
        [DisplayName("Updated")]
        public DateTimeOffset? Updated { get; set; }

        [DisplayName("Archived")]
        public bool Archived { get; set; }

        [DisplayName("Archived by Project")]
        public bool ArchivedByProject { get; set; }

        //FOREIGN KEY       
        public int ProjectId { get; set; }

        //FOREIGN KEY        
        public int TicketTypeId { get; set; }

        //FOREIGN KEY        
        public int TicketPriorityId { get; set; }

        //FOREIGN KEY        
        public int TicketStatusId { get; set; }

        //FOREIGN KEY
        [Required]
        public string? OwnerUserId { get; set; }

        //FOREIGN KEY
        public string? DeveloperUserId { get; set; }


        //Nav Props
        [DisplayName("Project")]
        public virtual Project? Project { get; set; }
        public virtual TicketType? TicketType { get; set; }
        public virtual TicketPriority? TicketPriority { get; set; }
        public virtual TicketStatus? TicketStatus { get; set; }
        public virtual BTUser? OwnerUser { get; set; }
        public virtual BTUser? DeveloperUser { get; set; }



        public virtual ICollection<TicketComment> Comments { get; set; } = new HashSet<TicketComment>();
        public virtual ICollection<TicketAttachment> Attachments { get; set; } = new HashSet<TicketAttachment>();
        public virtual ICollection<TicketHistory> History { get; set; } = new HashSet<TicketHistory>();
        public virtual ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();
       


        //public ICollection<TicketTask> Tasks = new HashSet<TicketTask>();
    }
}
