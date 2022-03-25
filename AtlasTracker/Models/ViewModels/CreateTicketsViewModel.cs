using Microsoft.AspNetCore.Mvc.Rendering;

namespace AtlasTracker.Models.ViewModels
{
    public class CreateTicketsViewModel
    {
        public SelectList? Project { get; set; }

        public SelectList? DeveloperUser { get; set; }

        public SelectList? OwnerUser { get; set; }

        public SelectList? TicketStatus { get; set; }
        public SelectList? TicketType { get; set; }
        public SelectList? TicketPriority{ get; set; }





    }
}
