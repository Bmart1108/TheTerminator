using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtlasTracker.Models
{
    public class Company
    { 
        // PRIMARY KEY
        public int Id { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [DisplayName("Name")]
        public string? Name { get; set; }


        
        [DisplayName("Company Description")]
        [StringLength(2500, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        public string? Description { get; set; }

        //[NotMapped]
        //[DataType(DataType.Upload)]
        //[Display(Name = "Company Image")]
        //public IFormFile? ProjectImageFile { get; set; } // file on computer 
        //public byte[]? ProjectImageData { get; set; } // file split up into bytes and added into an array // byte[] ImageData
        //public string? ProjectImageType { get; set; } // store the image type so it can be rendered


        //NAVIGATION PROPERTIES
        public virtual ICollection<BTUser> Members { get; set; } = new HashSet<BTUser>();

        public virtual ICollection<Project> Projects { get; set; } = new HashSet<Project>();

        public virtual ICollection<Invite> Invites { get; set; } = new HashSet<Invite>();

    }
}
