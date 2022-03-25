using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtlasTracker.Models
{
    public class Project
    {
        //PRIMARY KEY
        public int Id { get; set; }

        // NAV PROPERTY
        [DisplayName("Company Id")]
        public int CompanyId { get; set; }

        //NAV PROPERTY
        [DisplayName("Priority")]
        public int ProjectPriorityId { get; set; }

        [Required]
        [StringLength(250, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [DisplayName("Project Name")]
        public string? Name { get; set; }


        [StringLength(3000, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [DisplayName("Project Description")]
        public string? Description { get; set; }


       
        [DataType(DataType.Date)]
        [DisplayName("Created")]
        public DateTimeOffset CreatedDate { get; set; }

        
        [DataType(DataType.Date)]
        [DisplayName("Start Date")]
        public DateTimeOffset StartDate { get; set; }

       
        [DataType(DataType.Date)]
        [DisplayName("End Date")]
        public DateTimeOffset EndDate { get; set; }

        
        //IMAGE PROPERTY
        [NotMapped]
        [DataType(DataType.Upload)]
        
        public IFormFile? ImageFormFile { get; set; } 
        

        [DisplayName("File Name")]
        public string? ImageFileName { get; set; }

        public byte[]? ImageFileData { get; set; } 

        [DisplayName("File Extension")]
        public string? ImageContentType { get; set; } 


        [DisplayName("Archived")]
        public bool Archived { get; set; }

        // Nav PROPERTIES
        public virtual Company? Company { get; set; }
        public virtual ProjectPriority? ProjectPriority { get; set; }

        public virtual ICollection<BTUser> Members { get; set; } = new HashSet<BTUser>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();
        public virtual ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();

     
    }
}
