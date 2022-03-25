using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtlasTracker.Models
{
    public class BTUser : IdentityUser
    {
        [Required]
        [StringLength(25, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [DisplayName ("First Name")]
        public string? FirstName { get; set; }


        [StringLength(25, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [DisplayName ("Last Name")]
        public string? LastName { get; set; }


        [NotMapped]
        [DisplayName ("Full Name")]
        public string? FullName { get { return $"{FirstName} {LastName}"; } }


        [NotMapped]
        [DataType(DataType.Upload)]       
        public IFormFile? AvatarFormFile { get; set; }

        [DisplayName("Avatar")]

        public string? AvatarName { get; set; }
        public byte[]? AvatarData { get; set; }

        [DisplayName("File Extension")]
        public string? AvatarContentType { get; set; }


        public int CompanyId  { get; set; }

        public virtual Company? Company { get; set; }


        public virtual ICollection<Project> Projects { get; set; } = new HashSet<Project>();


    }
}
