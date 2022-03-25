using AtlasTracker.Extensions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static AtlasTracker.Extensions.MaxFileSizeAttribute;

namespace AtlasTracker.Models
{
    public class TicketAttachment
    {   //PRIMARY KEY
        public int Id { get; set; }
        [DisplayName("File Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [DisplayName("Created")]
        [DataType(DataType.Date)]
        public DateTimeOffset Created { get; set; }

        //IMAGE PROPERTY
        [NotMapped]
        [DisplayName("Select A File")]
        [DataType(DataType.Upload)]
        [MaxFileSize(1024 * 1024)]
        [AllowedExtensions(new string[] { ".jpg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".pdf" })]
        public IFormFile? FormFile { get; set; }
        

        [DisplayName("File Name")]
        public string? FileName { get; set; }
        public byte[]? FileData { get; set; }

        [DisplayName("File Extension")]
        public string? FileContentType { get; set; }      

   

        //FOREIGN KEY
        [Required]
        [DisplayName("User ID")]
        public string? UserId { get; set; }


        //FOREIGN KEY
        [DisplayName("Ticket ID")]        
        public int TicketId { get; set; }




        //FOREIGN KEY
        [DisplayName("Ticket Task")]
        public int TicketTaskId { get; set; }

   



        public virtual Ticket? Ticket { get; set; }
        public virtual BTUser? User { get; set; }

    }
}
