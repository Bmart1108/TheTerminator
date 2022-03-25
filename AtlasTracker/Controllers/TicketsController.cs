#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtlasTracker.Data;
using AtlasTracker.Models;
using Microsoft.AspNetCore.Identity;
using AtlasTracker.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using AtlasTracker.Extensions;
using AtlasTracker.Models.Enums;
using AtlasTracker.Models.ViewModels;



namespace AtlasTracker.Controllers
{
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBTProjectService _projectService;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTRolesService _rolesService;
        private readonly IBTLookupService _lookupsService;
        private readonly IBTCompanyInfoService _companyInfoService;
        private readonly IBTTicketService _ticketService;
        private readonly IBTFileService _fileService;
        private readonly IBTTicketHistoryService _ticketHistoryService;
        private readonly IBTNotificationService _notificationService;
        public TicketsController(UserManager<BTUser> userManager,
                                  IBTProjectService projectService,
                                  IBTRolesService rolesService,
                                  IBTLookupService lookupsService,
                                  IBTCompanyInfoService companyInfoService,
                                  IBTTicketService ticketService,
                                  IBTFileService fileService,
                                  ApplicationDbContext context,
                                  IBTTicketHistoryService ticketHistoryService,
                                  IBTNotificationService notificationService)
        {

            _projectService = projectService;
            _userManager = userManager;
            _rolesService = rolesService;
            _lookupsService = lookupsService;
            _companyInfoService = companyInfoService;
            _ticketService = ticketService;
            _fileService = fileService;
            _context = context;
            _ticketHistoryService = ticketHistoryService;
            _notificationService = notificationService;
        }


