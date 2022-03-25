#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtlasTracker.Data;
using AtlasTracker.Models;
using AtlasTracker.Extensions;
using AtlasTracker.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using AtlasTracker.Services;

namespace AtlasTracker.Controllers
{
    public class InvitesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBTProjectService _projectservice;
        private readonly IDataProtector _protector;
        private readonly IEmailSender _emailservice;
        private readonly IBTCompanyInfoService _companyinfoservice;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTInviteService _inviteService;


        public InvitesController(ApplicationDbContext context,
                                 IBTProjectService projectservice,
                                 IDataProtectionProvider dataProtectionProvider,
                                 IBTCompanyInfoService companyinfoservice,
                                 UserManager<BTUser> userManager,
                                 IBTInviteService inviteService,
                                 IEmailSender emailservice)
        {
            _context = context;
            _projectservice = projectservice;
            _protector = dataProtectionProvider.CreateProtector("BM.ATLAS.TheBugTracker");

            _companyinfoservice = companyinfoservice;
            _userManager = userManager;
            _inviteService = inviteService;
            _emailservice = emailservice;
        }

        // GET: Invites
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Invites.Include(i => i.Company).Include(i => i.Invitee).Include(i => i.Invitor).Include(i => i.Project);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Invites/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invite = await _context.Invites
                .Include(i => i.Company)
                .Include(i => i.Invitee)
                .Include(i => i.Invitor)
                .Include(i => i.Project)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (invite == null)
            {
                return NotFound();
            }

            return View(invite);
        }

        // GET: Invites/Create
        public async Task<IActionResult> Create()
        {
            int companyId = User.Identity.GetCompanyId();

            ViewData["ProjectId"] = new SelectList(await _projectservice.GetAllProjectsByCompanyAsync(companyId), "Id", "Name");

            return View();
        }

        // POST: Invites/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ProjectId,InviteeEmail,InviteeFirstName,InviteeLastName,Message")] Invite invite)
        {
            int companyId = User.Identity.GetCompanyId();
            ModelState.Remove("InvitorId");

            if (ModelState.IsValid)
            {
                Guid guid = Guid.NewGuid();

                string token = _protector.Protect(guid.ToString());
                string email = _protector.Protect(invite.InviteeEmail);
                string company = _protector.Protect(companyId.ToString());

                string callbackUrl = Url.Action("ProcessInvite", "Invites", new { token, email, company }, protocol: Request.Scheme);

                string body = $@"{invite.Message} <br />
                              Please join my Company. <br />
                              Click the following link to join our team. <br />
                              <a href = ""{callbackUrl}"">COLLABORATE</a>";

                string destination = invite.InviteeEmail;

                //Customize company name here
                Company btCompany = await _companyinfoservice.GetCompanyInfoByIdAsync(companyId);
                string subject = $"The Terminiator: {btCompany.Name}";

                await _emailservice.SendEmailAsync(destination, subject, body);

                invite.CompanyToken = guid;
                invite.CompanyId = companyId;
                invite.InviteDate = DateTime.UtcNow;
                invite.InvitorId = _userManager.GetUserId(User);
                invite.IsValid = true;

                await _inviteService.AddNewInviteAsync(invite);

                return RedirectToAction("Dashboard", "Home", new {swalMessage = "Invite Sent"});
            }

            ViewData["ProjectId"] = new SelectList(await _projectservice.GetAllProjectsByCompanyAsync(companyId), "Id", "Name");

            return View(invite);
        }



        [HttpGet]
        public async Task<IActionResult> ProcessInvite(string token, string email, string company)
        {
            if (token == null)
            {
                return NotFound();
            }
            Guid companyToken = Guid.Parse(_protector.Unprotect(token));
            string inviteeEmail = _protector.Unprotect(email);
            int companyId = int.Parse(_protector.Unprotect(company));
            try
            {
                Invite invite = await _inviteService.GetInviteAsync(companyToken, inviteeEmail, companyId);
                if (invite != null)
                {
                    return View(invite);
                }
                return NotFound();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