        //My Tickets
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpGet]
        public async Task<IActionResult> MyTickets()
        {
            string userId = _userManager.GetUserId(User);
            int companyId = User.Identity.GetCompanyId();

            List<Ticket> tickets = await _ticketService.GetTicketsByUserIdAsync(userId, companyId);


            return View(tickets);
        }

        //All Tickets
        public async Task<IActionResult> Alltickets()
        {
            int companyId = User.Identity.GetCompanyId();
            List<Ticket> tickets = await _ticketService.GetAllTicketsByCompanyAsync(companyId);

            if (User.IsInRole(nameof(BTRole.Developer)) || User.IsInRole(nameof(BTRole.Submitter)))
            {
                tickets = await _companyInfoService.GetAllTicketsAsync(companyId);
            }
            else
            {
                tickets = (await _ticketService.GetAllTicketsByCompanyAsync(companyId))
                                               .Where(t => t.Archived == false).ToList();
            }

            return View(tickets);
        }

        //Archived Tickets
        public async Task<IActionResult> ArchivedTickets()
        {
            int companyId = User.Identity.GetCompanyId();
            List<Ticket> tickets = await _ticketService.GetArchivedTicketsAsync(companyId);

            return View(tickets);
        }

        //Unassigned Tickets
        [HttpGet]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> UnassignedTickets()
        {
            int companyId = User.Identity.GetCompanyId();
            string btUserId = _userManager.GetUserId(User);

            List<Ticket> tickets = await _ticketService.GetUnassignedTicketsAsync(companyId);
            if (User.IsInRole(nameof(BTRole.Admin)))
            {
                return View(tickets);
            }
            else
            {
                List<Ticket> pmTickets = new();
                foreach (Ticket ticket in tickets)
                {
                    if (await _projectService.IsAssignedProjectManagerAsync(btUserId, ticket.ProjectId))
                        pmTickets.Add(ticket);
                }
                return RedirectToAction(nameof(AllTickets));
            }
        }


        [HttpGet]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> AssignDeveloper(int? ticketId)
        {
            if (ticketId == null)
            {
                return NotFound();
            }

            AssignDeveloperViewModel model = new();


            model.Ticket = await _ticketService.GetTicketByIdAsync(ticketId.Value);
            model.DeveloperList = new SelectList(await _projectService.GetProjectMembersByRoleAsync(model.Ticket.ProjectId, nameof(BTRole.Developer)), "Id", "FullName");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AssignDeveloper(AssignDeveloperViewModel model)
        {

            

            if (model.DeveloperId != null)
            {
                BTUser btUser = await _userManager.GetUserAsync(User);
                model.Ticket = await _ticketService.GetTicketByIdAsync(model.Ticket.Id);
                await _ticketService.AssignTicketAsync(model.Ticket.Id, model.DeveloperId);

                Ticket oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(model.Ticket!.Id);
                try
                {
                    await _ticketService.AssignTicketAsync(model.Ticket.Id, model.DeveloperId);
                }
                catch (Exception)
                {
                    throw;
                }

                Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(model.Ticket.Id);
                await _ticketHistoryService.AddHistoryAsync(oldTicket, newTicket, btUser.Id);

                    // Assign Developer Notification
                    if (model.Ticket.DeveloperUserId != null)
                    {
                        Notification devNotification = new()
                        {
                            TicketId = model.Ticket.Id,
                            NotificationTypeId = (await _lookupsService.LookupNotificationTypeIdAsync(nameof(BTNotificationType.Ticket))).Value,
                            Title = "Ticket Updated",
                            Message = $"Ticket: {model.Ticket.Title}, was updated by {btUser.FullName}",
                            Created = DateTime.UtcNow,
                            SenderId = btUser.Id,
                            RecipientId = model.Ticket.DeveloperUserId
                        };
                        await _notificationService.AddNotificationAsync(devNotification);
                        await _notificationService.SendEmailNotificationAsync(devNotification, "Ticket Updated");
                    }

                return RedirectToAction(nameof(Details));
            }

            return RedirectToAction(nameof(AssignDeveloper), new { ticketId = model.Ticket!.Id });
        }
        // GET: Tickets

        [HttpGet]
        public async Task<IActionResult> AllTickets()
        {

            List<Ticket> tickets = new();

            int companyId = User.Identity.GetCompanyId();

            if (User.IsInRole(nameof(BTRole.Admin)) || User.IsInRole(nameof(BTRole.ProjectManager)))
            {
                tickets = await _companyInfoService.GetAllTicketsAsync(companyId);
            }
            else
            {
                tickets = await _ticketService.GetAllTicketsByCompanyAsync(companyId);
            }

            return View(tickets);
        }




        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTicketComment([Bind("Id,TicketId,Comment")] TicketComment ticketComment)
        {
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                try
                {
                    ticketComment.UserId = _userManager.GetUserId(User);
                    ticketComment.Created = DateTime.UtcNow;

                    await _ticketService.AddTicketCommentAsync(ticketComment);

                    await _ticketHistoryService.AddHistoryAsync(ticketComment.TicketId, nameof(TicketComment), ticketComment.UserId);

                }
                catch (Exception)
                {
                    throw;
                }
            }

            return RedirectToAction("Details", new { id = ticketComment.TicketId });
        }


        // GET: Tickets/Create
        [Authorize(Roles = "Admin, ProjectManager")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {            
           
            BTUser btUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole(nameof(BTRole.Admin)))
            {
                ViewData["ProjectId"] = new SelectList(await _companyInfoService.GetAllProjectsAsync(btUser.CompanyId), "Id", "Name");
            }
            else
            {
                ViewData["ProjectId"] = new SelectList(await _projectService.GetUserProjectsAsync(btUser.Id), "Id", "Name");
            }

            ViewData["TicketPriorityId"] = new SelectList(await _lookupsService.GetTicketPrioritiesAsync(), "Id", "Name");
            ViewData["TicketTypeId"] = new SelectList(await _lookupsService.GetTicketTypesAsync(), "Id", "Name");

            return View();
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,ProjectId,TicketTypeId,TicketPriorityId")] Ticket ticket)
        {
            BTUser btUser = await _userManager.GetUserAsync(User);            
            ModelState.Remove("OwnerUserId");

            if (ModelState.IsValid)
            {
                try
                {
                    ticket.Created = DateTime.UtcNow;
                    ticket.OwnerUserId = btUser.Id;

                    ticket.TicketStatusId = (await _ticketService.LookupTicketStatusIdAsync(nameof(BTTicketStatus.New))).Value;

                    await _ticketService.AddNewTicketAsync(ticket);

                    Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);
                    await _ticketHistoryService.AddHistoryAsync(null!, newTicket, btUser.Id);

                    BTUser projectManager = await _projectService.GetProjectManagerAsync(ticket.ProjectId);
                    int companyId = User.Identity!.GetCompanyId()!;
                    Notification notification = new()
                    {
                        TicketId = ticket.Id,
                        NotificationTypeId = (await _lookupsService.LookupNotificationTypeIdAsync(nameof(BTNotificationType.Ticket))).Value,
                        Title = "New Ticket",
                        Message = $"New Ticket: {ticket.Title}, was created by {btUser.FullName}",
                        Created = DateTime.UtcNow,
                        SenderId = btUser.Id,
                        RecipientId = projectManager?.Id
                    };
                    if (projectManager != null)
                    {
                        await _notificationService.AddNotificationAsync(notification);
                        await _notificationService.SendEmailNotificationAsync(notification, "New Ticket Added");
                    }
                    else
                    {
                        BTUser admin = (await _rolesService.GetUsersInRoleAsync(nameof(BTRole.Admin), companyId)).FirstOrDefault();
                        notification.RecipientId = admin?.Id;
                        //Admin notification
                        await _notificationService.AddNotificationAsync(notification);
                        await _notificationService.SendEmailNotificationsByRoleAsync(notification, companyId, nameof(BTRole.Admin));
                    }

                    return RedirectToAction(nameof(AllTickets));
                }
                catch (Exception)
                {
                    throw;
                }
            }


            if (User.IsInRole(nameof(BTRole.Admin)))
            {
                ViewData["ProjectId"] = new SelectList(await _companyInfoService.GetAllProjectsAsync(btUser.CompanyId), "Id", "Name");
            }
            else
            {
                ViewData["ProjectId"] = new SelectList(await _projectService.GetUserProjectsAsync(btUser.Id), "Id", "Name");
            }

            ViewData["TicketPriorityId"] = new SelectList(await _lookupsService.GetTicketPrioritiesAsync(), "Id", "Name");
            ViewData["TicketTypeId"] = new SelectList(await _lookupsService.GetTicketTypesAsync(), "Id", "Name");

            return View(ticket);
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(int? id)

        {            

            if (id == null)
            {
                return NotFound();
            }

            Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);
            BTUser btUser = await _userManager.GetUserAsync(User);
           

            if (ticket == null)
            {
                return NotFound();
            }
            ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompanyAsync(btUser.CompanyId), "Id", "Name");
            ViewData["TicketPriorityId"] = new SelectList(await _lookupsService.GetProjectPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(await _lookupsService.GetTicketStatusesAsync(), "Id", "Name", ticket.TicketStatusId);
            ViewData["TicketTypeId"] = new SelectList(await _lookupsService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);

            return View(ticket);
        }
        // POST: Tickets/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Created,Updated,Archived,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,DeveloperUserId")] Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                BTUser btUser = await _userManager.GetUserAsync(User);                
                Ticket oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);

                try
                {
                    ticket.Created = DateTime.SpecifyKind(ticket.Created.DateTime, DateTimeKind.Utc);
                    ticket.Updated = DateTime.UtcNow;

                    await _ticketService.UpdateTicketAsync(ticket);

                    // Ticket Edit notification
                    BTUser projectManager = await _projectService.GetProjectManagerAsync(ticket.ProjectId);
                    int companyId = User.Identity!.GetCompanyId();
                 
                    Notification notification = new()
                    {
                        TicketId = ticket.Id,
                        NotificationTypeId = (await _lookupsService.LookupNotificationTypeIdAsync(nameof(BTNotificationType.Ticket))).Value,
                        Title = "Ticket updated",
                        Message = $"Ticket: {ticket.Title}, was updated by {btUser.FullName}",
                        Created = DateTime.UtcNow,
                        SenderId = btUser.Id,
                        RecipientId = projectManager?.Id
                    };
                    // Notify PM or Admin
                    if (projectManager != null)
                    {
                     
                        await _notificationService.AddNotificationAsync(notification);
                        await _notificationService.SendEmailNotificationAsync(notification, "Ticket Updated");
                    }
                    else
                    {
                        BTUser admin = (await _rolesService.GetUsersInRoleAsync(nameof(BTRole.Admin), companyId)).FirstOrDefault();
                        notification.RecipientId = admin?.Id;
                        //Admin notification
                        await _notificationService.AddNotificationAsync(notification);
                        await _notificationService.SendEmailNotificationsByRoleAsync(notification, companyId, nameof(BTRole.Admin));
                    }
                    //Notify Developer
                    if (ticket.DeveloperUserId != null)
                    {
                        Notification devNotification = new()
                        {
                            TicketId = ticket.Id,
                            NotificationTypeId = (await _lookupsService.LookupNotificationTypeIdAsync(nameof(BTNotificationType.Ticket))).Value,
                            Title = "Ticket Updated",
                            Message = $"Ticket: {ticket.Title}, was updated by {btUser.FullName}",
                            Created = DateTime.UtcNow,                          
                            SenderId = btUser.Id,
                            RecipientId = ticket.DeveloperUserId
                        };
                        await _notificationService.AddNotificationAsync(devNotification);
                        await _notificationService.SendEmailNotificationAsync(devNotification, "Ticket Updated");
                    }



                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await TicketExists(ticket.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);
                await _ticketHistoryService.AddHistoryAsync(oldTicket, newTicket, btUser.Id);

                return RedirectToAction(nameof(AllTickets));
            }
            ViewData["TicketPriorityId"] = new SelectList(await _lookupsService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(await _lookupsService.GetTicketStatusesAsync(), "Id", "Id", ticket.TicketStatusId);
            ViewData["TicketTypeId"] = new SelectList(await _lookupsService.GetTicketTypesAsync(), "Id", "Id", ticket.TicketTypeId);

            return View(ticket);
        }

        // GET: Tickets/ARCHIVE/5
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpGet]
        public async Task<IActionResult> Archive(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            int companyId = User.Identity.GetCompanyId();
            Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);



            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }


        // POST: Tickets/ARCHIVE/5
        [HttpPost, ActionName("Archive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveConfirmed(int id)
        {
            int companyId = User.Identity.GetCompanyId();
            Ticket ticket = await _ticketService.GetTicketByIdAsync(id);
            ticket.Archived = true;
            await _ticketService.ArchiveTicketAsync(ticket);

            return RedirectToAction(nameof(AllTickets));
        }


        //GET RESTORE TICKETS
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpGet]
        public async Task<IActionResult> Restore(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            int companyId = User.Identity.GetCompanyId();
            var ticket = await _ticketService.GetTicketByIdAsync(id.Value);



            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }



        //POST RESTORE TICKETS
        [HttpPost, ActionName("Restore")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreConfirmed(int id)
        {
            Ticket ticket = await _ticketService.GetTicketByIdAsync(id);
            ticket.Archived = false;
            await _ticketService.UpdateTicketAsync(ticket);

            return RedirectToAction(nameof(AllTickets));
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTicketAttachment([Bind("Id,FormFile,Description,TicketId")] TicketAttachment ticketAttachment)
        {
            string statusMessage;
            ModelState.Remove("UserId");

            if (ModelState.IsValid && ticketAttachment.FormFile != null)
            {
                ticketAttachment.FileData = await _fileService.ConvertFileToByteArrayAsync(ticketAttachment.FormFile);
                ticketAttachment.FileName = ticketAttachment.FormFile.FileName;
                ticketAttachment.FileContentType = ticketAttachment.FormFile.ContentType;

                ticketAttachment.Created = DateTime.UtcNow;
                ticketAttachment.UserId = _userManager.GetUserId(User);

                await _ticketService.AddTicketAttachmentAsync(ticketAttachment);
                statusMessage = "Success: New attachment added to Ticket.";
            }
            else
            {
                statusMessage = "Error: Invalid data.";

            }

            await _ticketHistoryService.AddHistoryAsync(ticketAttachment.TicketId, nameof(TicketAttachment), ticketAttachment.UserId);

            return RedirectToAction("Details", new { id = ticketAttachment.TicketId, message = statusMessage });
        }

        public async Task<IActionResult> ShowFile(int id)
        {
            TicketAttachment ticketAttachment = await _ticketService.GetTicketAttachmentByIdAsync(id);
            string fileName = ticketAttachment.FileName;
            byte[] fileData = ticketAttachment.FileData;
            string ext = Path.GetExtension(fileName).Replace(".", "");

            Response.Headers.Add("Content-Disposition", $"inline; filename={fileName}");
            return File(fileData, $"application/{ext}");
        }

        private async Task<bool> TicketExists(int id)

        {
            int companyId = User.Identity.GetCompanyId();

            return (await _ticketService.GetAllTicketsByCompanyAsync(companyId))
                                        .Any(t => t.Id == id);
        }
   


        private bool ProjectPriorityExists(int id)
        {
            return _context.ProjectPriorities.Any(e => e.Id == id);
        }
    }
}



